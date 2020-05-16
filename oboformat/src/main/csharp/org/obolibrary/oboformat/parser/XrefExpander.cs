#nullable enable

using NLog;
using org.obolibrary.oboformat.model;
using org.semanticweb.owlapi.util;
using System.Collections.Generic;
using static org.obolibrary.oboformat.model.Frame;
using static org.obolibrary.oboformat.parser.OBOFormatConstants;

namespace org.obolibrary.oboformat.parser
{
    public class XrefExpander
    {
        protected static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        private readonly IDictionary<string, Rule> treatMap = new Dictionary<string, Rule>();
        protected IDictionary<string, OBODoc> targetDocMap = new Dictionary<string, OBODoc>();
        OBODoc sourceOBODoc;
        OBODoc? targetOBODoc;
        string? targetBase;

        /**
         * @param src src
         * @throws InvalidXrefMapException InvalidXrefMapException
         */
        public XrefExpander(OBODoc src)
        {
            sourceOBODoc = src;
            Frame shf = OWLAPIPreconditions.CheckNotNull(src.HeaderFrame);
            string? ontId = shf.GetTagValue<string>(OboFormatTag.TAG_ONTOLOGY);
            string tgtOntId = ontId + "/xref_expansions";
            targetOBODoc = new OBODoc();
            Frame thf = new Frame(FrameType.HEADER);
            thf.AddClause(new Clause(OboFormatTag.TAG_ONTOLOGY, tgtOntId));
            targetOBODoc.HeaderFrame = thf;
            sourceOBODoc.AddImportedOBODoc(targetOBODoc);
            SetUp();
        }

        /**
         * @param src src
         * @param targetBase targetBase
         * @throws InvalidXrefMapException InvalidXrefMapException
         */
        public XrefExpander(OBODoc src, string targetBase)
        {
            sourceOBODoc = src;
            this.targetBase = targetBase;
            SetUp();
        }

        /**
         * @param src src
         * @param tgt tgt
         * @throws InvalidXrefMapException InvalidXrefMapException
         */
        public XrefExpander(OBODoc src, OBODoc tgt)
        {
            sourceOBODoc = src;
            targetOBODoc = tgt;
            SetUp();
        }

        private static string GetIDSpace(string x)
        {
            string[] parts = x.Split(":", 2);
            return parts[0];
        }

        /**
         * @throws InvalidXrefMapException InvalidXrefMapException
         */
        public void SetUp()
        {
            // required for translation of IDs
            IDictionary<string, string> relationsUseByIdSpace = new Dictionary<string, string>();
            Frame? headerFrame = sourceOBODoc.HeaderFrame;
            if (headerFrame != null)
            {
                foreach (Clause c in headerFrame.GetClauses())
                {
                    string[] parts;
                    string v = c.Value().ToString();
                    parts = v.Split("\\s");
                    string? relation = null;
                    string idSpace = parts[0];
                    string? tag = c.Tag;
                    if (tag == null)
                    {
                        continue;
                    }
                    if (tag.Equals(OboFormatTag.TAG_TREAT_XREFS_AS_EQUIVALENT.Tag))
                    {
                        AddRule(parts[0], new EquivalenceExpansion());
                    }
                    else if (tag.Equals(OboFormatTag.TAG_TREAT_XREFS_AS_GENUS_DIFFERENTIA.Tag))
                    {
                        AddRule(idSpace, new GenusDifferentiaExpansion(parts[1], parts[2]));
                        relationsUseByIdSpace[idSpace] = parts[1];
                        relation = parts[1];
                    }
                    else if (tag.Equals(OboFormatTag.TAG_TREAT_XREFS_AS_REVERSE_GENUS_DIFFERENTIA.Tag))
                    {
                        AddRule(idSpace, new ReverseGenusDifferentiaExpansion(parts[1], parts[2]));
                        relationsUseByIdSpace[idSpace] = parts[1];
                        relation = parts[1];
                    }
                    else if (tag.Equals(OboFormatTag.TAG_TREAT_XREFS_AS_HAS_SUBCLASS.Tag))
                    {
                        AddRule(idSpace, new HasSubClassExpansion());
                    }
                    else if (tag.Equals(OboFormatTag.TAG_TREAT_XREFS_AS_IS_A.Tag))
                    {
                        AddRule(idSpace, new IsaExpansion());
                    }
                    else if (tag.Equals(OboFormatTag.TAG_TREAT_XREFS_AS_RELATIONSHIP.Tag))
                    {
                        AddRule(idSpace, new RelationshipExpansion(parts[1]));
                        relationsUseByIdSpace[idSpace] = parts[1];
                        relation = parts[1];
                    }
                    else
                    {
                        continue;
                    }
                    if (targetBase != null)
                    {
                        // create a new bridge ontology for every expansion macro
                        OBODoc tgt = new OBODoc();
                        Frame thf = new Frame(FrameType.HEADER);
                        thf.AddClause(new Clause(OboFormatTag.TAG_ONTOLOGY,
                            targetBase + "-" + idSpace.ToLower()));
                        tgt.HeaderFrame = thf;
                        targetDocMap[idSpace] = tgt;
                        sourceOBODoc.AddImportedOBODoc(tgt);
                        if (relation != null)
                        {
                            // See 4.4.2
                            // "In addition, any Typedef frames for relations used
                            // in a header macro are also copied into the
                            // corresponding bridge ontology
                            Frame? tdf = sourceOBODoc.GetTypedefFrame(relation);
                            if (tdf != null)
                            {
                                try
                                {
                                    tgt.AddTypedefFrame(tdf);
                                }
                                catch (FrameMergeException e)
                                {
                                    LOGGER.Debug("frame merge failed", e);
                                }
                            }
                        }
                    }
                }
            }
        }

        /**
         * @param idSpace idSpace
         * @return target doc
         */
        public OBODoc GetTargetDoc(string idSpace)
        {
            if (targetOBODoc != null)
            {
                return targetOBODoc;
            }
            return targetDocMap[idSpace];
        }

        private void AddRule(string db, Rule rule)
        {
            if (treatMap.ContainsKey(db))
            {
                throw new InvalidXrefMapException(db);
            }
            rule.IdSpace = db;
            treatMap[db] = rule;
        }

        /**
         * Expand xrefs.
         */
        public void ExpandXrefs()
        {
            foreach (Frame f in sourceOBODoc.GetTermFrames())
            {
                string id = OWLAPIPreconditions.CheckNotNull(f.GetTagValue<string>(OboFormatTag.TAG_ID));
                ICollection<Clause> clauses = f.GetClauses(OboFormatTag.TAG_XREF);
                foreach (Clause c in clauses)
                {
                    Xref? x = c.Value<Xref>();
                    string xid = x.Idref;
                    string s = GetIDSpace(xid);
                    if (treatMap.ContainsKey(s))
                    {
                        treatMap[s].Expand(this, f, id, xid);
                    }
                }
            }
        }

        /**
         * Rule.
         */
        private abstract class Rule
        {
            protected string? xref;
            /**
             * Id space.
             */
            public string? IdSpace { get; set; }

            /**
             * @param sf sf
             * @param id id
             * @param xRef xref
             */
            public abstract void Expand(XrefExpander expander, Frame sf, string id, string xRef);

            protected Frame GetTargetFrame(XrefExpander expander, string id)
            {
                OBODoc targetDoc = expander.GetTargetDoc(OWLAPIPreconditions.VerifyNotNull(IdSpace, "idSpace not set yet"));
                Frame? f = targetDoc.GetTermFrame(id);
                if (f == null)
                {
                    f = new Frame
                    {
                        Id = id
                    };
                    try
                    {
                        targetDoc.AddTermFrame(f);
                    }
                    catch (FrameMergeException e)
                    {
                        // this should be impossible
                        LOGGER.Error("Frame merge exceptions should not be possible", e);
                    }
                }
                return f;
            }
        }

        /**
         * Equivalence expansion.
         */
        private class EquivalenceExpansion : Rule
        {

            public override void Expand(XrefExpander expander, Frame sf, string id, string xRef)
            {
                Clause c = new Clause(OboFormatTag.TAG_EQUIVALENT_TO, xRef);
                sf.AddClause(c);
            }
        }

        /**
         * Subclass expansion.
         */
        private class HasSubClassExpansion : Rule
        {

            public override void Expand(XrefExpander expander, Frame sf, string id, string xRef)
            {
                Clause c = new Clause(OboFormatTag.TAG_IS_A, id);
                GetTargetFrame(expander, xRef).AddClause(c);
            }
        }

        /**
         * Genus diff expansion.
         */
        private class GenusDifferentiaExpansion : Rule
        {

            protected readonly string rel;
            protected readonly string tgt;

            /**
             * @param rel rel
             * @param tgt tgt
             */
            public GenusDifferentiaExpansion(string rel, string tgt)
            {
                this.rel = rel;
                this.tgt = tgt;
            }

            public override void Expand(XrefExpander expander, Frame sf, string id, string xRef)
            {
                Clause gc = new Clause(OboFormatTag.TAG_INTERSECTION_OF, xRef);
                Clause dc = new Clause(OboFormatTag.TAG_INTERSECTION_OF);
                dc.SetValue(rel);
                dc.AddValue(tgt);
                GetTargetFrame(expander, id).AddClause(gc);
                GetTargetFrame(expander, id).AddClause(dc);
            }
        }

        /**
         * Reverse genus differentia expansion.
         */
        private class ReverseGenusDifferentiaExpansion : Rule
        {

            protected readonly string rel;
            protected readonly string tgt;

            /**
             * @param rel rel
             * @param tgt tgt
             */
            public ReverseGenusDifferentiaExpansion(string rel, string tgt)
            {
                this.rel = rel;
                this.tgt = tgt;
            }

            public override void Expand(XrefExpander expander, Frame sf, string id, string xRef)
            {
                Clause gc = new Clause(OboFormatTag.TAG_INTERSECTION_OF, id);
                Clause dc = new Clause(OboFormatTag.TAG_INTERSECTION_OF);
                dc.SetValue(rel);
                dc.AddValue(tgt);
                GetTargetFrame(expander, xRef).AddClause(gc);
                GetTargetFrame(expander, xRef).AddClause(dc);
            }
        }

        /**
         * Is a expansion.
         */
        private class IsaExpansion : Rule
        {

            public override void Expand(XrefExpander expander, Frame sf, string id, string xRef)
            {
                Clause c = new Clause(OboFormatTag.TAG_IS_A, xRef);
                GetTargetFrame(expander, id).AddClause(c);
            }
        }

        /**
         * Relationship expansion.
         */
        private class RelationshipExpansion : Rule
        {

            protected readonly string rel;

            /**
             * @param rel rel
             */
            public RelationshipExpansion(string rel)
            {
                this.rel = rel;
            }

            public override void Expand(XrefExpander expander, Frame sf, string id, string xRef)
            {
                Clause c = new Clause(OboFormatTag.TAG_RELATIONSHIP, rel);
                c.AddValue(xRef);
                GetTargetFrame(expander, id).AddClause(c);
            }
        }
    }
}
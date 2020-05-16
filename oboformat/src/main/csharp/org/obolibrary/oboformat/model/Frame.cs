#nullable enable

using com.melandra.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static org.obolibrary.oboformat.parser.OBOFormatConstants;

namespace org.obolibrary.oboformat.model
{

    //import static org.semanticweb.owlapi.util.OWLAPIPreconditions.verifyNotNull;
    //import org.obolibrary.obo2owl.OboInOwlCardinalityTools;
    //import org.obolibrary.oboformat.parser.OBOFormatConstants.OboFormatTag;

    public class Frame
    {

        /**
         * The clauses.
         */
        protected ICollection<Clause> clauses = new List<Clause>();
        /**
         * The id.
         */
        public string? Id { get; set; }
        /**
         * The type.
         */
        public FrameType? Type { get; set; }

        /**
         * Instantiates a new frame.
         */
        public Frame()
            : this(null)
        {
        }

        /**
         * Instantiates a new frame.
         *
         * @param type the type
         */
        public Frame(FrameType? type)
        {
            Type = type;
        }

        /**
         * freezing a frame signals that a frame has become quiescent, and that data structures can be
         * adjusted to increase performance or reduce memory consumption. If a frozen frame is
         * subsequently modified it will be thawed as necessary.
         */
        public void Freeze()
        {
            if (clauses.Count == 0)
            {
                clauses = Array.Empty<Clause>();
                return;
            }
            foreach (Clause clause in clauses)
                clause.Freeze();
            if (clauses is List<Clause>)
                clauses = clauses.ToArray();
        }

        /**
         * @return the clauses
         */
        public ICollection<Clause> GetClauses()
        {
            return clauses;
        }

        /**
         * @param clauses the new clauses
         */
        public void SetClauses(ICollection<Clause> clauses)
        {
            this.clauses = clauses;
        }

        /**
         * @param tag the tag
         * @return the clauses for tag
         */
        public IList<Clause> GetClauses(string? tag)
        {
            IList<Clause> cls = new List<Clause>();
            if (tag == null)
            {
                return cls;
            }
            foreach (Clause cl in clauses)
            {
                if (tag.Equals(cl.Tag))
                    cls.Add(cl);
            }
            return cls;
        }

        /**
         * @param tag the tag
         * @return the clauses for tag
         */
        public IList<Clause> GetClauses(OboFormatTag tag)
        {
            return GetClauses(tag.Tag);
        }

        /**
         * @param tag the tag
         * @return null if no value set, otherwise first value
         */
        public Clause? GetClause(string? tag)
        {
            if (tag == null)
                return null;
            foreach (Clause cl in clauses)
            {
                if (tag.Equals(cl.Tag))
                    return cl;
                // TODO - throw exception if more than one clause of this type?
            }
            return null;
        }

        /**
         * @param tag the tag
         * @return the clause for tag
         */
        public Clause? GetClause(OboFormatTag tag)
        {
            return GetClause(tag.Tag);
        }

        /**
         * @param cl the clause
         */
        public void AddClause(Clause cl)
        {
            if (!(clauses is List<Clause>))
                clauses = new List<Clause>(clauses);
            clauses.Add(cl);
        }

        override public string ToString()
        {
            StringBuilder sb = new StringBuilder("Frame(");
            sb.Append(Id);
            sb.Append(' ');
            foreach (Clause cl in clauses)
                sb.Append(cl).Append(' ');
            sb.Append(')');
            return sb.ToString();
        }

        /**
         * @param tag the tag
         * @return the tag value for tag
         */
        public object? GetTagValue(string tag)
        {
            Clause? clause = GetClause(tag);
            if (clause == null)
                return null;
            return clause.Value();
        }

        /**
         * @param tag the tag
         * @return the tag value for tag
         */
        public object? GetTagValue(OboFormatTag tag)
        {
            return GetTagValue(tag.Tag);
        }

        /**
         * @param <T> the generic type
         * @param tag the tag
         * @param cls the cls
         * @return the tag value for tag and class
         */
        public T? GetTagValue<T>(string tag) where T : class
        {
            Clause? clause = GetClause(tag);
            if (clause == null)
                return default;
            return clause.Value() as T;
        }

        /**
         * @param <T> the generic type
         * @param tag the tag
         * @param cls the cls
         * @return the tag value for tag and class
         */
        public T? GetTagValue<T>(OboFormatTag tag) where T : class
        {
            return GetTagValue<T>(tag.Tag);
        }

        /**
         * @param tag the tag
         * @return the tag values for tag
         */
        public ICollection<object> GetTagValues(OboFormatTag tag)
        {
            return GetTagValues(tag.Tag);
        }

        /**
         * @param tag the tag
         * @return the tag values for tag
         */
        public ICollection<object> GetTagValues(string tag)
        {
            ICollection<object> vals = new List<object>();
            GetClauses(tag).ForEach(v => vals.Add(v.Value()));
            return vals;
        }

        /**
         * @param <T> the generic type
         * @param tag the tag
         * @param cls the cls
         * @return the tag values for tag and class
         */
        public IEnumerable<T?> GetTagValues<T>(OboFormatTag tag) where T : class
        {
            return GetTagValues<T>(tag.Tag);
        }

        /**
         * @param <T> the generic type
         * @param tag the tag
         * @param cls the cls
         * @return the tag values for tag and class
         */
        public IEnumerable<T?> GetTagValues<T>(string tag) where T : class => GetClauses(tag).Select(c => c.Value<T>());

        /**
         * @param tag the tag
         * @return the tag xrefs for tg
         */
        public ICollection<Xref> GetTagXrefs(string tag)
        {
            ICollection<Xref> xrefs = new List<Xref>();
            Clause? clause = GetClause(tag);
            if (clause != null)
            {
                foreach (object ob in clause.Values)
                    if (ob is Xref)
                        xrefs.Add((Xref)ob);
            }
            return xrefs;
        }

        /**
         * @return the tags
         */
        public ISet<string> GetTags()
        {
            ISet<string> tags = new HashSet<string>();
            GetClauses().ForEach(cl => tags.Add(cl.Tag));
            return tags;
        }

        private bool SameID(Frame f) => Id == null ? f.Id == null : Id.Equals(f.Id);

        private bool SameType(Frame f) => Type == null ? f.Type == null : Type.Equals(f.Type);

        /**
         * @param extFrame the external frame
         * @throws FrameMergeException the frame merge exception
         */
        public void Merge(Frame extFrame)
        {
            if (this == extFrame)
                return;
            if (!SameID(extFrame))
                throw new FrameMergeException("ids do not match");
            if (!SameType(extFrame))
                throw new FrameMergeException("frame types do not match");
            extFrame.GetClauses().ForEach(AddClause);
            // note we do not perform a document structure check at this point
        }

        /**
         * Check this frame for violations, i.e. cardinality constraint violations.
         *
         * @throws FrameStructureException the frame structure exception
         * @see OboInOwlCardinalityTools for equivalent checks in OWL
         */
        public void Check()
        {
            if (FrameType.HEADER.Equals(Type))
            {
                CheckMaxOneCardinality(OboFormatTag.TAG_ONTOLOGY, OboFormatTag.TAG_FORMAT_VERSION,
                                OboFormatTag.TAG_DATE, OboFormatTag.TAG_DEFAULT_NAMESPACE,
                                OboFormatTag.TAG_SAVED_BY, OboFormatTag.TAG_AUTO_GENERATED_BY);
            }
            if (FrameType.TYPEDEF.Equals(Type))
            {
                CheckMaxOneCardinality(OboFormatTag.TAG_DOMAIN, OboFormatTag.TAG_RANGE,
                                OboFormatTag.TAG_IS_METADATA_TAG, OboFormatTag.TAG_IS_CLASS_LEVEL_TAG);
            }
            if (!FrameType.HEADER.Equals(Type))
            {
                IList<Clause> tagIdClauses = GetClauses(OboFormatTag.TAG_ID);
                if (tagIdClauses.Count != 1)
                {
                    throw new FrameStructureException(this, "cardinality of id field must be 1");
                }
                // this call will verify that the value is not null
                tagIdClauses[0].Value();
                if (Id == null)
                {
                    throw new FrameStructureException(this, "id field must be set");
                }
            }
            IList<Clause> iClauses = GetClauses(OboFormatTag.TAG_INTERSECTION_OF);
            if (iClauses.Count == 1)
            {
                throw new FrameStructureException(this, "single intersection_of tags are not allowed");
            }
            CheckMaxOneCardinality(OboFormatTag.TAG_IS_ANONYMOUS, OboFormatTag.TAG_NAME,
                            // OboFormatTag.TAG_NAMESPACE,
                            OboFormatTag.TAG_DEF, OboFormatTag.TAG_COMMENT,
                            OboFormatTag.TAG_IS_ANTI_SYMMETRIC, OboFormatTag.TAG_IS_CYCLIC,
                            OboFormatTag.TAG_IS_REFLEXIVE, OboFormatTag.TAG_IS_SYMMETRIC,
                            OboFormatTag.TAG_IS_TRANSITIVE, OboFormatTag.TAG_IS_FUNCTIONAL,
                            OboFormatTag.TAG_IS_INVERSE_FUNCTIONAL, OboFormatTag.TAG_IS_OBSELETE,
                            OboFormatTag.TAG_CREATED_BY, OboFormatTag.TAG_CREATION_DATE);
        }

        /**
         * Check max one cardinality.
         *
         * @param tags the tags
         * @throws FrameStructureException frame structure exception
         */
        private void CheckMaxOneCardinality(params OboFormatTag[] tags)
        {
            foreach (OboFormatTag tag in tags)
                if (GetClauses(tag).Count > 1)
                    throw new FrameStructureException(this, "multiple " + tag.Tag + " tags not allowed.");
        }

        /** The Enum FrameType. */
        public enum FrameType
        {
            //@formatter:off
            /** HEADER. */
            HEADER,
            /** TERM. */
            TERM,
            /** TYPEDEF. */
            TYPEDEF,
            /** INSTANCE. */
            INSTANCE,
            /** ANNOTATION. */
            ANNOTATION
            //@formatter:on
        }
    }
}
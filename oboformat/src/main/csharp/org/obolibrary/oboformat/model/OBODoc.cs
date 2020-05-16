#nullable enable

using org.semanticweb.owlapi.util;
using System.Collections.Generic;
using static org.obolibrary.oboformat.parser.OBOFormatConstants;

namespace org.obolibrary.oboformat.model
{

    // import static org.semanticweb.owlapi.util.OWLAPIPreconditions.verifyNotNull;

    // import org.obolibrary.obo2owl.OboInOwlCardinalityTools;
    // import org.obolibrary.oboformat.parser.OBOFormatConstants.OboFormatTag;

    /**
     * An OBODoc is a container for a header frame and zero or more entity frames.
     */
    public class OBODoc
    {

        /**
         * The term frame map.
         */
        protected readonly IDictionary<string, Frame> termFrameMap = new Dictionary<string, Frame>();
        /**
         * The typedef frame map.
         */
        protected readonly IDictionary<string, Frame> typedefFrameMap = new Dictionary<string, Frame>();
        /**
         * The instance frame map.
         */
        protected readonly IDictionary<string, Frame> instanceFrameMap = new Dictionary<string, Frame>();
        /**
         * The annotation frames.
         */
        protected readonly IList<Frame> annotationFrames = new List<Frame>();
        /**
         * The imported obo docs.
         */
        protected readonly List<OBODoc> importedOBODocs = new List<OBODoc>();
        /**
         * The header frame.
         */
        public Frame? HeaderFrame { get; set; }

        private static void FreezeFrameMap(IDictionary<string, Frame> frameMap)
        {
            foreach (Frame frame in frameMap.Values)
                frame.Freeze();
        }

        /**
         * Looks up the ID prefix to IRI prefix mapping. Header-Tag: idspace
         *
         * @param prefix prefix
         * @return IRI prefix as string
         */
        public static string? GetIDSpace(string prefix)
        {
            // built-in
            if (prefix.Equals("RO"))
            {
                return "http://purl.obolibrary.org/obo/RO_";
            }
            // TODO
            return null;
        }

        /**
         * @param prefix the prefix
         * @return true, if is treat xrefs as equivalent
         */
        public static bool IsTreatXrefsAsEquivalent(string? prefix)
        {
            if ("RO".Equals(prefix))
            {
                return true;
            }
            return false;
        }

        /**
         * @return the term frames
         */
        public ICollection<Frame> GetTermFrames()
        {
            return termFrameMap.Values;
        }

        /**
         * @return the typedef frames
         */
        public ICollection<Frame> GetTypedefFrames()
        {
            return typedefFrameMap.Values;
        }

        /**
         * @return the instance frames
         */
        public ICollection<Frame> GetInstanceFrames()
        {
            return instanceFrameMap.Values;
        }

        /**
         * Freezing an OBODoc signals that the document has become quiescent, and
         * that the system may optimize data structures for performance or space.
         */
        public void FreezeFrames()
        {
            OWLAPIPreconditions.VerifyNotNull(HeaderFrame,
                "headerFrame cannot be null at this stage. Setting the headr frame has been skipped")
                .Freeze();
            FreezeFrameMap(termFrameMap);
            FreezeFrameMap(typedefFrameMap);
            FreezeFrameMap(instanceFrameMap);
            foreach (Frame frame in annotationFrames)
            {
                frame.Freeze();
            }
        }

        /**
         * @param id the id
         * @return the term frame
         */
        public Frame? GetTermFrame(string id)
        {
            return GetTermFrame(id, false);
        }

        /**
         * @param id the id
         * @param followImport the follow import
         * @return the term frame
         */
        public Frame? GetTermFrame(string id, bool followImport)
        {
            if (!followImport)
                return termFrameMap[id];
            // this set is used to check for cycles
            return GetTermFrame(id, new HashSet<string> { GetHeaderDescriptor() });
        }

        /**
         * @param id the id
         * @param visitedDocs the visited docs
         * @return the frame
         */
        private Frame? GetTermFrame(string id, ISet<string> visitedDocs)
        {
            Frame? f = termFrameMap[id];
            if (f != null)
                return f;
            foreach (OBODoc doc in importedOBODocs)
            {
                string headerDescriptor = doc.GetHeaderDescriptor();
                if (!visitedDocs.Contains(headerDescriptor))
                {
                    visitedDocs.Add(headerDescriptor);
                    f = doc.GetTermFrame(id, true);
                }
                if (f != null)
                {
                    return f;
                }
            }
            return null;
        }

        /**
         * @param id the id
         * @return the typedef frame
         */
        public Frame? GetTypedefFrame(string id)
        {
            return GetTypedefFrame(id, false);
        }

        /**
         * @param id the id
         * @param followImports the follow imports
         * @return the typedef frame
         */
        public Frame? GetTypedefFrame(string id, bool followImports)
        {
            if (!followImports)
            {
                return typedefFrameMap[id];
            }
            return GetTypedefFrame(id, new HashSet<string> { GetHeaderDescriptor() });
        }

        /**
         * @param id the id
         * @param visitedDocs the visited docs
         * @return the frame
         */
        private Frame? GetTypedefFrame(string id, ISet<string> visitedDocs)
        {
            Frame? f = typedefFrameMap[id];
            if (f != null)
                return f;
            foreach (OBODoc doc in importedOBODocs)
            {
                string headerDescriptor = doc.GetHeaderDescriptor();
                if (!visitedDocs.Contains(headerDescriptor))
                {
                    visitedDocs.Add(headerDescriptor);
                    f = doc.GetTypedefFrame(id, true);
                }
                if (f != null)
                {
                    return f;
                }
            }
            return null;
        }

        /**
         * @param id the id
         * @return the instance frame
         */
        public Frame GetInstanceFrame(string id)
        {
            return instanceFrameMap[id];
        }

        /**
         * @return the imported obo docs
         */
        public ICollection<OBODoc> GetImportedOBODocs()
        {
            return importedOBODocs;
        }

        /**
         * @param importedOBODocs the new imported obo docs
         */
        public void SetImportedOBODocs(ICollection<OBODoc> importedOBODocs)
        {
            this.importedOBODocs.Clear();
            this.importedOBODocs.AddRange(importedOBODocs);
        }

        /**
         * Adds the imported obo doc.
         *
         * @param doc the doc
         */
        public void AddImportedOBODoc(OBODoc doc)
        {
            importedOBODocs.Add(doc);
        }

        /**
         * Adds the frame.
         *
         * @param f the frame
         * @throws FrameMergeException the frame merge exception
         */
        public void AddFrame(Frame f)
        {
            if (f.Type == Frame.FrameType.TERM)
            {
                AddTermFrame(f);
            }
            else if (f.Type == Frame.FrameType.TYPEDEF)
            {
                AddTypedefFrame(f);
            }
            else if (f.Type == Frame.FrameType.INSTANCE)
            {
                AddInstanceFrame(f);
            }
        }

        /**
         * Adds the term frame.
         *
         * @param f the frame
         * @throws FrameMergeException the frame merge exception
         */
        public void AddTermFrame(Frame f)
        {
            string? id = f.Id;
            if (termFrameMap.ContainsKey(id))
            {
                termFrameMap[id].Merge(f);
            }
            else
            {
                termFrameMap[id] = f;
            }
        }

        /**
         * Adds the typedef frame.
         *
         * @param f the frame
         * @throws FrameMergeException the frame merge exception
         */
        public void AddTypedefFrame(Frame f)
        {
            string? id = f.Id;
            if (typedefFrameMap.ContainsKey(id))
            {
                typedefFrameMap[id].Merge(f);
            }
            else
            {
                typedefFrameMap[id] = f;
            }
        }

        /**
         * Adds the instance frame.
         *
         * @param f the frame
         * @throws FrameMergeException the frame merge exception
         */
        public void AddInstanceFrame(Frame f)
        {
            string? id = f.Id;
            if (instanceFrameMap.ContainsKey(id))
            {
                instanceFrameMap[id].Merge(f);
            }
            else
            {
                instanceFrameMap[id] = f;
            }
        }

        /**
         * Merge contents.
         *
         * @param extDoc the external doc
         * @throws FrameMergeException the frame merge exception
         */
        public void MergeContents(OBODoc extDoc)
        {
            foreach (Frame f in extDoc.GetTermFrames())
            {
                AddTermFrame(f);
            }
            foreach (Frame f in extDoc.GetTypedefFrames())
            {
                AddTypedefFrame(f);
            }
            foreach (Frame f in extDoc.GetInstanceFrames())
            {
                AddInstanceFrame(f);
            }
        }

        /**
         * Adds the default ontology header.
         *
         * @param defaultOnt the default ont
         */
        public void AddDefaultOntologyHeader(string defaultOnt)
        {
            Frame hf = OWLAPIPreconditions.VerifyNotNull(HeaderFrame);
            Clause? ontClause = hf.GetClause(OboFormatTag.TAG_ONTOLOGY);
            if (ontClause == null)
            {
                ontClause = new Clause(OboFormatTag.TAG_ONTOLOGY, defaultOnt);
                hf.AddClause(ontClause);
            }
        }

        /**
         * Check this document for violations, i.e. cardinality constraint
         * violations.
         *
         * @see OboInOwlCardinalityTools for equivalent checks in OWL
         */
        public void Check()
        {
            OWLAPIPreconditions.VerifyNotNull(HeaderFrame).Check();
            foreach (Frame f in GetTermFrames())
            {
                f.Check();
            }
            foreach (Frame f in GetTypedefFrames())
            {
                f.Check();
            }
            foreach (Frame f in GetInstanceFrames())
            {
                f.Check();
            }
        }

        override public string ToString()
        {
            return GetHeaderDescriptor();
        }

        /**
         * @return the header descriptor
         */
        private string GetHeaderDescriptor()
        {
            return "OBODoc(" + HeaderFrame + ')';
        }
    }
}

#nullable enable

using com.melandra.Utilities;
using NLog;
using org.obolibrary.oboformat.model;
using org.semanticweb.owlapi.util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using static org.obolibrary.oboformat.model.Frame;
using static org.obolibrary.oboformat.parser.OBOFormatConstants;

namespace org.obolibrary.oboformat.parser
{
    // import com.github.benmanes.caffeine.cache.Caffeine;
    // import com.github.benmanes.caffeine.cache.LoadingCache;

    /**
     * Implements the OBO Format 1.4 specification.
     */
    public class OBOFormatParser
    {

        private const string BRACE = " !{";
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        protected readonly MyStream stream;
        // private readonly LoadingCache<string, string> stringCache;
        public bool FollowImports { get; set; }
        private object? location;
        private readonly IDictionary<string, OBODoc> importCache = new ConcurrentDictionary<string, OBODoc>();

        public OBOFormatParser()
            : this(new MyStream(), new Dictionary<string, OBODoc>())
        {
        }

        /**
         * @param importsMap import map
         */
        public OBOFormatParser(IDictionary<string, OBODoc> importsMap)
            : this(new MyStream(), importsMap)
        {
        }

        /**
         * @param s input stream
         * @param importsMap import map
         */
        protected OBOFormatParser(MyStream s, IDictionary<string, OBODoc> importsMap)
        {
            stream = s;
            importCache.AddRange(importsMap);
            /*
            Caffeine<string, string> builder = Caffeine.newBuilder()
                .maximumWeight(8388608)
                .weigher((string key, string value)=>key.length());
            if (LOGGER.IsDebugEnabled)
                builder.recordStats();
            stringCache = builder.build(key => key);
            */
        }

        private static void AddOboNamespace(ICollection<Frame>? frames, string defaultOboNamespace)
        {
            if (frames != null)
            {
                foreach (Frame termFrame in frames)
                {
                    Clause? clause = termFrame.GetClause(OboFormatTag.TAG_NAMESPACE);
                    if (clause == null)
                    {
                        clause = new Clause(OboFormatTag.TAG_NAMESPACE, defaultOboNamespace);
                        termFrame.AddClause(clause);
                    }
                }
            }
        }

        private static string MapDeprecatedTag(string tag)
        {
            if ("inverse_of_on_instance_level".Equals(tag))
            {
                return OboFormatTag.TAG_INVERSE_OF.Tag;
            }
            if ("xref_analog".Equals(tag))
            {
                return OboFormatTag.TAG_XREF.Tag;
            }
            if ("xref_unknown".Equals(tag))
            {
                return OboFormatTag.TAG_XREF.Tag;
            }
            if ("instance_level_is_transitive".Equals(tag))
            {
                return OboFormatTag.TAG_IS_TRANSITIVE.Tag;
            }
            return tag;
        }

        private static string RemoveTrailingWS(string s)
        {
            return Regex.Replace(s, "\\s*$", "");
        }

        /**
         * @param key key for the import
         * @param doc document
         * @return true if the key is new
         */
        public bool AddImport(string key, OBODoc doc)
        {
            bool exists = importCache.ContainsKey(key);
            importCache[key] = doc;
            return !exists;
        }

        /**
         * @param r r
         */
        public void SetReader(TextReader r)
        {
            stream.Reader = r;
        }

        /**
         * Parses a local file or URL to an OBODoc.
         *
         * @param fn fn
         * @return parsed obo document
         * @throws IOException io exception
         * @throws OBOFormatParserException parser exception
         */
        public OBODoc Parse(string fn)
        {
            return IsUrlIsh(fn)
                ? Parse(new Uri(fn))
                : Parse(new PathHolder(fn));
        }

        /**
         * Parses a local file to an OBODoc.
         *
         * @param file file
         * @return parsed obo document
         * @throws IOException io exception
         * @throws OBOFormatParserException parser exception
         */
        public OBODoc Parse(PathHolder file)
        {
            location = file;
            using Stream f = new FileStream(file.Path, FileMode.Open, FileAccess.Read);
            using TextReader in2 = new StreamReader(f, Encoding.UTF8);
            return Parse(in2);
        }

        /**
         * Parses a remote URL to an OBODoc.
         *
         * @param url url
         * @return parsed obo document
         * @throws IOException io exception
         * @throws OBOFormatParserException parser exception
         */
        public OBODoc Parse(Uri url)
        {
            location = url;
            WebClient client = new WebClient();
            return Parse(new StreamReader(client.OpenRead(url), Encoding.UTF8));
        }

        /**
         * Parses a remote URL to an OBODoc.
         *
         * @param urlstr urlstr
         * @return parsed obo document
         * @throws IOException io exception
         * @throws OBOFormatParserException parser exception
         */
        public OBODoc ParseURL(string urlstr) => Parse(new Uri(urlstr));

        // ----------------------------------------
        // GRAMMAR
        // ----------------------------------------

        private string ResolvePath(string inputPath)
        {
            string path = inputPath;
            if (!(path.StartsWith("http:") || path.StartsWith("file:") || path.StartsWith("https:")))
            {
                // path is not absolute then guess it.
                if (location != null)
                {
                    if (location is Uri)
                    {
                        // Easy: treat as a relative URI.
                        return new Uri((Uri)location, inputPath).AbsoluteUri;
                    }
                    else
                    {
                        PathHolder f = new PathHolder(location.ToString());
                        f = f.Directory.Combine(path);
                        return new Uri(f.Path).ToString();
                    }
                }
            }
            return path;
        }

        private bool IsUrlIsh(string path)
        {
            return path.StartsWith("http:") || path.StartsWith("file:") || path.StartsWith("https:");
        }

        /**
         * @param reader reader
         * @return parsed obo document
         * @throws IOException io exception
         * @throws OBOFormatParserException parser exception
         */
        public OBODoc Parse(TextReader reader)
        {
            SetReader(reader);
            OBODoc obodoc = new OBODoc();
            ParseOBODoc(obodoc);
            // handle imports
            Frame? hf = obodoc.HeaderFrame;
            IList<OBODoc> imports = new List<OBODoc>();
            if (hf != null)
            {
                foreach (Clause cl in hf.GetClauses(OboFormatTag.TAG_IMPORT))
                {
                    string path = ResolvePath(cl.Value<string>());
                    // TBD -- changing the relative path to absolute
                    cl.SetValue(path);
                    if (FollowImports)
                    {
                        // resolve OboDoc documents from import paths.
                        if (!importCache.TryGetValue(path, out OBODoc doc))
                        {
                            OBOFormatParser parser = new OBOFormatParser(importCache);
                            doc = parser.ParseURL(path);
                        }
                        imports.Add(doc);
                    }
                }
                obodoc.SetImportedOBODocs(imports);
            }
            return obodoc;
        }

        /**
         * @param obodoc obodoc
         * @throws OBOFormatParserException parser exception
         */
        public void ParseOBODoc(OBODoc obodoc)
        {
            Frame h = new Frame(FrameType.HEADER);
            obodoc.HeaderFrame = h;
            ParseHeaderFrame(h);
            h.Freeze();
            ParseZeroOrMoreWsOptCmtNl();
            while (!stream.Eof())
            {
                ParseEntityFrame(obodoc);
                ParseZeroOrMoreWsOptCmtNl();
            }
            // set OBO namespace in frames
            string? defaultOboNamespace = h.GetTagValue<string>(OboFormatTag.TAG_DEFAULT_NAMESPACE);
            if (defaultOboNamespace != null)
            {
                AddOboNamespace(obodoc.GetTermFrames(), defaultOboNamespace);
                AddOboNamespace(obodoc.GetTypedefFrames(), defaultOboNamespace);
                AddOboNamespace(obodoc.GetInstanceFrames(), defaultOboNamespace);
            }
        }

        /**
         * @param doc doc
         * @return list of references
         * @throws OBOFormatDanglingReferenceException dangling reference error
         */
        public IList<string> CheckDanglingReferences(OBODoc doc)
        {
            IList<string> danglingReferences = new List<string>();
            // check term frames
            foreach (Frame f in doc.GetTermFrames())
            {
                foreach (string tag in f.GetTags())
                {
                    OboFormatTag? tagconstant = GetTag(tag);
                    Clause? c = f.GetClause(tag);
                    Validate(doc, danglingReferences, f, tag, tagconstant, c);
                }
            }
            // check typedef frames
            foreach (Frame f in doc.GetTypedefFrames())
            {
                foreach (string tag in f.GetTags())
                {
                    OboFormatTag? tagConstant = GetTag(tag);
                    Clause? c = f.GetClause(tag);
                    Validate1(doc, danglingReferences, f, tag, tagConstant, c);
                }
            }
            return danglingReferences;
        }

        protected void Validate1(OBODoc doc, IList<string> danglingReferences, Frame f, string tag, OboFormatTag? tagConstant, Clause? c)
        {
            if (c != null && tagConstant != null)
            {
                if (OboFormatTag.TYPEDEF_FRAMES.Contains(tagConstant))
                {
                    string? error = CheckRelation(c.Value<string>(), tag, f.Id, doc);
                    if (error != null)
                    {
                        danglingReferences.Add(error);
                    }
                }
                else if (tagConstant == OboFormatTag.TAG_HOLDS_OVER_CHAIN
                  || tagConstant == OboFormatTag.TAG_EQUIVALENT_TO_CHAIN
                  || tagConstant == OboFormatTag.TAG_RELATIONSHIP)
                {
                    string? error = CheckRelation(c.Value<string>(), tag, f.Id, doc);
                    if (error != null)
                    {
                        danglingReferences.Add(error);
                    }
                    error = CheckRelation(c.Value2<string>(), tag, f.Id, doc);
                    if (error != null)
                    {
                        danglingReferences.Add(error);
                    }
                }
                else if (tagConstant == OboFormatTag.TAG_DOMAIN
                  || tagConstant == OboFormatTag.TAG_RANGE)
                {
                    string? error = CheckClassReference(c.Value<string>(), tag, f.Id, doc);
                    if (error != null)
                    {
                        danglingReferences.Add(error);
                    }
                }
            }
        }

        protected void Validate(OBODoc doc, IList<string> danglingReferences, Frame f, string tag, OboFormatTag? tagconstant, Clause? c)
        {
            if (c != null && OboFormatTag.TERM_FRAMES.Contains(tagconstant))
            {
                if (c.Values.Count > 1)
                {
                    string? error = CheckRelation(c.Value<string>(), tag, f.Id, doc);
                    if (error != null)
                    {
                        danglingReferences.Add(error);
                    }
                    error = CheckClassReference(c.Value2<string>(), tag, f.Id, doc);
                    if (error != null)
                    {
                        danglingReferences.Add(error);
                    }
                }
                else
                {
                    string? error = CheckClassReference(c.Value<string>(), tag, f.Id, doc);
                    if (error != null)
                    {
                        danglingReferences.Add(error);
                    }
                }
            }
        }

        private string? CheckRelation(string relId, string tag, string? frameId, OBODoc doc)
        {
            if (doc.GetTypedefFrame(relId, FollowImports) == null)
            {
                return "The relation '" + relId + "' reference in" + " the tag '" + tag
                    + " ' in the frame of id '" + frameId + "' is not declared";
            }
            return null;
        }

        private string? CheckClassReference(string classId, string tag, string? frameId, OBODoc doc)
        {
            if (doc.GetTermFrame(classId, FollowImports) == null)
            {
                return "The class '" + classId + "' reference in" + " the tag '" + tag
                    + " ' in the frame of id '" + frameId + "'is not declared";
            }
            return null;
        }

        /**
         * @param h h
         * @throws OBOFormatParserException parser exception
         */
        public void ParseHeaderFrame(Frame h)
        {
            while (ParseHeaderClauseNl(h))
            {
                // repeat while more available
            }
        }

        /**
         * header-clause ::= format-version-TVP | ... | ...
         *
         * @param h header frame
         * @return false if there are no more header clauses, other wise true
         * @throws OBOFormatParserException parser exception
         */
        protected bool ParseHeaderClauseNl(Frame h)
        {
            ParseZeroOrMoreWsOptCmtNl();
            if (stream.PeekCharIs('[') || stream.Eof())
            {
                return false;
            }
            ParseHeaderClause(h);
            ParseHiddenComment();
            ForceParseNlOrEof();
            return true;
        }

        protected Clause ParseHeaderClause(Frame h)
        {
            string t = GetParseTag();
            Clause cl = new Clause(t);
            OboFormatTag? tag = GetTag(t);
            h.AddClause(cl);
            if (tag == null)
            {
                return ParseUnquotedString(cl);
            }
            switch (tag.innerEnumValue)
            {
                case OboFormatTag.InnerEnum.TAG_SYNONYMTYPEDEF:
                    return ParseSynonymTypedef(cl);
                case OboFormatTag.InnerEnum.TAG_SUBSETDEF:
                    return ParseSubsetdef(cl);
                case OboFormatTag.InnerEnum.TAG_DATE:
                    return ParseHeaderDate(cl);
                case OboFormatTag.InnerEnum.TAG_PROPERTY_VALUE:
                    ParsePropertyValue(cl);
                    return ParseQualifierAndHiddenComment(cl);
                case OboFormatTag.InnerEnum.TAG_IMPORT:
                    return ParseImport(cl);
                case OboFormatTag.InnerEnum.TAG_IDSPACE:
                    return ParseIdSpace(cl);
                // $CASES-OMITTED$
                default:
                    return ParseUnquotedString(cl);
            }
        }

        protected Clause ParseQualifierAndHiddenComment(Clause cl)
        {
            ParseZeroOrMoreWs();
            ParseQualifierBlock(cl);
            ParseHiddenComment();
            return cl;
        }

        // ----------------------------------------
        // [Term] Frames
        // ----------------------------------------

        /**
         * @param obodoc obodoc
         * @throws OBOFormatParserException parser exception
         */
        public void ParseEntityFrame(OBODoc obodoc)
        {
            ParseZeroOrMoreWsOptCmtNl();
            string rest = stream.Rest();
            if (rest.StartsWith("[Term]"))
            {
                ParseTermFrame(obodoc);
            }
            else if (rest.StartsWith("[Instance]"))
            {
                LOGGER.Error("Error: Instance frames are not supported yet. Parsing stopped at line: {0}", stream.LineNo);
                while (!stream.Eof())
                {
                    stream.AdvanceLine();
                }
            }
            else
            {
                ParseTypedefFrame(obodoc);
            }
        }

        /**
         * term-frame ::= nl* '[Term]' nl id-Tag Class-ID EOL { term-frame-clause EOL }.
         *
         * @param obodoc obodoc
         * @throws OBOFormatParserException parser exception
         */
        public void ParseTermFrame(OBODoc obodoc)
        {
            Frame f = new Frame(FrameType.TERM);
            ParseZeroOrMoreWsOptCmtNl();
            if (stream.Consume("[Term]"))
            {
                ForceParseNlOrEof();
                ParseIdLine(f);
                ParseZeroOrMoreWsOptCmtNl();
                while (true)
                {
                    if (stream.Eof() || stream.PeekCharIs('['))
                    {
                        // reached end of file or new stanza
                        break;
                    }
                    ParseTermFrameClauseEOL(f);
                    ParseZeroOrMoreWsOptCmtNl();
                }
                try
                {
                    f.Freeze();
                    obodoc.AddFrame(f);
                }
                catch (FrameMergeException e)
                {
                    throw new OBOFormatParserException(
                        "Could not add frame " + f + " to document, duplicate frame definition?", e,
                        stream.LineNo, stream.Line());
                }
            }
            else
            {
                Error("Expected a [Term] frame, but found unknown stanza type.");
            }
        }

        /**
         * @param f f
         * @throws OBOFormatParserException parser exception
         */
        protected void ParseTermFrameClauseEOL(Frame f)
        {
            // comment line:
            if (stream.PeekCharIs('!'))
            {
                ParseHiddenComment();
                ForceParseNlOrEof();
            }
            else
            {
                Clause cl = ParseTermFrameClause();
                ParseEOL(cl);
                f.AddClause(cl);
            }
        }

        // ----------------------------------------
        // [Typedef] Frames
        // ----------------------------------------

        /**
         * @return parsed clause
         * @throws OBOFormatParserException parser exception
         */
        public Clause ParseTermFrameClause()
        {
            string t = GetParseTag();
            Clause cl = new Clause(t);
            if (ParseDeprecatedSynonym(t, cl))
            {
                return cl;
            }
            OboFormatTag? tag = GetTag(t);
            if (tag == null)
            {
                // Treat unexpected tags as custom tags
                return ParseCustomTag(cl);
            }
            switch (tag.innerEnumValue)
            {
                case OboFormatTag.InnerEnum.TAG_IS_ANONYMOUS:
                case OboFormatTag.InnerEnum.TAG_BUILTIN:
                case OboFormatTag.InnerEnum.TAG_IS_OBSELETE:
                    return ParseBoolean(cl);
                case OboFormatTag.InnerEnum.TAG_NAME:
                case OboFormatTag.InnerEnum.TAG_COMMENT:
                case OboFormatTag.InnerEnum.TAG_CREATED_BY:
                    return ParseUnquotedString(cl);
                case OboFormatTag.InnerEnum.TAG_NAMESPACE:
                case OboFormatTag.InnerEnum.TAG_ALT_ID:
                case OboFormatTag.InnerEnum.TAG_IS_A:
                case OboFormatTag.InnerEnum.TAG_UNION_OF:
                case OboFormatTag.InnerEnum.TAG_EQUIVALENT_TO:
                case OboFormatTag.InnerEnum.TAG_DISJOINT_FROM:
                case OboFormatTag.InnerEnum.TAG_REPLACED_BY:
                case OboFormatTag.InnerEnum.TAG_CONSIDER:
                    return ParseIdRef(cl);
                case OboFormatTag.InnerEnum.TAG_DEF:
                    return ParseDef(cl);
                case OboFormatTag.InnerEnum.TAG_SUBSET:
                    // in the obof1.4 spec, subsets may not contain spaces.
                    // unfortunately OE does not prohibit this, so subsets with spaces
                    // frequently escape. We should either allow spaces in the spec
                    // (which complicates parsing) or forbid them and reject all obo
                    // documents that do not conform. Unfortunately that would limit
                    // the utility of this parser, so for now we allow spaces. We may
                    // make it strict again when community is sufficiently forewarned.
                    // (alternatively we may add smarts to OE to translate the spaces to
                    // underscores, so it's a one-off translation)
                    return ParseUnquotedString(cl);
                case OboFormatTag.InnerEnum.TAG_SYNONYM:
                    return ParseSynonym(cl);
                case OboFormatTag.InnerEnum.TAG_XREF:
                    return ParseDirectXref(cl);
                case OboFormatTag.InnerEnum.TAG_PROPERTY_VALUE:
                    return ParsePropertyValue(cl);
                case OboFormatTag.InnerEnum.TAG_INTERSECTION_OF:
                    return ParseTermIntersectionOf(cl);
                case OboFormatTag.InnerEnum.TAG_RELATIONSHIP:
                    return ParseRelationship(cl);
                case OboFormatTag.InnerEnum.TAG_CREATION_DATE:
                    return ParseISODate(cl);
                // $CASES-OMITTED$
                default:
                    // Treat unexpected tags as custom tags
                    return ParseCustomTag(cl);
            }
        }

        /**
         * Typedef-frame ::= nl* '[Typedef]' nl id-Tag Class-ID EOL { Typedef-frame-clause EOL }.
         *
         * @param obodoc obodoc
         * @throws OBOFormatParserException parser exception
         */
        public void ParseTypedefFrame(OBODoc obodoc)
        {
            Frame f = new Frame(FrameType.TYPEDEF);
            ParseZeroOrMoreWsOptCmtNl();
            if (stream.Consume("[Typedef]"))
            {
                ForceParseNlOrEof();
                ParseIdLine(f);
                ParseZeroOrMoreWsOptCmtNl();
                while (true)
                {
                    if (stream.Eof() || stream.PeekCharIs('['))
                    {
                        // reached end of file or new stanza
                        break;
                    }
                    ParseTypedefFrameClauseEOL(f);
                    ParseZeroOrMoreWsOptCmtNl();
                }
                try
                {
                    f.Freeze();
                    obodoc.AddFrame(f);
                }
                catch (FrameMergeException e)
                {
                    throw new OBOFormatParserException(
                        "Could not add frame " + f + " to document, duplicate frame definition?", e,
                        stream.LineNo, stream.Line());
                }
            }
            else
            {
                Error("Expected a [Typedef] frame, but found unknown stanza type.");
            }
        }

        /**
         * @param f f
         * @throws OBOFormatParserException parser exception
         */
        protected void ParseTypedefFrameClauseEOL(Frame f)
        {
            // comment line:
            if (stream.PeekCharIs('!'))
            {
                ParseHiddenComment();
                ForceParseNlOrEof();
            }
            else
            {
                Clause cl = ParseTypedefFrameClause();
                ParseEOL(cl);
                f.AddClause(cl);
            }
        }

        /**
         * @return parsed clause
         * @throws OBOFormatParserException parser exception
         */
        public Clause ParseTypedefFrameClause()
        {
            string t = GetParseTag();
            if ("is_metadata".Equals(t))
            {
                LOGGER.Info("is_metadata DEPRECATED; switching to is_metadata_tag");
                t = OboFormatTag.TAG_IS_METADATA_TAG.Tag;
            }
            Clause cl = new Clause(t);
            if (ParseDeprecatedSynonym(t, cl))
            {
                return cl;
            }
            OboFormatTag? tag = GetTag(t);
            if (tag == null)
            {
                // Treat unexpected tags as custom tags
                return ParseCustomTag(cl);
            }
            switch (tag.innerEnumValue)
            {
                case OboFormatTag.InnerEnum.TAG_IS_ANONYMOUS:
                case OboFormatTag.InnerEnum.TAG_BUILTIN:
                case OboFormatTag.InnerEnum.TAG_IS_OBSELETE:
                case OboFormatTag.InnerEnum.TAG_IS_ANTI_SYMMETRIC:
                case OboFormatTag.InnerEnum.TAG_IS_CYCLIC:
                case OboFormatTag.InnerEnum.TAG_IS_REFLEXIVE:
                case OboFormatTag.InnerEnum.TAG_IS_SYMMETRIC:
                case OboFormatTag.InnerEnum.TAG_IS_ASYMMETRIC:
                case OboFormatTag.InnerEnum.TAG_IS_TRANSITIVE:
                case OboFormatTag.InnerEnum.TAG_IS_FUNCTIONAL:
                case OboFormatTag.InnerEnum.TAG_IS_INVERSE_FUNCTIONAL:
                case OboFormatTag.InnerEnum.TAG_IS_METADATA_TAG:
                case OboFormatTag.InnerEnum.TAG_IS_CLASS_LEVEL_TAG:
                    return ParseBoolean(cl);
                case OboFormatTag.InnerEnum.TAG_NAME:
                case OboFormatTag.InnerEnum.TAG_COMMENT:
                case OboFormatTag.InnerEnum.TAG_CREATED_BY:
                    return ParseUnquotedString(cl);
                case OboFormatTag.InnerEnum.TAG_NAMESPACE:
                case OboFormatTag.InnerEnum.TAG_ALT_ID:
                case OboFormatTag.InnerEnum.TAG_SUBSET:
                case OboFormatTag.InnerEnum.TAG_IS_A:
                case OboFormatTag.InnerEnum.TAG_UNION_OF:
                case OboFormatTag.InnerEnum.TAG_EQUIVALENT_TO:
                case OboFormatTag.InnerEnum.TAG_DISJOINT_FROM:
                case OboFormatTag.InnerEnum.TAG_REPLACED_BY:
                case OboFormatTag.InnerEnum.TAG_CONSIDER:
                case OboFormatTag.InnerEnum.TAG_INVERSE_OF:
                case OboFormatTag.InnerEnum.TAG_TRANSITIVE_OVER:
                case OboFormatTag.InnerEnum.TAG_DISJOINT_OVER:
                case OboFormatTag.InnerEnum.TAG_DOMAIN:
                case OboFormatTag.InnerEnum.TAG_RANGE:
                    return ParseIdRef(cl);
                case OboFormatTag.InnerEnum.TAG_DEF:
                    return ParseDef(cl);
                case OboFormatTag.InnerEnum.TAG_SYNONYM:
                    return ParseSynonym(cl);
                case OboFormatTag.InnerEnum.TAG_XREF:
                    return ParseDirectXref(cl);
                case OboFormatTag.InnerEnum.TAG_PROPERTY_VALUE:
                    return ParsePropertyValue(cl);
                case OboFormatTag.InnerEnum.TAG_INTERSECTION_OF:
                    return ParseTypedefIntersectionOf(cl);
                case OboFormatTag.InnerEnum.TAG_RELATIONSHIP:
                    return ParseRelationship(cl);
                case OboFormatTag.InnerEnum.TAG_CREATION_DATE:
                    return ParseISODate(cl);
                case OboFormatTag.InnerEnum.TAG_HOLDS_OVER_CHAIN:
                case OboFormatTag.InnerEnum.TAG_EQUIVALENT_TO_CHAIN:
                    return ParseIdRefPair(cl);
                case OboFormatTag.InnerEnum.TAG_EXPAND_ASSERTION_TO:
                case OboFormatTag.InnerEnum.TAG_EXPAND_EXPRESSION_TO:
                    return ParseOwlDef(cl);
                // $CASES-OMITTED$
                default:
                    // Treat unexpected tags as custom tags
                    return ParseCustomTag(cl);
            }
        }

        // ----------------------------------------
        // [Instance] Frames - TODO
        // ----------------------------------------
        // ----------------------------------------
        // TVP
        // ----------------------------------------
        private string GetParseTag()
        {
            if (stream.Eof())
            {
                Error("Expected an id tag, not end of file.");
            }
            if (stream.Eol())
            {
                Error("Expected an id tag, not end of line");
            }
            int i = stream.IndexOf(':');
            if (i == -1)
            {
                Error("Could not find tag separator ':' in line.");
            }
            string tag = stream.Rest().Substring(0, i);
            stream.Advance(i + 1);
            ParseWs();
            ParseZeroOrMoreWs();
            // Memory optimization
            // re-use the tag string
            OboFormatTag? formatTag = GetTag(tag);
            if (formatTag != null)
            {
                tag = formatTag.Tag;
            }
            return MapDeprecatedTag(tag);
        }

        private Clause ParseIdRef(Clause cl)
        {
            return ParseIdRef(cl, false);
        }

        private Clause ParseIdRef(Clause cl, bool optional)
        {
            string id = GetParseUntil(BRACE);
            if (!optional && id.Length < 1)
            {
                Error("");
            }
            cl.AddValue(id);
            return cl;
        }

        private Clause ParseIdRefPair(Clause cl)
        {
            ParseIdRef(cl);
            ParseOneOrMoreWs();
            return ParseIdRef(cl);
        }

        private Clause ParseISODate(Clause cl)
        {
            string dateStr = GetParseUntil(BRACE);
            cl.SetValue(dateStr);
            return cl;
        }

        private Clause ParseSubsetdef(Clause cl)
        {
            ParseIdRef(cl);
            ParseOneOrMoreWs();
            if (stream.Consume("\""))
            {
                string desc = GetParseUntilAdv("\"");
                cl.AddValue(desc);
            }
            else
            {
                Error("");
            }
            return ParseQualifierAndHiddenComment(cl);
        }

        private Clause ParseSynonymTypedef(Clause cl)
        {
            ParseIdRef(cl);
            ParseOneOrMoreWs();
            if (stream.Consume("\""))
            {
                string desc = GetParseUntilAdv("\"");
                cl.AddValue(desc);
                // TODO: handle edge case where line ends with trailing whitespace
                // and no scope
                if (stream.PeekCharIs(' '))
                {
                    ParseOneOrMoreWs();
                    ParseIdRef(cl, true);
                    // TODO - verify that this is a valid scope
                }
            }
            return ParseQualifierAndHiddenComment(cl);
        }

        private Clause ParseHeaderDate(Clause cl)
        {
            ParseZeroOrMoreWs();
            string v = GetParseUntil("!");
            v = RemoveTrailingWS(v);
            if (!DateTime.TryParseExact(v, OBOFormatConstants.HeaderDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                throw new OBOFormatParserException("Could not parse date from string: " + v, stream.LineNo, stream.Line());
            cl.AddValue(date);
            return cl;
        }

        private Clause ParseImport(Clause cl)
        {
            ParseZeroOrMoreWs();
            string v = GetParseUntil("!{");
            v = RemoveTrailingWS(v);
            cl.SetValue(v);
            // parse and ignore annotations for import statements
            ParseZeroOrMoreWs();
            if (stream.PeekCharIs('{'))
            {
                // do noy parse trailing qualifiers.
                GetParseUntilAdv("}");
            }
            ParseHiddenComment();// ignore return value, as comments are optional
            return cl;
        }

        private Clause ParseIdSpace(Clause cl)
        {
            ParseZeroOrMoreWs();
            ParseIdRefPair(cl);
            ParseZeroOrMoreWs();
            if (stream.PeekCharIs('"'))
            {
                stream.Consume("\"");
                string desc = GetParseUntilAdv("\"");
                cl.AddValue(desc);
            }
            else
            {
                string desc = GetParseUntil(BRACE);
                cl.AddValue(desc);
            }
            return ParseQualifierAndHiddenComment(cl);
        }

        private Clause ParseRelationship(Clause cl)
        {
            ParseIdRef(cl);
            ParseOneOrMoreWs();
            return ParseIdRef(cl);
        }

        private Clause ParsePropertyValue(Clause cl)
        {
            // parse a pair or triple
            // the first and second value, may be quoted strings
            if (stream.PeekCharIs('\"'))
            {
                stream.Consume("\"");
                string desc = GetParseUntilAdv("\"");
                cl.AddValue(desc);
            }
            else
            {
                ParseIdRef(cl);
            }
            ParseOneOrMoreWs();
            if (stream.PeekCharIs('\"'))
            {
                stream.Consume("\"");
                string desc = GetParseUntilAdv("\"");
                cl.AddValue(desc);
            }
            else
            {
                ParseIdRef(cl);
            }
            // check if there is a third value to parse
            ParseZeroOrMoreWs();
            if (stream.PeekCharIs('\"'))
            {
                stream.Consume("\"");
                string desc = GetParseUntilAdv("\"");
                cl.AddValue(desc);
            }
            else
            {
                string s = GetParseUntil(BRACE);
                if (!string.IsNullOrWhiteSpace(s))
                {
                    cl.AddValue(s);
                }
            }
            return cl;
        }

        /**
         * @param cl cl
         * @throws OBOFormatParserException parser exception
         * @return {@code intersection_of-Tag Class-ID | intersection_of-Tag Relation-ID Class-ID}
         */
        private Clause ParseTermIntersectionOf(Clause cl)
        {
            ParseIdRef(cl);
            // consumed the first ID
            ParseZeroOrMoreWs();
            if (!stream.Eol())
            {
                char c = stream.PeekChar();
                if (c != '!' && c != '{')
                {
                    // try to consume the second id
                    ParseIdRef(cl, true);
                }
            }
            return cl;
        }

        private Clause ParseTypedefIntersectionOf(Clause cl)
        {
            // single values only
            return ParseIdRef(cl);
        }

        // ----------------------------------------
        // Synonyms
        // ----------------------------------------
        private bool ParseDeprecatedSynonym(string tag, Clause cl)
        {
            string scope;
            if ("exact_synonym".Equals(tag))
            {
                scope = OboFormatTag.TAG_EXACT.Tag;
            }
            else if ("narrow_synonym".Equals(tag))
            {
                scope = OboFormatTag.TAG_NARROW.Tag;
            }
            else if ("broad_synonym".Equals(tag))
            {
                scope = OboFormatTag.TAG_BROAD.Tag;
            }
            else if ("related_synonym".Equals(tag))
            {
                scope = OboFormatTag.TAG_RELATED.Tag;
            }
            else
            {
                return false;
            }
            cl.Tag = OboFormatTag.TAG_SYNONYM.Tag;
            if (stream.Consume("\""))
            {
                string syn = GetParseUntilAdv("\"");
                cl.SetValue(syn);
                cl.AddValue(scope);
                ParseZeroOrMoreWs();
                ParseXrefList(cl, false);
                return true;
            }
            return false;
        }

        private Clause ParseSynonym(Clause cl)
        {
            if (stream.Consume("\""))
            {
                string syn = GetParseUntilAdv("\"");
                cl.SetValue(syn);
                ParseZeroOrMoreWs();
                if (!stream.PeekCharIs('['))
                {
                    ParseIdRef(cl, true);
                    ParseZeroOrMoreWs();
                    if (!stream.PeekCharIs('['))
                    {
                        ParseIdRef(cl, true);
                        ParseZeroOrMoreWs();
                    }
                }
                ParseXrefList(cl, false);
            }
            else
            {
                Error("The synonym is always a quoted string.");
            }
            return cl;
        }

        // ----------------------------------------
        // Definitions
        // ----------------------------------------
        private Clause ParseDef(Clause cl)
        {
            if (stream.Consume("\""))
            {
                string def = GetParseUntilAdv("\"");
                cl.SetValue(def);
                ParseZeroOrMoreWs();
                ParseXrefList(cl, true);
            }
            else
            {
                Error("Definitions should always be a quoted string.");
            }
            return cl;
        }

        private Clause ParseOwlDef(Clause cl)
        {
            if (stream.Consume("\""))
            {
                string def = GetParseUntilAdv("\"");
                cl.SetValue(def);
                ParseZeroOrMoreWs();
                ParseXrefList(cl, true);
            }
            else
            {
                Error("The " + cl.Tag + " clause is always a quoted string.");
            }
            return cl;
        }

        // ----------------------------------------
        // XrefLists - e.g. [A:1, B:2, ... ]
        // ----------------------------------------
        private void ParseXrefList(Clause cl, bool optional)
        {
            if (stream.Consume("["))
            {
                ParseZeroOrMoreXrefs(cl);
                ParseZeroOrMoreWs();
                if (!stream.Consume("]"))
                {
                    Error("Missing closing ']' for xref list at pos: " + stream.Pos);
                }
            }
            else if (!optional)
            {
                Error("Clause: " + cl.Tag
                    + "; expected an xref list, or at least an empty list '[]' at pos: " + stream.Pos);
            }
        }

        private bool ParseZeroOrMoreXrefs(Clause cl)
        {
            if (ParseXref(cl))
            {
                while (stream.Consume(",") && ParseXref(cl))
                {
                    // repeat while more available
                }
            }
            return true;
        }

        // an xref that supports a value of values in a clause
        private bool ParseXref(Clause cl)
        {
            ParseZeroOrMoreWs();
            string id = GetParseUntil("\",]!{", true);
            if (!string.IsNullOrWhiteSpace(id))
            {
                id = RemoveTrailingWS(id);
                if (id.Contains(" "))
                {
                    Warn("accepting bad xref with spaces:" + id);
                }
                Xref xref = new Xref(id);
                cl.AddXref(xref);
                ParseZeroOrMoreWs();
                if (stream.PeekCharIs('"'))
                {
                    stream.Consume("\"");
                    xref.Annotation = GetParseUntilAdv("\"");
                }
                ParseZeroOrMoreWs();
                ParseQualifierBlock(cl);
                return true;
            }
            return false;
        }

        // an xref that is a direct value of a clause
        private Clause ParseDirectXref(Clause cl)
        {
            ParseZeroOrMoreWs();
            string id = GetParseUntil("\",]!{", true);
            id = id.Trim();
            if (id.Contains(" "))
            {
                Warn("accepting bad xref with spaces:<" + id + '>');
            }
            id = Regex.Replace(id, " +\\Z", "");
            Xref xref = new Xref(id);
            cl.AddValue(xref);
            ParseZeroOrMoreWs();
            if (stream.PeekCharIs('"'))
            {
                stream.Consume("\"");
                xref.Annotation = GetParseUntilAdv("\"");
            }
            ParseZeroOrMoreWs();
            ParseQualifierBlock(cl);
            return cl;
        }

        /**
         * Qualifier Value blocks - e.g. {a="1",b="foo", ...}
         *
         * @param cl clause
         */
        private void ParseQualifierBlock(Clause cl)
        {
            if (stream.Consume("{"))
            {
                ParseZeroOrMoreQuals(cl);
                ParseZeroOrMoreWs();
                bool success = stream.Consume("}");
                if (!success)
                {
                    Error("Missing closing '}' for trailing qualifier block.");
                }
            }
        }

        private void ParseZeroOrMoreQuals(Clause cl)
        {
            if (ParseQual(cl))
            {
                while (stream.Consume(",") && ParseQual(cl))
                {
                    // repeat while more available
                }
            }
        }

        private bool ParseQual(Clause cl)
        {
            ParseZeroOrMoreWs();
            string rest = stream.Rest();
            if (!rest.Contains("="))
            {
                Error(
                    "Missing '=' in trailing qualifier block. This might happen for not properly escaped '{', '}' chars in comments.");
            }
            string q = GetParseUntilAdv("=");
            ParseZeroOrMoreWs();
            string v;
            if (stream.Consume("\""))
            {
                v = GetParseUntilAdv("\"");
            }
            else
            {
                v = GetParseUntil(" ,}");
                Warn("qualifier values should be enclosed in quotes. You have: " + q + '='
                    + stream.Rest());
            }
            if (string.IsNullOrWhiteSpace(v))
            {
                Warn("Empty value for qualifier in trailing qualifier block.");
                v = "";
            }
            QualifierValue qv = new QualifierValue(q, v);
            cl.AddQualifierValue(qv);
            ParseZeroOrMoreWs();
            return true;
        }

        // ----------------------------------------
        // Other
        // ----------------------------------------
        private Clause ParseBoolean(Clause cl)
        {
            if (stream.Consume("true"))
            {
                cl.SetValue(true);
            }
            else if (stream.Consume("false"))
            {
                cl.SetValue(false);
            }
            else
            {
                Error("Could not parse bool value.");
            }
            return cl;
        }

        // ----------------------------------------
        // End-of-line matter
        // ----------------------------------------

        protected void ParseIdLine(Frame f)
        {
            string t = GetParseTag();
            OboFormatTag? tag = GetTag(t);
            if (tag != OboFormatTag.TAG_ID)
            {
                Error("Expected id tag as first line in frame, but was: " + tag);
            }
            Clause cl = new Clause(t);
            f.AddClause(cl);
            string id = GetParseUntil(BRACE);
            if (string.IsNullOrWhiteSpace(id))
            {
                Error("Could not find an valid id, id is empty.");
            }
            cl.AddValue(id);
            f.Id = id;
            ParseEOL(cl);
        }

        /**
         * @param cl cl
         * @throws OBOFormatParserException parser exception
         */
        public void ParseEOL(Clause cl)
        {
            ParseQualifierAndHiddenComment(cl);
            ForceParseNlOrEof();
        }

        private void ParseHiddenComment()
        {
            ParseZeroOrMoreWs();
            if (stream.PeekCharIs('!'))
            {
                stream.ForceEol();
            }
        }

        protected Clause ParseUnquotedString(Clause cl)
        {
            ParseZeroOrMoreWs();
            string v = GetParseUntil("!{");
            // strip whitespace from the end - TODO
            v = RemoveTrailingWS(v);
            cl.SetValue(v);
            if (stream.PeekCharIs('{'))
            {
                ParseQualifierBlock(cl);
            }
            ParseHiddenComment();
            return cl;
        }

        protected Clause ParseCustomTag(Clause cl)
        {
            return ParseUnquotedString(cl);
        }

        // Newlines, whitespace
        protected void ForceParseNlOrEof()
        {
            ParseZeroOrMoreWs();
            if (stream.Eol())
            {
                stream.AdvanceLine();
                return;
            }
            if (stream.Eof())
            {
                return;
            }
            Error("expected newline or end of line but found: " + stream.Rest());
        }

        protected void ParseZeroOrMoreWsOptCmtNl()
        {
            while (true)
            {
                ParseZeroOrMoreWs();
                ParseHiddenComment();
                if (stream.Eol())
                {
                    stream.AdvanceLine();
                }
                else
                {
                    return;
                }
            }
        }

        // non-newline
        protected void ParseWs()
        {
            if (stream.Eol())
            {
                Error("Expected at least one white space, but found end of line at pos: " + stream.Pos);
            }
            if (stream.Eof())
            {
                Error("Expected at least one white space, but found end of file.");
            }
            if (stream.PeekChar() == ' ')
            {
                stream.Advance(1);
            }
            else
            {
                Warn("Expected white space at pos: " + stream.Pos);
            }
        }

        protected void ParseOneOrMoreWs()
        {
            if (stream.Eol() || stream.Eof())
            {
                Error("Expected at least one white space at pos: " + stream.Pos);
            }
            int n = 0;
            while (stream.PeekCharIs(' '))
            {
                stream.Advance(1);
                n++;
            }
            if (n == 0)
            {
                Error("Expected at least one white space at pos: " + stream.Pos);
            }
        }

        protected void ParseZeroOrMoreWs()
        {
            if (!stream.Eol() && !stream.Eof())
            {
                while (stream.PeekCharIs(' '))
                {
                    stream.Advance(1);
                }
            }
        }

        private string GetParseUntilAdv(string compl)
        {
            string ret = GetParseUntil(compl);
            stream.Advance(1);
            return ret;
        }

        private string GetParseUntil(string compl)
        {
            return GetParseUntil(compl, false);
        }

        private string GetParseUntil(string compl, bool commaWhitespace)
        {
            string r = stream.Rest();
            int i = 0;
            bool hasEscapedChars = false;
            while (i < r.Length)
            {
                if (r[i] == '\\')
                {
                    hasEscapedChars = true;
                    i += 2;// Escape
                    continue;
                }
                if (compl.Contains(r[i]))
                {
                    if (commaWhitespace && r[i] == ',')
                    {
                        // a comma is only a valid separator with a following
                        // whitespace
                        // see bug and specification update
                        // http://code.google.com/p/oboformat/issues/detail?id=54
                        if (i + 1 < r.Length && r[i + 1] == ' ')
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                i++;
            }
            if (i == 0)
            {
                return string.Empty;
            }
            string ret = r.Substring(0, i);
            if (hasEscapedChars)
            {
                ret = HandleEscapedChars(ret);
            }
            stream.Advance(i);
            // return stringCache.get(ret);
            return ret;
        }

        protected string HandleEscapedChars(string ret)
        {
            StringBuilder sb = new StringBuilder();
            for (int j = 0; j < ret.Length; j++)
            {
                char c = ret[j];
                if (c == '\\')
                {
                    int next = j + 1;
                    if (next < ret.Length)
                    {
                        char nextChar = ret[next];
                        HandleNextChar(sb, nextChar);
                        j += 1;// skip the next char
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        protected void HandleNextChar(StringBuilder sb, char nextChar)
        {
            switch (nextChar)
            {
                case 'n':// newline
                    sb.Append('\n');
                    break;
                case 'W':// single space
                    sb.Append(' ');
                    break;
                case 't':// tab
                    sb.Append('\t');
                    break;
                default:
                    // assume that any char after a backlash is an escaped char.
                    // spec for this optional behavior
                    // http://www.geneontology.org/GO.format.obo-1_2.shtml#S.1.5
                    sb.Append(nextChar);
                    break;
            }
        }

        private void Error(string message)
        {
            throw new OBOFormatParserException(message, stream.LineNo, stream.Line());
        }

        private void Warn(string message)
        {
            LOGGER.Warn("LINE: {0} {1}  LINE:\n{2}", stream.LineNo, message, stream.Line());
        }

        protected class MyStream
        {
            public int Pos { get; private set; } = 0;
            string? _line;
            public int LineNo { get; private set; } = 0;
            public TextReader? Reader { get; set; }

            public MyStream()
            {
                Pos = 0;
            }

            public MyStream(TextReader r)
            {
                Reader = r;
            }

            public static string GetTag()
            {
                return string.Empty;
            }

            public string Line()
            {
                return OWLAPIPreconditions.VerifyNotNull(_line);
            }

            public char PeekChar()
            {
                Prepare();
                return Line()[Pos];
            }

            public char NextChar()
            {
                Pos++;
                return Line()[Pos - 1];
            }

            public string Rest()
            {
                Prepare();
                if (_line == null)
                {
                    return string.Empty;
                }
                if (Pos >= Line().Length)
                {
                    return string.Empty;
                }
                return Line().Substring(Pos);
            }

            public void Advance(int dist)
            {
                Pos += dist;
            }

            public void Prepare()
            {
                if (_line == null)
                {
                    AdvanceLine();
                }
            }

            public void AdvanceLine()
            {
                try
                {
                    _line = OWLAPIPreconditions.VerifyNotNull(Reader, "reader must be set before accessing it").ReadLine();
                    LineNo++;
                    Pos = 0;
                }
                catch (IOException e)
                {
                    throw new OBOFormatParserException(e, LineNo, "Error reading from input.");
                }
            }

            public void ForceEol()
            {
                if (_line == null)
                {
                    return;
                }
                Pos = Line().Length;
            }

            public bool Eol()
            {
                Prepare();
                if (_line == null)
                {
                    return false;
                }
                return Pos >= Line().Length;
            }

            public bool Eof()
            {
                Prepare();
                return _line == null;
            }

            public bool Consume(string s)
            {
                string r = Rest();
                if (string.IsNullOrWhiteSpace(r))
                {
                    return false;
                }
                if (r.StartsWith(s))
                {
                    Pos += s.Length;
                    return true;
                }
                return false;
            }

            public int IndexOf(char c)
            {
                Prepare();
                if (_line == null)
                {
                    return -1;
                }
                return Line().Substring(Pos).IndexOf(c);
            }

            override public string ToString()
            {
                return _line + "//" + Pos + " LINE:" + LineNo;
            }

            public bool PeekCharIs(char c)
            {
                if (Eol() || Eof())
                {
                    return false;
                }
                return PeekChar() == c;
            }
        }
    }
}
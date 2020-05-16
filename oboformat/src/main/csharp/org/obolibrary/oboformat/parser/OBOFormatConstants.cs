#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace org.obolibrary.oboformat.parser
{


    /**
     * OBOformat constants.
     */
    public static class OBOFormatConstants
    {

        private static int LookupAndPullField(string s, Func<OboFormatTag, int> f)
        {
            return OboFormatTag.TAGSTABLE.TryGetValue(s, out OboFormatTag t)
                ? f(t)
                : 10000;
        }

        /// <summary>
        /// Header priority comparator.
        /// </summary>
        public static IComparer<string> headerPriority = Comparer<string>.Create((a, b) => LookupAndPullField(a, t => t.HeaderTagsPriority).CompareTo(LookupAndPullField(b, t => t.HeaderTagsPriority)));
        /// <summary>
        /// Tag priority comparator.
        /// </summary>
        public static IComparer<string> tagPriority = Comparer<string>.Create((a, b) => LookupAndPullField(a, t => t.TagsPriority).CompareTo(LookupAndPullField(b, t => t.TagsPriority)));
        /// <summary>
        /// typedef priority comparator.
        /// </summary>
        public static IComparer<string> typeDefPriority = Comparer<string>.Create((a, b) => LookupAndPullField(a, t => t.TypeDefTagsPriority).CompareTo(LookupAndPullField(b, t => t.TypeDefTagsPriority)));

        /// <param name="tag"> tag </param>
        /// <returns> oboformat tag </returns>
        public static OboFormatTag? GetTag(string tag)
        {
            return OboFormatTag.TAGSTABLE[tag];
        }

        /// <returns> Date format for OboFormatTag.TAG_DATE </returns>
        public const string HeaderDateFormat = "dd:MM:yyyy HH:mm";

        /// <summary>
        /// OBOformat tags.
        /// </summary>
        public sealed class OboFormatTag
        {
            internal static readonly IDictionary<string, OboFormatTag> TAGSTABLE;

            /// <summary>TAG_FORMAT_VERSION.</summary>
            public static readonly OboFormatTag TAG_FORMAT_VERSION = new OboFormatTag("TAG_FORMAT_VERSION", InnerEnum.TAG_FORMAT_VERSION, "format-version", 0);
            // moved from pos 5 to emulate OBO-Edit behavior
            public static readonly OboFormatTag TAG_ONTOLOGY = new OboFormatTag("TAG_ONTOLOGY", InnerEnum.TAG_ONTOLOGY, "ontology", 85);
            public static readonly OboFormatTag TAG_DATA_VERSION = new OboFormatTag("TAG_DATA_VERSION", InnerEnum.TAG_DATA_VERSION, "data-version", 10);
            public static readonly OboFormatTag TAG_DATE = new OboFormatTag("TAG_DATE", InnerEnum.TAG_DATE, "date", 15);
            public static readonly OboFormatTag TAG_SAVED_BY = new OboFormatTag("TAG_SAVED_BY", InnerEnum.TAG_SAVED_BY, "saved-by", 20);
            public static readonly OboFormatTag TAG_AUTO_GENERATED_BY = new OboFormatTag("TAG_AUTO_GENERATED_BY", InnerEnum.TAG_AUTO_GENERATED_BY, "auto-generated-by", 25);
            // moved from pos 30 to emulate OBO-Edit behavior
            public static readonly OboFormatTag TAG_IMPORT = new OboFormatTag("TAG_IMPORT", InnerEnum.TAG_IMPORT, "import", 80);
            public static readonly OboFormatTag TAG_SUBSETDEF = new OboFormatTag("TAG_SUBSETDEF", InnerEnum.TAG_SUBSETDEF, "subsetdef", 35);
            public static readonly OboFormatTag TAG_SYNONYMTYPEDEF = new OboFormatTag("TAG_SYNONYMTYPEDEF", InnerEnum.TAG_SYNONYMTYPEDEF, "synonymtypedef", 40);
            public static readonly OboFormatTag TAG_DEFAULT_NAMESPACE = new OboFormatTag("TAG_DEFAULT_NAMESPACE", InnerEnum.TAG_DEFAULT_NAMESPACE, "default-namespace", 45);
            public static readonly OboFormatTag TAG_IDSPACE = new OboFormatTag("TAG_IDSPACE", InnerEnum.TAG_IDSPACE, "idspace", 50);
            public static readonly OboFormatTag TAG_TREAT_XREFS_AS_EQUIVALENT = new OboFormatTag("TAG_TREAT_XREFS_AS_EQUIVALENT", InnerEnum.TAG_TREAT_XREFS_AS_EQUIVALENT, "treat-xrefs-as-equivalent", 55);
            public static readonly OboFormatTag TAG_TREAT_XREFS_AS_REVERSE_GENUS_DIFFERENTIA = new OboFormatTag("TAG_TREAT_XREFS_AS_REVERSE_GENUS_DIFFERENTIA", InnerEnum.TAG_TREAT_XREFS_AS_REVERSE_GENUS_DIFFERENTIA, "treat-xrefs-as-reverse-genus-differentia");
            public static readonly OboFormatTag TAG_TREAT_XREFS_AS_GENUS_DIFFERENTIA = new OboFormatTag("TAG_TREAT_XREFS_AS_GENUS_DIFFERENTIA", InnerEnum.TAG_TREAT_XREFS_AS_GENUS_DIFFERENTIA, "treat-xrefs-as-genus-differentia", 60);
            public static readonly OboFormatTag TAG_TREAT_XREFS_AS_RELATIONSHIP = new OboFormatTag("TAG_TREAT_XREFS_AS_RELATIONSHIP", InnerEnum.TAG_TREAT_XREFS_AS_RELATIONSHIP, "treat-xrefs-as-relationship", 65);
            public static readonly OboFormatTag TAG_TREAT_XREFS_AS_IS_A = new OboFormatTag("TAG_TREAT_XREFS_AS_IS_A", InnerEnum.TAG_TREAT_XREFS_AS_IS_A, "treat-xrefs-as-is_a", 70);
            public static readonly OboFormatTag TAG_TREAT_XREFS_AS_HAS_SUBCLASS = new OboFormatTag("TAG_TREAT_XREFS_AS_HAS_SUBCLASS", InnerEnum.TAG_TREAT_XREFS_AS_HAS_SUBCLASS, "treat-xrefs-as-has-subclass");
            public static readonly OboFormatTag TAG_OWL_AXIOMS = new OboFormatTag("TAG_OWL_AXIOMS", InnerEnum.TAG_OWL_AXIOMS, "owl-axioms", 110);
            public static readonly OboFormatTag TAG_REMARK = new OboFormatTag("TAG_REMARK", InnerEnum.TAG_REMARK, "remark", 75);
            public static readonly OboFormatTag TAG_ID = new OboFormatTag("TAG_ID", InnerEnum.TAG_ID, "id", 10000, 5, 5);
            public static readonly OboFormatTag TAG_NAME = new OboFormatTag("TAG_NAME", InnerEnum.TAG_NAME, "name", 10000, 15, 15);
            public static readonly OboFormatTag TAG_NAMESPACE = new OboFormatTag("TAG_NAMESPACE", InnerEnum.TAG_NAMESPACE, "namespace", 10000, 20, 20);
            public static readonly OboFormatTag TAG_ALT_ID = new OboFormatTag("TAG_ALT_ID", InnerEnum.TAG_ALT_ID, "alt_id", 10000, 25, 25);
            public static readonly OboFormatTag TAG_DEF = new OboFormatTag("TAG_DEF", InnerEnum.TAG_DEF, "def", 10000, 30, 30);
            public static readonly OboFormatTag TAG_COMMENT = new OboFormatTag("TAG_COMMENT", InnerEnum.TAG_COMMENT, "comment", 10000, 35, 35);
            public static readonly OboFormatTag TAG_SUBSET = new OboFormatTag("TAG_SUBSET", InnerEnum.TAG_SUBSET, "subset", 10000, 40, 40);
            public static readonly OboFormatTag TAG_SYNONYM = new OboFormatTag("TAG_SYNONYM", InnerEnum.TAG_SYNONYM, "synonym", 10000, 45, 45);
            public static readonly OboFormatTag TAG_XREF = new OboFormatTag("TAG_XREF", InnerEnum.TAG_XREF, "xref", 10000, 50, 50);
            public static readonly OboFormatTag TAG_BUILTIN = new OboFormatTag("TAG_BUILTIN", InnerEnum.TAG_BUILTIN, "builtin", 10000, 55, 70);
            public static readonly OboFormatTag TAG_PROPERTY_VALUE = new OboFormatTag("TAG_PROPERTY_VALUE", InnerEnum.TAG_PROPERTY_VALUE, "property_value", 100, 98, 55);
            public static readonly OboFormatTag TAG_IS_A = new OboFormatTag("TAG_IS_A", InnerEnum.TAG_IS_A, "is_a", 10000, 65, 115);
            public static readonly OboFormatTag TAG_INTERSECTION_OF = new OboFormatTag("TAG_INTERSECTION_OF", InnerEnum.TAG_INTERSECTION_OF, "intersection_of", 10000, 70, 120);
            public static readonly OboFormatTag TAG_UNION_OF = new OboFormatTag("TAG_UNION_OF", InnerEnum.TAG_UNION_OF, "union_of", 10000, 80, 125);
            public static readonly OboFormatTag TAG_EQUIVALENT_TO = new OboFormatTag("TAG_EQUIVALENT_TO", InnerEnum.TAG_EQUIVALENT_TO, "equivalent_to", 10000, 85, 130);
            public static readonly OboFormatTag TAG_DISJOINT_FROM = new OboFormatTag("TAG_DISJOINT_FROM", InnerEnum.TAG_DISJOINT_FROM, "disjoint_from", 10000, 90, 135);
            public static readonly OboFormatTag TAG_RELATIONSHIP = new OboFormatTag("TAG_RELATIONSHIP", InnerEnum.TAG_RELATIONSHIP, "relationship", 10000, 95, 165);
            public static readonly OboFormatTag TAG_CREATED_BY = new OboFormatTag("TAG_CREATED_BY", InnerEnum.TAG_CREATED_BY, "created_by", 10000, 130, 191);
            public static readonly OboFormatTag TAG_CREATION_DATE = new OboFormatTag("TAG_CREATION_DATE", InnerEnum.TAG_CREATION_DATE, "creation_date", 10000, 140, 192);
            public static readonly OboFormatTag TAG_IS_OBSELETE = new OboFormatTag("TAG_IS_OBSELETE", InnerEnum.TAG_IS_OBSELETE, "is_obsolete", 10000, 110, 169);
            public static readonly OboFormatTag TAG_REPLACED_BY = new OboFormatTag("TAG_REPLACED_BY", InnerEnum.TAG_REPLACED_BY, "replaced_by", 10000, 115, 185);
            public static readonly OboFormatTag TAG_IS_ANONYMOUS = new OboFormatTag("TAG_IS_ANONYMOUS", InnerEnum.TAG_IS_ANONYMOUS, "is_anonymous", 10000, 10, 10);
            public static readonly OboFormatTag TAG_DOMAIN = new OboFormatTag("TAG_DOMAIN", InnerEnum.TAG_DOMAIN, "domain", 10000, 10000, 60);
            public static readonly OboFormatTag TAG_RANGE = new OboFormatTag("TAG_RANGE", InnerEnum.TAG_RANGE, "range", 10000, 10000, 65);
            public static readonly OboFormatTag TAG_IS_ANTI_SYMMETRIC = new OboFormatTag("TAG_IS_ANTI_SYMMETRIC", InnerEnum.TAG_IS_ANTI_SYMMETRIC, "is_anti_symmetric", 10000, 10000, 75);
            public static readonly OboFormatTag TAG_IS_CYCLIC = new OboFormatTag("TAG_IS_CYCLIC", InnerEnum.TAG_IS_CYCLIC, "is_cyclic", 10000, 10000, 80);
            public static readonly OboFormatTag TAG_IS_REFLEXIVE = new OboFormatTag("TAG_IS_REFLEXIVE", InnerEnum.TAG_IS_REFLEXIVE, "is_reflexive", 10000, 10000, 85);
            public static readonly OboFormatTag TAG_IS_SYMMETRIC = new OboFormatTag("TAG_IS_SYMMETRIC", InnerEnum.TAG_IS_SYMMETRIC, "is_symmetric", 10000, 10000, 90);
            public static readonly OboFormatTag TAG_IS_TRANSITIVE = new OboFormatTag("TAG_IS_TRANSITIVE", InnerEnum.TAG_IS_TRANSITIVE, "is_transitive", 10000, 10000, 100);
            public static readonly OboFormatTag TAG_IS_FUNCTIONAL = new OboFormatTag("TAG_IS_FUNCTIONAL", InnerEnum.TAG_IS_FUNCTIONAL, "is_functional", 10000, 10000, 105);
            public static readonly OboFormatTag TAG_IS_INVERSE_FUNCTIONAL = new OboFormatTag("TAG_IS_INVERSE_FUNCTIONAL", InnerEnum.TAG_IS_INVERSE_FUNCTIONAL, "is_inverse_functional", 10000, 10000, 110);
            public static readonly OboFormatTag TAG_TRANSITIVE_OVER = new OboFormatTag("TAG_TRANSITIVE_OVER", InnerEnum.TAG_TRANSITIVE_OVER, "transitive_over", 10000, 10000, 145);
            public static readonly OboFormatTag TAG_HOLDS_OVER_CHAIN = new OboFormatTag("TAG_HOLDS_OVER_CHAIN", InnerEnum.TAG_HOLDS_OVER_CHAIN, "holds_over_chain", 10000, 60, 71);
            public static readonly OboFormatTag TAG_EQUIVALENT_TO_CHAIN = new OboFormatTag("TAG_EQUIVALENT_TO_CHAIN", InnerEnum.TAG_EQUIVALENT_TO_CHAIN, "equivalent_to_chain", 10000, 10000, 155);
            public static readonly OboFormatTag TAG_DISJOINT_OVER = new OboFormatTag("TAG_DISJOINT_OVER", InnerEnum.TAG_DISJOINT_OVER, "disjoint_over", 10000, 10000, 160);
            public static readonly OboFormatTag TAG_EXPAND_ASSERTION_TO = new OboFormatTag("TAG_EXPAND_ASSERTION_TO", InnerEnum.TAG_EXPAND_ASSERTION_TO, "expand_assertion_to", 10000, 10000, 195);
            public static readonly OboFormatTag TAG_EXPAND_EXPRESSION_TO = new OboFormatTag("TAG_EXPAND_EXPRESSION_TO", InnerEnum.TAG_EXPAND_EXPRESSION_TO, "expand_expression_to", 10000, 10000, 200);
            public static readonly OboFormatTag TAG_IS_CLASS_LEVEL_TAG = new OboFormatTag("TAG_IS_CLASS_LEVEL_TAG", InnerEnum.TAG_IS_CLASS_LEVEL_TAG, "is_class_level", 10000, 10000, 210);
            public static readonly OboFormatTag TAG_IS_METADATA_TAG = new OboFormatTag("TAG_IS_METADATA_TAG", InnerEnum.TAG_IS_METADATA_TAG, "is_metadata_tag", 10000, 10000, 205);
            public static readonly OboFormatTag TAG_CONSIDER = new OboFormatTag("TAG_CONSIDER", InnerEnum.TAG_CONSIDER, "consider", 10000, 120, 190);
            public static readonly OboFormatTag TAG_INVERSE_OF = new OboFormatTag("TAG_INVERSE_OF", InnerEnum.TAG_INVERSE_OF, "inverse_of", 10000, 10000, 140);
            public static readonly OboFormatTag TAG_IS_ASYMMETRIC = new OboFormatTag("TAG_IS_ASYMMETRIC", InnerEnum.TAG_IS_ASYMMETRIC, "is_asymmetric");
            public static readonly OboFormatTag TAG_NAMESPACE_ID_RULE = new OboFormatTag("TAG_NAMESPACE_ID_RULE", InnerEnum.TAG_NAMESPACE_ID_RULE, "namespace-id-rule", 46);
            public static readonly OboFormatTag TAG_LOGICAL_DEFINITION_VIEW_RELATION = new OboFormatTag("TAG_LOGICAL_DEFINITION_VIEW_RELATION", InnerEnum.TAG_LOGICAL_DEFINITION_VIEW_RELATION, "logical-definition-view-relation");

            // these are keywords, not tags, but we keep them here for convenience

            public static readonly OboFormatTag TAG_SCOPE = new OboFormatTag("TAG_SCOPE", InnerEnum.TAG_SCOPE, "scope");
            /// <summary>Implicit, in synonymtypedef.</summary>
            public static readonly OboFormatTag TAG_HAS_SYNONYM_TYPE = new OboFormatTag("TAG_HAS_SYNONYM_TYPE", InnerEnum.TAG_HAS_SYNONYM_TYPE, "has_synonym_type");
            /// <summary>implicit, in synonym. </summary>
            public static readonly OboFormatTag TAG_BROAD = new OboFormatTag("TAG_BROAD", InnerEnum.TAG_BROAD, "BROAD");
            public static readonly OboFormatTag TAG_NARROW = new OboFormatTag("TAG_NARROW", InnerEnum.TAG_NARROW, "NARROW");
            public static readonly OboFormatTag TAG_EXACT = new OboFormatTag("TAG_EXACT", InnerEnum.TAG_EXACT, "EXACT");
            public static readonly OboFormatTag TAG_RELATED = new OboFormatTag("TAG_RELATED", InnerEnum.TAG_RELATED, "RELATED");

            private static readonly List<OboFormatTag> valueList = new List<OboFormatTag>();

            static OboFormatTag()
            {
                valueList.Add(TAG_FORMAT_VERSION);
                valueList.Add(TAG_ONTOLOGY);
                valueList.Add(TAG_DATA_VERSION);
                valueList.Add(TAG_DATE);
                valueList.Add(TAG_SAVED_BY);
                valueList.Add(TAG_AUTO_GENERATED_BY);
                valueList.Add(TAG_IMPORT);
                valueList.Add(TAG_SUBSETDEF);
                valueList.Add(TAG_SYNONYMTYPEDEF);
                valueList.Add(TAG_DEFAULT_NAMESPACE);
                valueList.Add(TAG_IDSPACE);
                valueList.Add(TAG_TREAT_XREFS_AS_EQUIVALENT);
                valueList.Add(TAG_TREAT_XREFS_AS_REVERSE_GENUS_DIFFERENTIA);
                valueList.Add(TAG_TREAT_XREFS_AS_GENUS_DIFFERENTIA);
                valueList.Add(TAG_TREAT_XREFS_AS_RELATIONSHIP);
                valueList.Add(TAG_TREAT_XREFS_AS_IS_A);
                valueList.Add(TAG_TREAT_XREFS_AS_HAS_SUBCLASS);
                valueList.Add(TAG_OWL_AXIOMS);
                valueList.Add(TAG_REMARK);
                valueList.Add(TAG_ID);
                valueList.Add(TAG_NAME);
                valueList.Add(TAG_NAMESPACE);
                valueList.Add(TAG_ALT_ID);
                valueList.Add(TAG_DEF);
                valueList.Add(TAG_COMMENT);
                valueList.Add(TAG_SUBSET);
                valueList.Add(TAG_SYNONYM);
                valueList.Add(TAG_XREF);
                valueList.Add(TAG_BUILTIN);
                valueList.Add(TAG_PROPERTY_VALUE);
                valueList.Add(TAG_IS_A);
                valueList.Add(TAG_INTERSECTION_OF);
                valueList.Add(TAG_UNION_OF);
                valueList.Add(TAG_EQUIVALENT_TO);
                valueList.Add(TAG_DISJOINT_FROM);
                valueList.Add(TAG_RELATIONSHIP);
                valueList.Add(TAG_CREATED_BY);
                valueList.Add(TAG_CREATION_DATE);
                valueList.Add(TAG_IS_OBSELETE);
                valueList.Add(TAG_REPLACED_BY);
                valueList.Add(TAG_IS_ANONYMOUS);
                valueList.Add(TAG_DOMAIN);
                valueList.Add(TAG_RANGE);
                valueList.Add(TAG_IS_ANTI_SYMMETRIC);
                valueList.Add(TAG_IS_CYCLIC);
                valueList.Add(TAG_IS_REFLEXIVE);
                valueList.Add(TAG_IS_SYMMETRIC);
                valueList.Add(TAG_IS_TRANSITIVE);
                valueList.Add(TAG_IS_FUNCTIONAL);
                valueList.Add(TAG_IS_INVERSE_FUNCTIONAL);
                valueList.Add(TAG_TRANSITIVE_OVER);
                valueList.Add(TAG_HOLDS_OVER_CHAIN);
                valueList.Add(TAG_EQUIVALENT_TO_CHAIN);
                valueList.Add(TAG_DISJOINT_OVER);
                valueList.Add(TAG_EXPAND_ASSERTION_TO);
                valueList.Add(TAG_EXPAND_EXPRESSION_TO);
                valueList.Add(TAG_IS_CLASS_LEVEL_TAG);
                valueList.Add(TAG_IS_METADATA_TAG);
                valueList.Add(TAG_CONSIDER);
                valueList.Add(TAG_INVERSE_OF);
                valueList.Add(TAG_IS_ASYMMETRIC);
                valueList.Add(TAG_NAMESPACE_ID_RULE);
                valueList.Add(TAG_LOGICAL_DEFINITION_VIEW_RELATION);
                valueList.Add(TAG_SCOPE);
                valueList.Add(TAG_HAS_SYNONYM_TYPE);
                valueList.Add(TAG_BROAD);
                valueList.Add(TAG_NARROW);
                valueList.Add(TAG_EXACT);
                valueList.Add(TAG_RELATED);

                TAGSTABLE = valueList.ToDictionary(value => value.Tag);
            }

            public enum InnerEnum
            {
                TAG_FORMAT_VERSION,
                TAG_ONTOLOGY,
                TAG_DATA_VERSION,
                TAG_DATE,
                TAG_SAVED_BY,
                TAG_AUTO_GENERATED_BY,
                TAG_IMPORT,
                TAG_SUBSETDEF,
                TAG_SYNONYMTYPEDEF,
                TAG_DEFAULT_NAMESPACE,
                TAG_IDSPACE,
                TAG_TREAT_XREFS_AS_EQUIVALENT,
                TAG_TREAT_XREFS_AS_REVERSE_GENUS_DIFFERENTIA,
                TAG_TREAT_XREFS_AS_GENUS_DIFFERENTIA,
                TAG_TREAT_XREFS_AS_RELATIONSHIP,
                TAG_TREAT_XREFS_AS_IS_A,
                TAG_TREAT_XREFS_AS_HAS_SUBCLASS,
                TAG_OWL_AXIOMS,
                TAG_REMARK,
                TAG_ID,
                TAG_NAME,
                TAG_NAMESPACE,
                TAG_ALT_ID,
                TAG_DEF,
                TAG_COMMENT,
                TAG_SUBSET,
                TAG_SYNONYM,
                TAG_XREF,
                TAG_BUILTIN,
                TAG_PROPERTY_VALUE,
                TAG_IS_A,
                TAG_INTERSECTION_OF,
                TAG_UNION_OF,
                TAG_EQUIVALENT_TO,
                TAG_DISJOINT_FROM,
                TAG_RELATIONSHIP,
                TAG_CREATED_BY,
                TAG_CREATION_DATE,
                TAG_IS_OBSELETE,
                TAG_REPLACED_BY,
                TAG_IS_ANONYMOUS,
                TAG_DOMAIN,
                TAG_RANGE,
                TAG_IS_ANTI_SYMMETRIC,
                TAG_IS_CYCLIC,
                TAG_IS_REFLEXIVE,
                TAG_IS_SYMMETRIC,
                TAG_IS_TRANSITIVE,
                TAG_IS_FUNCTIONAL,
                TAG_IS_INVERSE_FUNCTIONAL,
                TAG_TRANSITIVE_OVER,
                TAG_HOLDS_OVER_CHAIN,
                TAG_EQUIVALENT_TO_CHAIN,
                TAG_DISJOINT_OVER,
                TAG_EXPAND_ASSERTION_TO,
                TAG_EXPAND_EXPRESSION_TO,
                TAG_IS_CLASS_LEVEL_TAG,
                TAG_IS_METADATA_TAG,
                TAG_CONSIDER,
                TAG_INVERSE_OF,
                TAG_IS_ASYMMETRIC,
                TAG_NAMESPACE_ID_RULE,
                TAG_LOGICAL_DEFINITION_VIEW_RELATION,
                TAG_SCOPE,
                TAG_HAS_SYNONYM_TYPE,
                TAG_BROAD,
                TAG_NARROW,
                TAG_EXACT,
                TAG_RELATED
            }

            public readonly InnerEnum innerEnumValue;
            private readonly string nameValue;
            public int Ordinal { get; }
            private static int nextOrdinal = 0;

            /// <summary>
            /// Term frames.
            /// </summary>
            public static readonly ISet<OboFormatTag> TERM_FRAMES = new HashSet<OboFormatTag> { TAG_INTERSECTION_OF, TAG_UNION_OF, TAG_EQUIVALENT_TO, TAG_DISJOINT_FROM, TAG_RELATIONSHIP, TAG_IS_A };
            /// <summary>
            /// Typedef frames.
            /// </summary>
            public static readonly ISet<OboFormatTag> TYPEDEF_FRAMES = new HashSet<OboFormatTag> { TAG_INTERSECTION_OF, TAG_UNION_OF, TAG_EQUIVALENT_TO, TAG_DISJOINT_FROM, TAG_INVERSE_OF, TAG_TRANSITIVE_OVER, TAG_DISJOINT_OVER, TAG_IS_A };
            internal string Tag { get; }
            internal int HeaderTagsPriority { get; }
            internal int TagsPriority { get; }
            internal  int TypeDefTagsPriority { get; }

            internal OboFormatTag(string name, InnerEnum innerEnum, string tag)
                : this(name, innerEnum, tag, 10000, 10000, 10000)
            {
                nameValue = name;
                Ordinal = nextOrdinal++;
                innerEnumValue = innerEnum;
            }

            internal OboFormatTag(string name, InnerEnum innerEnum, string tag, int header)
                : this(name, innerEnum, tag, header, 10000, 10000)
            {
                nameValue = name;
                Ordinal = nextOrdinal++;
                innerEnumValue = innerEnum;
            }

            internal OboFormatTag(string name, InnerEnum innerEnum, string tag, int header, int priority, int typedef)
            {
                Tag = tag;
                HeaderTagsPriority = header;
                TagsPriority = priority;
                TypeDefTagsPriority = typedef;

                nameValue = name;
                Ordinal = nextOrdinal++;
                innerEnumValue = innerEnum;
            }

            public static OboFormatTag[] Values()
            {
                return valueList.ToArray();
            }

            public override string ToString() => nameValue;

            public static OboFormatTag ValueOf(string name)
            {
                foreach (OboFormatTag enumInstance in valueList)
                    if (enumInstance.nameValue == name)
                        return enumInstance;
                throw new ArgumentException(name);
            }
        }
    }
}
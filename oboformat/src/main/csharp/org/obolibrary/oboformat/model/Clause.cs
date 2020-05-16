#nullable enable

using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using static org.obolibrary.oboformat.parser.OBOFormatConstants;

namespace org.obolibrary.oboformat.model
{

    // import org.obolibrary.oboformat.parser.OBOFormatConstants.OboFormatTag;

    /// <remarks>The original version of this went to considerable lengths to minimise memory usage. This goes to less.</remarks>
    public class Clause
    {

        private static readonly ILogger LOGGER = LogManager.GetCurrentClassLogger();
        private static readonly ISet<object> trueValues = new HashSet<object> { true, "true" };
        private static readonly ISet<object> falseValues = new HashSet<object> { false, "false" };
        public string? Tag { get; set; }
        public IList<object> Values { get; private set; } = new List<object>();
        protected IList<Xref> Xrefs { get; private set; } = new List<Xref>();
        protected IList<QualifierValue> QualifierValues { get; private set; } = new List<QualifierValue>();

        public Clause(OboFormatTag tag)
            : this(tag.Tag)
        {
        }

        public Clause(string? tag)
        {
            Tag = tag;
        }

        public Clause(string? tag, string value)
            : this(tag)
        {
            SetValue(value);
        }

        public Clause(OboFormatTag tag, string value)
            : this(tag.Tag, value)
        {
        }

        [Obsolete("Use Clause(String). Using this constructor makes the hashcode variable.")]
        public Clause()
        {
        }

        private static bool CollectionsEquals<T>(ICollection<T>? c1, ICollection<T>? c2)
        {
            if (c1 == null || c1.Count == 0)
                return c2 == null || c2.Count == 0;
            if (c2 == null || c1.Count != c2.Count)
                return false;
            return CheckContents(c1, c2);
        }

        protected static bool CheckContents<T>(ICollection<T> c1, ICollection<T> c2)
        {
            // xrefs are stored as lists to preserve order, but order is not important for comparisons
            ISet<T> s = new HashSet<T>(c2);
            foreach (T x in c1)
                if (!s.Contains(x))
                    return false;
            return true;
        }

        /**
         * @param value value to set
         * @return modified clause
         */
        public Clause WithValue(string value)
        {
            SetValue(value);
            return this;
        }

        /**
         * freezing a clause signals that the clause has become quiescent, and that data structures can
         * be adjusted to increase performance, or reduce memory consumption.
         */
        public void Freeze()
        {
            FreezeValues();
            FreezeXrefs();
            FreezeQualifiers();
        }

        void FreezeValues()
        {
            if (0 == Values.Count)
                Values = Array.Empty<object>();
        }

        void FreezeXrefs()
        {
            if (0 == Xrefs.Count)
                Xrefs = Array.Empty<Xref>();
        }

        void FreezeQualifiers()
        {
            if (0 == QualifierValues.Count)
                QualifierValues = Array.Empty<QualifierValue>();
        }

        /**
         * @return true if no xrefs or qualifiers exist
         */
        public bool HasNoAnnotations() => Xrefs.Count == 0 && QualifierValues.Count == 0;

        public void SetValues(IList<object> values)
        {
            if (Values is List<object>)
                Values.Clear();
            else
                Values = new List<object>(values);
            ((List<object>)Values).AddRange(values);
        }

        public void AddValue(object v)
        {
            Values.Add(v);
        }

        /**
         * @return value
         * @throws FrameStructureException if there is no value
         */
        public object Value()
        {
            if (null == Values || Values.Count == 0 || null == Values[0])
            {
                LOGGER.Error("Cannot translate: {}", this);
                throw new FrameStructureException("Clause value is null: " + this);
            }
            return Values[0];
        }

        public void SetValue(object v)
        {
            Values.Clear();
            Values.Add(v);
        }

        public T? Value<T>() where T : class => (T?)Value();

        public object? Value2() => Values.Count > 1 ? Values[1] : null;

        public T? Value2<T>() where T : class => (T?)Value2();

        public void SetXrefs(ICollection<Xref> xrefs)
        {
            Xrefs.Clear();
            ((List<Xref>)Xrefs).AddRange(xrefs);
        }

        public void AddXref(Xref xref)
        {
            Xrefs.Add(xref);
        }

        public void SetQualifierValues(ICollection<QualifierValue> qualifierValues)
        {
            QualifierValues.Clear();
            ((List<QualifierValue>)QualifierValues).AddRange(qualifierValues);
        }

        public void AddQualifierValue(QualifierValue qv)
        {
            QualifierValues.Add(qv);
        }

        override public string ToString()
        {
            StringBuilder sb = new StringBuilder(Tag);
            sb.Append('(');
            foreach (object ob in Values)
            {
                sb.Append(' ');
                sb.Append(ob);
            }
            if (QualifierValues.Count > 0)
            {
                sb.Append('{');
                foreach (QualifierValue qv in QualifierValues)
                {
                    sb.Append(qv);
                    sb.Append(' ');
                }
                sb.Append('}');
            }
            if (Xrefs.Count > 0)
            {
                sb.Append('[');
                foreach (Xref x in Xrefs)
                {
                    sb.Append(x);
                    sb.Append(' ');
                }
                sb.Append(']');
            }
            sb.Append(')');
            return sb.ToString();
        }

        override public int GetHashCode()
        {
            return 31 * 31 * 31 * QualifierValues.GetHashCode()
                + 31 * 31 * Values.GetHashCode()
                + 31 * Xrefs.GetHashCode()
                + Taghash();
        }

        private int Taghash()
        {
            return Tag == null
                ? 0
                : Tag.GetHashCode();
        }

        private bool TagEquals(string? otherTag)
        {
            return Tag == null
                ? otherTag == null
                : Tag.Equals(otherTag);
        }

        override public bool Equals(object? obj)
        {
            if (!(obj is Clause))
            {
                return false;
            }
            if (obj == this)
            {
                return true;
            }
            Clause other = (Clause)obj;
            if (!TagEquals(other.Tag))
            {
                return false;
            }
            if (Values.Count == 1 && other.Values.Count == 1)
            {
                // special case for comparing booleans
                // this is a bit of a hack - ideally owl2obo would use the correct
                // types
                if (!CompareValues(other))
                    return false;
            }
            else
            {
                if (!Values.Equals(other.Values))
                    return false;
            }
            if (!CollectionsEquals(Xrefs, other.Xrefs))
                return false;
            return CollectionsEquals(QualifierValues, other.QualifierValues);
        }

        protected bool CompareValues(Clause other)
        {
            try
            {
                object? v1 = Value();
                object? v2 = other.Value();
                if (v1 != v2 && !v1.Equals(v2))
                {
                    return trueValues.Contains(v1) && trueValues.Contains(v2)
                        || falseValues.Contains(v1) && falseValues.Contains(v2);
                }
            }
            catch (FrameStructureException e)
            {
                // this cannot happen as it's already been tested
                LOGGER.Debug(e);
            }
            return true;
        }
    }
}
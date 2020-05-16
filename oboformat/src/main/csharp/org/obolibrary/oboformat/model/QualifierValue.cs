#nullable enable

using System;

namespace org.obolibrary.oboformat.model
{
    /**
     * Qualifier value.
     */
    public class QualifierValue : IComparable<QualifierValue>
    {

        public string Qualifier { get; set; }
        public string Value { get; set; }

        /**
         * @param q qualifier
         * @param v value
         */
        public QualifierValue(string q, string v)
        {
            Qualifier = q;
            Value = v;
        }

        override public string ToString()
        {
            return $"{{{Qualifier}={Value}}}";
        }

        override public int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + Qualifier.GetHashCode();
            result = prime * result + Value.GetHashCode();
            return result;
        }

        override public bool Equals(object? obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
            {
                return false;
            }
            if (!(obj is QualifierValue))
            {
                return false;
            }
            QualifierValue other = (QualifierValue)obj;
            if (!Qualifier.Equals(other.Qualifier))
            {
                return false;
            }
            if (!Value.Equals(other.Value))
            {
                return false;
            }
            return true;
        }

        public int CompareTo(QualifierValue? o)
        {
            if (o == null)
            {
                return 1;
            }
            // use toString representation
            return ToString().CompareTo(o.ToString());
        }
    }
}
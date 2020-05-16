#nullable enable

using System;

namespace org.obolibrary.oboformat.model
{
    public class Xref {

        public string Idref { get; set; }
        public string? Annotation { get; set; }

        public Xref(string idref) {
            Idref = idref;
        }

        override public bool Equals(object? obj) {
            if (!(obj is Xref)) {
                return false;
            }
            if (obj == this) {
                return true;
            }
            Xref other = (Xref)obj;
            if (!Idref.Equals(other.Idref)) {
                return false;
            }
            // if (false) {
            // // TODO: make this configurable?
            // // xref comments are treated as semi-invisible
            // if (annotation == null && other.annotation == null) {
            // return true;
            // }
            // if (annotation == null || other.annotation == null) {
            // return false;
            // }
            // return annotation.equals(other.annotation);
            // }
            return true;
        }

        public override int GetHashCode() => HashCode.Combine(Idref);

        public override string ToString()
        {
            if (Annotation == null) {
                return Idref;
            }
            return '<' + Idref + " \"" + Annotation + "\">";
        }
    }
}
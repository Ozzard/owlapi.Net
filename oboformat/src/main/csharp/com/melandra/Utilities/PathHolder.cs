using System;

namespace com.melandra.Utilities
{
    /// <summary>
    /// What should have been the instance side of System.IO.Path: something that holds a file path and treats it as distinct from a String.
    /// </summary>
    /// <remarks>Immutable.</remarks>
    public struct PathHolder
    {
        public string Path { get; }

        public PathHolder(string path)
        {
            Path = path;
        }

        public PathHolder Combine(params string[] rest)
        {
            string[] merged = new string[rest.Length + 1];
            merged[0] = Path;
            Array.Copy(rest, 0, merged, 1, rest.Length);
            return new PathHolder(System.IO.Path.Combine(merged));
        }

        public PathHolder Combine(string rhs)
        {
            return new PathHolder(System.IO.Path.Combine(Path, rhs));
        }

        public PathHolder Directory => new PathHolder(System.IO.Path.GetDirectoryName(Path));

        public override bool Equals(object obj) => (object)this == obj || (null != obj && obj is PathHolder && Path.Equals(((PathHolder)obj).Path));
        public override int GetHashCode() => HashCode.Combine(Path);
        public static bool operator ==(PathHolder left, PathHolder right) => left.Equals(right);
        public static bool operator !=(PathHolder left, PathHolder right) => !(left == right);
    }
}

#nullable enable

using System;

namespace org.obolibrary.oboformat.model
{
    public class FrameMergeException : Exception
    {
        /**
         * Instantiates a new frame merge exception.
         *
         * @param msg the msg
         */
        public FrameMergeException(string msg)
            : base(msg)
        {
        }
    }
}

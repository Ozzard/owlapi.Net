#nullable enable

using System;

namespace org.obolibrary.oboformat.model
{
    /**
     * The Class DocumentStructureException.
     */
    public class DocumentStructureException : Exception
    {

        /**
         * Instantiates a new document structure exception.
         *
         * @param msg the msg
         */
        public DocumentStructureException(string msg)
            : base(msg)
        {
        }
    }
}
using System;

namespace org.obolibrary.oboformat.parser
{
    public class OBOFormatDanglingReferenceException : OBOFormatException
    {
        /**
         * Instantiates a new oBO format dangling reference exception.
         */
        public OBOFormatDanglingReferenceException()
        {
        }

        /**
         * Instantiates a new oBO format dangling reference exception.
         *
         * @param message the message
         */
        public OBOFormatDanglingReferenceException(string message)
            : base(message)
        {
        }

        /**
         * Instantiates a new oBO format dangling reference exception.
         *
         * @param e the e
         */
        public OBOFormatDanglingReferenceException(Exception e)
            : base(e)
        {
        }

        /**
         * Instantiates a new oBO format dangling reference exception.
         *
         * @param message the message
         * @param e the e
         */
        public OBOFormatDanglingReferenceException(string message, Exception e)
            : base(message, e)
        {
        }
    }
}
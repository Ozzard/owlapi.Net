using System;

namespace org.obolibrary.oboformat.parser
{

    /**
     * The Class InvalidXrefMapException.
     */
    public class InvalidXrefMapException : OBOFormatException
    {

        /**
         * Instantiates a new invalid xref map exception.
         */
        public InvalidXrefMapException()
        {
        }

        /**
         * Instantiates a new invalid xref map exception.
         *
         * @param message the message
         * @param e the e
         */
        public InvalidXrefMapException(string message, Exception e)
            : base(message, e)
        {
        }

        /**
         * Instantiates a new invalid xref map exception.
         *
         * @param message the message
         */
        public InvalidXrefMapException(string message)
            : base(message)
        {
        }

        /**
         * Instantiates a new invalid xref map exception.
         *
         * @param e the e
         */
        public InvalidXrefMapException(Exception e)
        : base(e)
        {
        }
    }
}
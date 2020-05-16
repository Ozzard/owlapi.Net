using org.semanticweb.owlapi.io;
using System;

namespace org.obolibrary.oboformat.parser
{
    /**
     * The Class OBOFormatException.
     */
    public class OBOFormatException : OWLParserException
    {
        /**
         * Instantiates a new oBO format exception.
         */
        public OBOFormatException()
        {
        }

        /**
         * Instantiates a new oBO format exception.
         *
         * @param message the message
         */
        public OBOFormatException(string message)
            : base(message)
        {
        }

        /**
         * Instantiates a new oBO format exception.
         *
         * @param e the e
         */
        public OBOFormatException(Exception e)
            : base(e)
        {
        }

        /**
         * Instantiates a new oBO format exception.
         *
         * @param message the message
         * @param e the e
         */
        public OBOFormatException(string message, Exception e)
            : base(message, e)
        {
        }
    }
}
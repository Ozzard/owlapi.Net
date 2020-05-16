#nullable enable

using System;

namespace org.obolibrary.oboformat.parser
{
    /**
     * The Class OBOFormatParserException.
     */
    public class OBOFormatParserException : OBOFormatException
    {

        public int LineNo { get; }
        public string? Line { get; }

        /**
         * @param message the message
         * @param e the cause
         * @param lineNo the line no
         * @param line the line
         */
        public OBOFormatParserException(string message, Exception e, int lineNo, string? line)
            : base(message, e)
        {
            LineNo = lineNo;
            Line = line;
        }

        /**
         * @param message the message
         * @param lineNo the line no
         * @param line the line
         */
        public OBOFormatParserException(string message, int lineNo, string? line)
            : base(message)
        {
            LineNo = lineNo;
            Line = line;
        }

        /**
         * @param e the cause
         * @param lineNo the line no
         * @param line the line
         */
        public OBOFormatParserException(Exception e, int lineNo, string? line)
            : base(e)
        {
            LineNo = lineNo;
            Line = line;
        }

        public override string Message => $"LINENO: {LineNo} - {base.Message}{Environment.NewLine}LINE: {Line}";

        public override string ToString() => Message;
    }
}
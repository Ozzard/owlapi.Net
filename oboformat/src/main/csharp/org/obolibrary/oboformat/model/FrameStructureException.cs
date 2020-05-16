#nullable enable

namespace org.obolibrary.oboformat.model
{

    public class FrameStructureException : DocumentStructureException
    {

        /**
         * Instantiates a new frame structure exception.
         *
         * @param msg the msg
         */
        public FrameStructureException(string msg)
            : base(msg)
        {
        }

        /**
         * Instantiates a new frame structure exception.
         *
         * @param frame the frame
         * @param msg the msg
         */
        public FrameStructureException(Frame frame, string msg)
            : base(msg + " in frame:" + frame)
        {
        }
    }
}
using System;
using System.Runtime.Serialization;

namespace KT.Content.Data.Exceptions
{
    /// <summary>
    /// Process exception. Represents issues retrieving content from the CMS
    /// that are likely recoverable.
    /// </summary>
    [Serializable]
    public class ProcessException : System.Exception
    {
        public ProcessException()
        {
        }

        public ProcessException(string message)
            : base(message)
        {
        }

        public ProcessException(string message, System.Exception inner)
            : base(message, inner)
        {
        }

        protected ProcessException(SerializationInfo si, StreamingContext ctx)
            : base(si, ctx)
        {   
        }
    }
}
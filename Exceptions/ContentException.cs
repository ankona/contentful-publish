using System;
using System.Runtime.Serialization;

namespace KT.Content.Data.Exceptions
{
    /// <summary>
    /// Content exception. Represents issues retrieving content from the CMS
    /// that are due to invalid inputs.
    /// </summary>
    [Serializable]
    public class ContentException : System.Exception
    {
        public ContentException()
        {
        }

        public ContentException(string message)
            : base(message)
        {
        }

        public ContentException(string message, System.Exception inner)
            : base(message, inner)
        {
        }

        protected ContentException(SerializationInfo si, StreamingContext ctx)
            : base(si, ctx)
        {   
        }
    }
}
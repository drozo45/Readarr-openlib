using System;

namespace NzbDrone.Core.MetadataSource.OpenLibrary
{
    public class OpenLibraryException : Exception
    {
        public OpenLibraryException(string message) : base(message)
        {
        }

        public OpenLibraryException(string message, params object[] args) : base(string.Format(message, args))
        {
        }

        public OpenLibraryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public OpenLibraryException(string message, Exception innerException, params object[] args) : base(string.Format(message, args), innerException)
        {
        }
    }
}

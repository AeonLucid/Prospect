using System.Runtime.Serialization;

namespace Prospect.Server.Api.Exceptions;

public class ProspectException : Exception
{
    public ProspectException()
    {
    }

    protected ProspectException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ProspectException(string message) : base(message)
    {
    }

    public ProspectException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
namespace InterfaceAggregate;

public class FacadeRegistrationException : Exception
{
    public FacadeRegistrationException(string message) : base(message)
    {
    }

    public FacadeRegistrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
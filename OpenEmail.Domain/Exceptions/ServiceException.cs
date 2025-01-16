namespace OpenEmail.Domain.Exceptions
{
    public class ServiceException : System.Exception
    {
        public ServiceException(string message) : base(message)
        {
        }
    }
}

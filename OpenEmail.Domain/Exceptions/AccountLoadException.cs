namespace OpenEmail.Domain.Exceptions
{
    public class AccountLoadException : System.Exception
    {
        public AccountLoadException(string message) : base(message) { }
    }
}

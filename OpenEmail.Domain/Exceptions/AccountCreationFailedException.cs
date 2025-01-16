namespace OpenEmail.Domain.Exceptions
{
    public class AccountCreationFailedException : Exception
    {
        public AccountCreationFailedException(string message) : base(message)
        {
        }
    }
}

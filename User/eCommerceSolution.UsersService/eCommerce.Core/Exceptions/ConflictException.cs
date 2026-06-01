namespace eCommerce.Core.Exceptions
{
    public class ConflictException : BusinessException
    {
        public ConflictException(string message) : base(message)
        {
        }
    }
}

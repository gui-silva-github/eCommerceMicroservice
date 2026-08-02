namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Exceptions
{
    public class ExternalServiceUnavailableException : Exception
    {
        public ExternalServiceUnavailableException(string message) : base(message)
        {
        }

        public ExternalServiceUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

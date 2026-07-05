namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Exceptions
{
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message)
        {
        }
    }
}

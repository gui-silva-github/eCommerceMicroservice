namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO
{
    public record OrderItemAddRequest(Guid ProductID, decimal UnitPrice, int Quantity)
    {
        public OrderItemAddRequest() : this(default, default, default) { }
    }
}
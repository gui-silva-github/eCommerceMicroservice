namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO
{
    public record OrderAddRequest(Guid UserID, DateTime OrderDate, List<OrderItemAddRequest> OrderItems)
    {
        public OrderAddRequest() : this(default, default, new List<OrderItemAddRequest>()) { }
    }
}
namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO
{
    public record OrderItemResponse(
        Guid ProductID,
        decimal UnitPrice,
        int Quantity,
        decimal TotalPrice,
        string? ProductName = null,
        string? Category = null)
    {
        public OrderItemResponse() : this(default, default, default, default)
        {
        }
    }
}

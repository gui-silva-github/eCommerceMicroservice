namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ
{
    public record ProductEventMessage(
        Guid ProductID,
        string? ProductName,
        string? Category,
        double? UnitPrice,
        int? QuantityInStock,
        string EventType);
}

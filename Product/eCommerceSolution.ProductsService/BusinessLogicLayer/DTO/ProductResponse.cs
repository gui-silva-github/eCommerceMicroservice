namespace eCommerce.BusinessLogicLayer.DTO
{
    public record ProductResponse(Guid ProductID, string ProductName, CategoryOptions Category, double? UnitPrice, int? QuantityInStock)
    {
        public ProductResponse() : this(default, default!, default, default, default) { }
    }
}
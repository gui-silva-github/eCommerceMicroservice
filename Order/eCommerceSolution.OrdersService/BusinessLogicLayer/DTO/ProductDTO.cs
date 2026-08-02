namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO
{
    /// <summary>
    /// Contrato espelhado do ProductsService para comunicação síncrona via HttpClient.
    /// </summary>
    public record ProductDTO(
        Guid ProductID,
        string? ProductName,
        string? Category,
        double? UnitPrice,
        int? QuantityInStock
    )
    {
        public ProductDTO() : this(default, default, default, default, default)
        {
        }
    }
}

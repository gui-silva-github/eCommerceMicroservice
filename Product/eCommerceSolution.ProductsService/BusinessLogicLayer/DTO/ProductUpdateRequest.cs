using System.Text.Json.Serialization;
using eCommerce.BusinessLogicLayer.Json;

namespace eCommerce.BusinessLogicLayer.DTO
{
    public record ProductUpdateRequest(
        Guid ProductID,
        string ProductName,
        [property: JsonConverter(typeof(NullableCategoryOptionsJsonConverter))] CategoryOptions? Category,
        double? UnitPrice,
        int? QuantityInStock)
    {
        public ProductUpdateRequest() : this(default, default!, default, default, default) { }
    }
}
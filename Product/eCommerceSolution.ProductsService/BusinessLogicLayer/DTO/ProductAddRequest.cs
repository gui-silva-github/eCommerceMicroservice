using System.Text.Json.Serialization;
using eCommerce.BusinessLogicLayer.Json;

namespace eCommerce.BusinessLogicLayer.DTO
{
    public record ProductAddRequest(
        string ProductName,
        [property: JsonConverter(typeof(NullableCategoryOptionsJsonConverter))] CategoryOptions? Category,
        double? UnitPrice,
        int? QuantityInStock)
    {
        public ProductAddRequest() : this(default!, default, default, default) { }
    }
}
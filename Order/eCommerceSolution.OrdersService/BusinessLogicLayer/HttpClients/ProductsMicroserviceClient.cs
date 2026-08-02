using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Exceptions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients
{
    public class ProductsMicroserviceClient : IProductsMicroserviceClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductsMicroserviceClient> _logger;

        public ProductsMicroserviceClient(HttpClient httpClient, ILogger<ProductsMicroserviceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ProductDTO?> GetProductByProductID(Guid productID)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(
                    $"/api/products/search/product-id/{productID}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "ProductsService retornou {StatusCode} ao buscar produto {ProductID}. Body: {Body}",
                        (int)response.StatusCode,
                        productID,
                        body);

                    throw new ExternalServiceUnavailableException(
                        "ProductsService indisponível ou retornou erro ao validar o produto.");
                }

                using Stream stream = await response.Content.ReadAsStreamAsync();
                using JsonDocument document = await JsonDocument.ParseAsync(stream);

                JsonElement root = document.RootElement;

                return new ProductDTO(
                    RootGuid(root, "productID", "ProductID"),
                    RootString(root, "productName", "ProductName"),
                    RootString(root, "category", "Category"),
                    RootDouble(root, "unitPrice", "UnitPrice"),
                    RootInt(root, "quantityInStock", "QuantityInStock"));
            }
            catch (ExternalServiceUnavailableException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Falha de rede ao chamar ProductsService para o produto {ProductID}.", productID);
                throw new ExternalServiceUnavailableException(
                    "Não foi possível conectar ao ProductsService.",
                    ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout ao chamar ProductsService para o produto {ProductID}.", productID);
                throw new ExternalServiceUnavailableException(
                    "Timeout ao conectar ao ProductsService.",
                    ex);
            }
        }

        private static Guid RootGuid(JsonElement root, params string[] names)
        {
            foreach (string name in names)
            {
                if (root.TryGetProperty(name, out JsonElement value) &&
                    value.ValueKind != JsonValueKind.Null &&
                    Guid.TryParse(value.ToString(), out Guid guid))
                {
                    return guid;
                }
            }

            return Guid.Empty;
        }

        private static string? RootString(JsonElement root, params string[] names)
        {
            foreach (string name in names)
            {
                if (root.TryGetProperty(name, out JsonElement value) &&
                    value.ValueKind != JsonValueKind.Null)
                {
                    return value.ToString();
                }
            }

            return null;
        }

        private static double? RootDouble(JsonElement root, params string[] names)
        {
            foreach (string name in names)
            {
                if (root.TryGetProperty(name, out JsonElement value) &&
                    value.ValueKind != JsonValueKind.Null &&
                    value.TryGetDouble(out double number))
                {
                    return number;
                }
            }

            return null;
        }

        private static int? RootInt(JsonElement root, params string[] names)
        {
            foreach (string name in names)
            {
                if (root.TryGetProperty(name, out JsonElement value) &&
                    value.ValueKind != JsonValueKind.Null &&
                    value.TryGetInt32(out int number))
                {
                    return number;
                }
            }

            return null;
        }
    }
}

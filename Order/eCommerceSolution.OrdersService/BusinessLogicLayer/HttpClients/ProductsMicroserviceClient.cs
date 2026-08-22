using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Exceptions;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;
using Microsoft.Extensions.Caching.Distributed;
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
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<ProductsMicroserviceClient> _logger;
        private readonly ResilienceOptions _resilienceOptions;

        public ProductsMicroserviceClient(
            HttpClient httpClient,
            IDistributedCache distributedCache,
            ILogger<ProductsMicroserviceClient> logger,
            ResilienceOptions resilienceOptions)
        {
            _httpClient = httpClient;
            _distributedCache = distributedCache;
            _logger = logger;
            _resilienceOptions = resilienceOptions;
        }

        public async Task<ProductDTO?> GetProductByProductID(Guid productID)
        {
            string cacheKey = $"{productID}";
            ProductDTO? cached = await TryGetCacheAsync<ProductDTO>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(
                    $"/api/products/search/product-id/{productID}");

                if (response.Headers.Contains(ResiliencePolicies.FallbackHeader))
                {
                    FaultDTO? fault = await response.Content.ReadFromJsonAsync<FaultDTO>(JsonOptions);
                    _logger.LogWarning("Fallback Polly ao buscar produto {ProductID}: {@Fault}", productID, fault);
                    throw new ExternalServiceUnavailableException(
                        fault?.Message ?? "ProductsService indisponível (fallback).");
                }

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

                ProductDTO product = new(
                    RootGuid(root, "productID", "ProductID"),
                    RootString(root, "productName", "ProductName"),
                    RootString(root, "category", "Category"),
                    RootDouble(root, "unitPrice", "UnitPrice"),
                    RootInt(root, "quantityInStock", "QuantityInStock"));

                await TrySetCacheAsync(cacheKey, product);
                return product;
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

        private async Task<T?> TryGetCacheAsync<T>(string cacheKey)
        {
            try
            {
                string? cachedEntity = await _distributedCache.GetStringAsync(cacheKey);
                if (!string.IsNullOrWhiteSpace(cachedEntity))
                {
                    return JsonSerializer.Deserialize<T>(cachedEntity, JsonOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis indisponível ao ler a chave {CacheKey}.", cacheKey);
            }

            return default;
        }

        private async Task TrySetCacheAsync<T>(string cacheKey, T value)
        {
            try
            {
                DistributedCacheEntryOptions options = new()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_resilienceOptions.CacheSeconds)
                };

                await _distributedCache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(value, JsonOptions),
                    options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis indisponível ao gravar a chave {CacheKey}.", cacheKey);
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

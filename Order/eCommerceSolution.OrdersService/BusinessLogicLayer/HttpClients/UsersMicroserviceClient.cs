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
    public class UsersMicroserviceClient : IUsersMicroserviceClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly HttpClient _httpClient;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<UsersMicroserviceClient> _logger;
        private readonly ResilienceOptions _resilienceOptions;

        public UsersMicroserviceClient(
            HttpClient httpClient,
            IDistributedCache distributedCache,
            ILogger<UsersMicroserviceClient> logger,
            ResilienceOptions resilienceOptions)
        {
            _httpClient = httpClient;
            _distributedCache = distributedCache;
            _logger = logger;
            _resilienceOptions = resilienceOptions;
        }

        public async Task<UserDTO?> GetUserByUserID(Guid userID)
        {
            string cacheKey = $"{userID}";
            UserDTO? cached = await TryGetCacheAsync<UserDTO>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/Users/{userID}");

                if (response.Headers.Contains(ResiliencePolicies.FallbackHeader))
                {
                    FaultDTO? fault = await response.Content.ReadFromJsonAsync<FaultDTO>(JsonOptions);
                    _logger.LogWarning("Fallback Polly ao buscar usuário {UserID}: {@Fault}", userID, fault);
                    throw new ExternalServiceUnavailableException(
                        fault?.Message ?? "UsersService indisponível (fallback).");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "UsersService retornou {StatusCode} ao buscar usuário {UserID}. Body: {Body}",
                        (int)response.StatusCode,
                        userID,
                        body);

                    throw new ExternalServiceUnavailableException(
                        "UsersService indisponível ou retornou erro ao validar o usuário.");
                }

                UserDTO? user = await response.Content.ReadFromJsonAsync<UserDTO>(JsonOptions);
                if (user != null)
                {
                    await TrySetCacheAsync(cacheKey, user);
                }

                return user;
            }
            catch (ExternalServiceUnavailableException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Falha de rede ao chamar UsersService para o usuário {UserID}.", userID);
                throw new ExternalServiceUnavailableException(
                    "Não foi possível conectar ao UsersService.",
                    ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout ao chamar UsersService para o usuário {UserID}.", userID);
                throw new ExternalServiceUnavailableException(
                    "Timeout ao conectar ao UsersService.",
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
    }
}

using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Exceptions;
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
        private readonly ILogger<UsersMicroserviceClient> _logger;

        public UsersMicroserviceClient(HttpClient httpClient, ILogger<UsersMicroserviceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<UserDTO?> GetUserByUserID(Guid userID)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/Users/{userID}");

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

                return await response.Content.ReadFromJsonAsync<UserDTO>(JsonOptions);
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
    }
}

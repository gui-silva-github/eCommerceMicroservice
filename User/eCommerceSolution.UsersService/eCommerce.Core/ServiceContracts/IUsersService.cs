using eCommerce.Core.DTO;

namespace eCommerce.Core.ServiceContracts
{
    public interface IUsersService
    {
        Task<AuthenticationResponse> Login(LoginRequest loginRequest);

        Task<AuthenticationResponse> Register(RegisterRequest registerRequest);

        Task<UserDTO> GetUserByUserID(Guid userID);

        /// <summary>
        /// Cria ou atualiza o perfil local a partir das claims do Entra External ID (upsert por oid).
        /// </summary>
        Task<UserDTO> SyncExternalUser(ExternalUserInfo externalUser);
    }
}

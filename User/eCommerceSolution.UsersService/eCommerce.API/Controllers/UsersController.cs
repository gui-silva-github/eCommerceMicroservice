using eCommerce.Core.DTO;
using eCommerce.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;
        private readonly IConfiguration _configuration;

        public UsersController(IUsersService usersService, IConfiguration configuration)
        {
            _usersService = usersService;
            _configuration = configuration;
        }

        /// <summary>
        /// Perfil do usuário logado no Microsoft Entra External ID.
        /// Na primeira chamada cria a linha em "Users" com UserID = oid (o Orders valida esse ID);
        /// nas seguintes atualiza nome/e-mail. IsAdmin vem do app role do token.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCurrentUser()
        {
            string requiredScope = _configuration["EntraExternalId:RequiredScope"] ?? "Ecommerce.Access";
            string adminRole = _configuration["EntraExternalId:AdminRole"] ?? "Admin";
            string genderClaim = _configuration["EntraExternalId:GenderClaim"] ?? "gender";

            // "scp" é uma string separada por espaço com os scopes delegados.
            string[] scopes = (User.FindFirst("scp")?.Value ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (!scopes.Contains(requiredScope, StringComparer.Ordinal))
            {
                return Forbid();
            }

            Guid.TryParse(User.FindFirst("oid")?.Value, out Guid objectId);

            var externalUser = new ExternalUserInfo(
                ObjectId: objectId,
                Email: User.FindFirst("email")?.Value ?? User.FindFirst("preferred_username")?.Value,
                PersonName: User.FindFirst("name")?.Value,
                Gender: User.FindFirst(genderClaim)?.Value);

            UserDTO user = await _usersService.SyncExternalUser(externalUser);

            return Ok(new CurrentUserResponse(
                user.UserID,
                user.Email,
                user.PersonName,
                user.Gender,
                IsAdmin: User.IsInRole(adminRole)));
        }

        /// <summary>
        /// Retorna dados públicos do usuário por ID (sem senha).
        /// Usado por outros microserviços via comunicação síncrona (HttpClient).
        /// </summary>
        [HttpGet("{userID:guid}")]
        [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserByUserID(Guid userID)
        {
            UserDTO user = await _usersService.GetUserByUserID(userID);
            return Ok(user);
        }
    }
}

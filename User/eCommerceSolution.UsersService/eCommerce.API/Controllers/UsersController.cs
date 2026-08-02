using eCommerce.Core.DTO;
using eCommerce.Core.ServiceContracts;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;

        public UsersController(IUsersService usersService)
        {
            _usersService = usersService;
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

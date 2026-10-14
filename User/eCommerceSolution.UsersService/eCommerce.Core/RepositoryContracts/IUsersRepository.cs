using eCommerce.Core.Entities;

namespace eCommerce.Core.RepositoryContracts
{
    public interface IUsersRepository
    {
        Task<ApplicationUser> AddUser(ApplicationUser user);

        Task<ApplicationUser?> GetUserByEmail(string email);

        Task<ApplicationUser?> GetUserByEmailAndPassword(string? email, string? password);

        Task<ApplicationUser?> GetUserByUserID(Guid userID);

        /// <summary>
        /// INSERT ... ON CONFLICT ("UserID") DO UPDATE. Usuários do Entra não têm senha local (Password = NULL).
        /// </summary>
        Task<ApplicationUser> UpsertExternalUser(ApplicationUser user);
    }
}

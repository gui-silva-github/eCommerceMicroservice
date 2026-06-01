using Dapper;
using eCommerce.Core.Entities;
using eCommerce.Core.Exceptions;
using eCommerce.Core.RepositoryContracts;
using eCommerce.Infrastructure.DbContext;
using Npgsql;

namespace eCommerce.Infrastructure.Repositories
{
    internal class UsersRepository : IUsersRepository
    {
        private readonly DapperDbContext _dbContext;

        public UsersRepository(DapperDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ApplicationUser> AddUser(ApplicationUser user)
        {
            user.UserID = Guid.NewGuid();

            const string query = """
                INSERT INTO public."Users"("UserID", "Email", "PersonName", "Gender", "Password")
                VALUES(@UserID, @Email, @PersonName, @Gender, @Password)
                """;

            try
            {
                int rowCountAffected = await _dbContext.DbConnection.ExecuteAsync(query, user);

                if (rowCountAffected <= 0)
                {
                    throw new BusinessException("Não foi possível cadastrar o usuário.");
                }

                return user;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new ConflictException("Email já cadastrado.");
            }
        }

        public async Task<ApplicationUser?> GetUserByEmail(string email)
        {
            const string query = """SELECT * FROM public."Users" WHERE "Email" = @Email""";

            return await _dbContext.DbConnection.QueryFirstOrDefaultAsync<ApplicationUser>(
                query,
                new { Email = email });
        }

        public async Task<ApplicationUser?> GetUserByEmailAndPassword(string? email, string? password)
        {
            const string query = """
                SELECT * FROM public."Users"
                WHERE "Email" = @Email AND "Password" = @Password
                """;

            return await _dbContext.DbConnection.QueryFirstOrDefaultAsync<ApplicationUser>(
                query,
                new { Email = email, Password = password });
        }
    }
}

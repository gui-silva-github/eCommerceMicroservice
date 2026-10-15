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

        public async Task<ApplicationUser> UpsertExternalUser(ApplicationUser user)
        {
            // COALESCE no Gender: se o token não trouxer a claim, mantém o que já estava salvo.
            const string query = """
                INSERT INTO public."Users"("UserID", "Email", "PersonName", "Gender", "Password")
                VALUES(@UserID, @Email, @PersonName, @Gender, NULL)
                ON CONFLICT ("UserID") DO UPDATE SET
                    "Email" = EXCLUDED."Email",
                    "PersonName" = EXCLUDED."PersonName",
                    "Gender" = COALESCE(EXCLUDED."Gender", public."Users"."Gender")
                RETURNING *
                """;

            try
            {
                return await _dbContext.DbConnection.QuerySingleAsync<ApplicationUser>(query, user);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                // Mesmo e-mail já usado por uma conta local antiga (ex.: seed admin@gmail.com) com outro UserID.
                throw new ConflictException("Email já cadastrado por uma conta local. Use outro e-mail no Entra ou remova a conta local.");
            }
        }

        public async Task<ApplicationUser?> GetUserByUserID(Guid userID)
        {
            const string query = """SELECT * FROM public."Users" WHERE "UserID" = @UserID""";

            return await _dbContext.DbConnection.QueryFirstOrDefaultAsync<ApplicationUser>(
                query,
                new { UserID = userID });
        }
    }
}

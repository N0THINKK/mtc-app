using System.Threading.Tasks;
using Dapper;
using mtc_app.shared.data.dtos;

namespace mtc_app.features.authentication.data.repositories
{
    public class AuthRepository : IAuthRepository
    {
        public async Task<UserDto> LoginAsync(string username, string password)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // NOTE: Password check is currently Plain Text to support existing database data.
                // In a future update, migrate to hashed passwords (e.g., BCrypt).
                string sql = @"
                    SELECT 
                        u.user_id AS UserId, 
                        u.username AS Username, 
                        u.full_name AS FullName, 
                        u.role_id AS RoleId, 
                        r.role_name AS RoleName 
                    FROM users u 
                    JOIN roles r ON u.role_id = r.role_id 
                    WHERE u.username = @Username AND u.password = @Password 
                    LIMIT 1";

                return await connection.QueryFirstOrDefaultAsync<UserDto>(sql, new { Username = username, Password = password });
            }
        }
    }
}

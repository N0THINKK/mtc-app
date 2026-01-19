using System;
using System.Linq;
using Dapper;
using mtc_app.shared.data.dtos;

namespace mtc_app.shared.data.services
{
    public class AuthService
    {
        public UserDto Login(string username, string password)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                // Note: In production, password should be hashed. keeping simple for now as per existing codebase.
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
                
                return connection.QueryFirstOrDefault<UserDto>(sql, new { Username = username, Password = password });
            }
        }
    }
}

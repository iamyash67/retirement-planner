using MySql.Data.MySqlClient;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;

namespace RetirementPlanner.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IConfiguration _config;
        private readonly ILogger<UserRepository> _logger;
        private readonly string _connString;

        public UserRepository(IConfiguration config, ILogger<UserRepository> logger)
        {
            _config = config;
            _logger = logger;
            _connString = _config.GetConnectionString("DefaultConnection");
        }

        public async Task<Profile?> ValidateCredentialsAsync(string username, string password)
        {
            await using var conn = new MySqlConnection(_connString);
            try
            {
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("CALL ValidateLogin(@UserName,@Password)", conn);
                cmd.Parameters.AddWithValue("@UserName", username);
                cmd.Parameters.AddWithValue("@Password", password);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Profile
                    {
                        ProfileId = reader.GetInt32(reader.GetOrdinal("ProfileId")),
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        Age = reader.GetInt32(reader.GetOrdinal("Age")),
                        Gender = reader.GetString(reader.GetOrdinal("Gender")),
                        UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    };
                }
                return null;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error validating credentials for {Username}", username);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error validating credentials for {Username}", username);
                return null;
            }
        }
    }
}

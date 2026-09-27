using System.Data;
using MySql.Data.MySqlClient;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.DTO;

namespace RetirementPlanner.Repositories
{
    public class GoalRepository : IGoalRepository
    {
        private readonly IConfiguration _config;
        private readonly ILogger<GoalRepository> _logger;

        public GoalRepository(IConfiguration config, ILogger<GoalRepository> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<Goal?> GetByProfileIdAsync(int profileId)
        {
            var connString = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connString);
            try
            {
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("CALL GetGoal(@ProfileId)", conn);
                cmd.Parameters.AddWithValue("@ProfileId", profileId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Goal
                    {
                        GoalId = reader.GetInt32("GoalId"),
                        ProfileId = reader.GetInt32("ProfileId"),
                        CurrentAge = reader.GetInt32("CurrentAge"),
                        RetirementAge = reader.GetInt32("RetirementAge"),
                        TargetSavings = reader.GetDecimal("TargetSavings"),
                        MonthlyContribution = reader.GetDecimal("MonthlyContribution"),
                        CurrentSavings = reader.GetDecimal("CurrentSavings")
                    };
                }
                return null;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error retrieving goal with profileID {profileId}", profileId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error retrieving goal with profileID {profileId}", profileId);
                return null;
            }
        }

        public async Task<bool> ExistsByProfileIdAsync(int profileId)
        {
            var connString = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connString);
            try
            {
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("SELECT COUNT(1) FROM Goals WHERE ProfileId = @ProfileId", conn);
                cmd.Parameters.AddWithValue("@ProfileId", profileId);

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error checking existence for ProfileId {ProfileId}", profileId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error checking existence for ProfileId {ProfileId}", profileId);
                return false;
            }
        }

        public async Task<bool> ExistsByGoalIdAsync(int goalId)
        {
            var connString = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connString);
            try
            {
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("SELECT COUNT(1) FROM Goals WHERE GoalId = @GoalId", conn);
                cmd.Parameters.AddWithValue("@GoalId", goalId);

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error checking existence for GoalId {GoalId}", goalId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error checking existence for GoalId {GoalId}", goalId);
                return false;
            }
        }

        public async Task<bool> CreateAsync(GoalDTO goal)
        {
            var connString = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connString);
            try
            {
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("CALL CreateGoal(@ProfileId, @CurrentAge, @RetirementAge, @TargetSavings, @CurrentSavings)", conn);
                cmd.Parameters.AddWithValue("@ProfileId", goal.ProfileId);
                cmd.Parameters.AddWithValue("@CurrentAge", goal.CurrentAge);
                cmd.Parameters.AddWithValue("@RetirementAge", goal.RetirementAge);
                cmd.Parameters.AddWithValue("@TargetSavings", goal.TargetSavings);
                cmd.Parameters.AddWithValue("@CurrentSavings", goal.CurrentSavings);

                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error creating goal for ProfileId {ProfileId}", goal.ProfileId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error creating goal for ProfileId {ProfileId}", goal.ProfileId);
                return false;
            }
        }

        public async Task<int?> GetProfileIdByGoalIdAsync(int goalId)
        {
            var connString = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connString);
            try
            {
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("SELECT ProfileId FROM Goals WHERE GoalId = @GoalId", conn);
                cmd.Parameters.AddWithValue("@GoalId", goalId);

                var result = await cmd.ExecuteScalarAsync();
                if (result != null && int.TryParse(result.ToString(), out int profileId))
                    return profileId;

                return null;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error fetching ProfileId for GoalId {GoalId}", goalId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error fetching ProfileId for GoalId {GoalId}", goalId);
                return null;
            }
        }
        public async Task<Goal?> GetByGoalIdAsync(int goalId)
        {
            var connString = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connString);
            try
            {
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("SELECT * FROM Goals WHERE GoalId = @GoalId", conn);
                cmd.Parameters.AddWithValue("@GoalId", goalId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Goal
                    {
                        GoalId = reader.GetInt32("GoalId"),
                        ProfileId = reader.GetInt32("ProfileId"),
                        CurrentAge = reader.GetInt32("CurrentAge"),
                        RetirementAge = reader.GetInt32("RetirementAge"),
                        TargetSavings = reader.GetDecimal("TargetSavings"),
                        MonthlyContribution = reader.GetDecimal("MonthlyContribution"),
                        CurrentSavings = reader.GetDecimal("CurrentSavings")
                    };
                }
                return null;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error retrieving goal with GoalId {GoalId}", goalId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error retrieving goal with GoalId {GoalId}", goalId);
                return null;
            }
        }
        public async Task<decimal?> GetUserProgressAsync(int goalId)
        {
            var connString = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connString);
            try
            {
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("CALL GetProgress(@GoalId)", conn);
                cmd.Parameters.AddWithValue("@GoalId", goalId);

                var result = await cmd.ExecuteScalarAsync();
                return result != null ? Convert.ToDecimal(result) : (decimal?)null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetTotalSavingsByGoalId for GoalId: {GoalId}", goalId);
                return null;
            }
        }
    }
}

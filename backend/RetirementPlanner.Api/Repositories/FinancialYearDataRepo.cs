using System.Data;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using MySql.Data.MySqlClient;

namespace RetirementPlanner.Repositories
{
    public class FinancialYearDataRepo : IFinancialYearDataRepo
    {
        private readonly IConfiguration _config;
        private readonly ILogger<FinancialYearDataRepo> _logger;
        private readonly string _connString;

        public FinancialYearDataRepo(IConfiguration config, ILogger<FinancialYearDataRepo> logger)
        {
            _config = config;
            _logger = logger;
            _connString = _config.GetConnectionString("DefaultConnection");
        }

        public async Task<FinancialYearData?> GetByProfileAndYearAsync(int goalId, int year, int month)
        {
            await using var conn = new MySqlConnection(_connString);
            try
            {
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("CALL GetFinancialYearData(@GoalId, @Year, @Month)", conn);
                cmd.Parameters.AddWithValue("@GoalId", goalId);
                cmd.Parameters.AddWithValue("@Year", year);
                cmd.Parameters.AddWithValue("@Month", month);

                using MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                return await reader.ReadAsync() ? MapFinancialData(reader) : null;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error retrieving financial data for GoalId: {GoalId}, Year: {Year}, Month: {Month}", goalId, year, month);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error retrieving financial data for GoalId: {GoalId}", goalId);
                return null;
            }
        }

        public async Task<FinancialYearData?> CreateOrUpdateAsync(FinancialYearData data)
        {
            await using var conn = new MySqlConnection(_connString);
            try
            {
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("CALL CreateFinancialYearData(@GoalId, @Year, @Month, @Amount)", conn);
                cmd.Parameters.AddWithValue("@GoalId", data.GoalId);
                cmd.Parameters.AddWithValue("@Year", data.Year);
                cmd.Parameters.AddWithValue("@Month", data.Month);
                cmd.Parameters.AddWithValue("@Amount", data.MonthlyInvestment);

                using MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                return await reader.ReadAsync() ? MapFinancialData(reader) : null;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error creating/updating financial data for GoalId: {GoalId}", data.GoalId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error creating/updating financial data for GoalId: {GoalId}", data.GoalId);
                return null;
            }
        }

        public async Task<bool> MarkAsInvestedAsync(int recordId)
        {
            await using var conn = new MySqlConnection(_connString);
            try
            {
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("CALL MarkFinancialInvestment(@Id)", conn);
                cmd.Parameters.AddWithValue("@Id", recordId);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error marking investment for RecordId: {RecordId}", recordId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error marking investment for RecordId: {RecordId}", recordId);
                return false;
            }
        }

        private FinancialYearData MapFinancialData(MySqlDataReader reader)
        {
            try
            {
                return new FinancialYearData
                {
                    Id = reader.GetInt32("Id"),
                    GoalId = reader.GetInt32("GoalId"),
                    Year = reader.GetInt32("Year"),
                    Month = reader.GetInt32("Month"),
                    MonthlyInvestment = reader.GetDecimal("MonthlyInvestment"),
                    IsInvested = reader.GetBoolean("IsInvested"),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping FinancialYearData");
                throw;
            }
        }
    }
}

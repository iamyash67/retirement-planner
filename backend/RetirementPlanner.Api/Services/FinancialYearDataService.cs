using System.Data;
using MySql.Data.MySqlClient;
using RetirementPlanner.Models;
using RetirementPlanner.Services.Interfaces;
using RetirementPlanner.Repositories.Interfaces;

namespace RetirementPlanner.Services
{
    public class FinancialYearDataService : IFinancialYearDataService
    {
        private readonly IFinancialYearDataRepo _repo;
        private readonly string _connectionString;
        private readonly ILogger<FinancialYearDataService> _logger;

        public FinancialYearDataService(
            IFinancialYearDataRepo repo,
            IConfiguration config,
            ILogger<FinancialYearDataService> logger)
        {
            _repo = repo;
            _connectionString = config.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public async Task<FinancialYearData?> GetFinancialDataAsync(int goalId, int year, int month)
        {
            _logger.LogInformation("Fetching financial data for GoalId: {GoalId}, Year: {Year}, Month: {Month}",
                goalId, year, month);

            try
            {
                return await _repo.GetByProfileAndYearAsync(goalId, year, month);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving financial data for GoalId: {GoalId}", goalId);
                throw;
            }
        }

        public async Task<FinancialYearData?> CreateOrUpdateFinancialDataAsync(FinancialYearData data)
        {
            _logger.LogInformation("Creating/Updating financial data for GoalId: {GoalId}", data.GoalId);

            try
            {
                return await _repo.CreateOrUpdateAsync(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating financial data for GoalId: {GoalId}", data.GoalId);
                throw;
            }
        }

        public async Task<bool> RecordInvestmentAsync(int recordId)
        {
            _logger.LogInformation("Marking investment for RecordId: {RecordId}", recordId);

            try
            {
                return await _repo.MarkAsInvestedAsync(recordId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking investment for RecordId: {RecordId}", recordId);
                throw;
            }
        }
        public IDbTransaction BeginTransaction()
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn.BeginTransaction();
        }
    }
}

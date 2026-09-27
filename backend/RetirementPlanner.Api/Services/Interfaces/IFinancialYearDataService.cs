using System.Data;
using RetirementPlanner.Models;

namespace RetirementPlanner.Services.Interfaces
{
    public interface IFinancialYearDataService
    {
        Task<FinancialYearData?> GetFinancialDataAsync(int goalId, int year, int month);
        Task<FinancialYearData?> CreateOrUpdateFinancialDataAsync(FinancialYearData data);
        Task<bool> RecordInvestmentAsync(int recordId);
        IDbTransaction BeginTransaction();
    }
}

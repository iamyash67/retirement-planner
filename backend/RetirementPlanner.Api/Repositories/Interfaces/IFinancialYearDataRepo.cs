using RetirementPlanner.Models;

namespace RetirementPlanner.Repositories.Interfaces
{
    public interface IFinancialYearDataRepo
    {
        Task<FinancialYearData?> GetByProfileAndYearAsync(int goalId, int year, int month);
        Task<FinancialYearData?> CreateOrUpdateAsync(FinancialYearData data);
        Task<bool> MarkAsInvestedAsync(int recordId);
    }
}

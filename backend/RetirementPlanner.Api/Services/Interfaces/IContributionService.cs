using RetirementPlanner.DTO;
using RetirementPlanner.Models;

namespace RetirementPlanner.Services.Interfaces
{
    public interface IContributionService
    {
        /// <summary>Records one month's investment for a goal in a single transaction.</summary>
        Task<ContributionResult> RecordAsync(FinancialDTO request, CancellationToken cancellationToken = default);
    }
}

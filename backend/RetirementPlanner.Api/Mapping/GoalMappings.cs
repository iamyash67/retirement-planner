using System.Globalization;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.DTO.Responses;
using RetirementPlanner.Models;

namespace RetirementPlanner.Mapping
{
    public static class GoalMappings
    {
        public static CreateGoalCommand ToCommand(this CreateGoalRequest request) => new(
            UserId: request.ProfileId,
            CurrentAge: request.CurrentAge,
            RetirementAge: request.RetirementAge,
            TargetAmount: request.TargetSavings,
            CurrentSavings: request.CurrentSavings,
            Name: request.Name,
            ExpectedAnnualReturn: request.ExpectedAnnualReturn,
            ReturnVolatility: request.ReturnVolatility,
            InflationRate: request.InflationRate,
            AnnualContributionIncrease: request.AnnualContributionIncrease);

        public static GoalResponse ToResponse(this Goal goal) => new(
            ProfileId: goal.UserId,
            GoalId: goal.Id,
            CurrentAge: goal.CurrentAge,
            RetirementAge: goal.RetirementAge,
            TargetSavings: goal.TargetAmount,
            MonthlyContribution: goal.PlannedMonthlyContribution,
            CurrentSavings: goal.CurrentSavings);

        /// <summary>Formats a percentage as "12.34%", rounded to two decimals, independent of server culture.</summary>
        public static ProgressResponse ToProgressResponse(this decimal percentage, int goalId) => new(
            GoalId: goalId,
            Progress: string.Create(CultureInfo.InvariantCulture, $"{Math.Round(percentage, 2)}%"));
    }
}

using System.Globalization;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.DTO.Responses;
using RetirementPlanner.Models;

namespace RetirementPlanner.Mapping
{
    public static class GoalMappings
    {
        /// <summary>The owner is the authenticated user; the request has no say in it.</summary>
        public static CreateGoalCommand ToCommand(this CreateGoalRequest request, int userId) => new(
            UserId: userId,
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
            Id: goal.Id,
            Name: goal.Name,
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

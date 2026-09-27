using System.Globalization;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Mapping;
using RetirementPlanner.Models;

namespace RetirementPlanner.Tests.Mapping
{
    public class GoalMappingsTests
    {
        [Fact]
        public void CreateGoalRequest_ToCommand_MapsEveryField()
        {
            var request = new CreateGoalRequest
            {
                CurrentAge = 30,
                RetirementAge = 65,
                TargetSavings = 900_000m,
                CurrentSavings = 50_000m,
                Name = "Early",
                ExpectedAnnualReturn = 0.07m,
                ReturnVolatility = 0.15m,
                InflationRate = 0.03m,
                AnnualContributionIncrease = 0.02m
            };

            var command = request.ToCommand(userId: 3);

            Assert.Equal(new CreateGoalCommand(
                UserId: 3, CurrentAge: 30, RetirementAge: 65, TargetAmount: 900_000m, CurrentSavings: 50_000m,
                Name: "Early", ExpectedAnnualReturn: 0.07m, ReturnVolatility: 0.15m, InflationRate: 0.03m,
                AnnualContributionIncrease: 0.02m), command);
        }

        [Fact]
        public void CreateGoalRequest_ToCommand_KeepsOmittedOptionalFieldsNull()
        {
            var command = new CreateGoalRequest { CurrentAge = 30, RetirementAge = 60, TargetSavings = 10m }.ToCommand(userId: 1);

            Assert.Null(command.Name);
            Assert.Null(command.ExpectedAnnualReturn);
            Assert.Null(command.ReturnVolatility);
            Assert.Null(command.InflationRate);
            Assert.Null(command.AnnualContributionIncrease);
        }

        [Fact]
        public void Goal_ToResponse_MapsDomainNamesToApiNames()
        {
            var goal = new Goal
            {
                Id = 11,
                UserId = 3,
                Name = "Retirement",
                CurrentAge = 30,
                RetirementAge = 60,
                TargetAmount = 1_000_000m,
                CurrentSavings = 102_500m,
                ExpectedAnnualReturn = 0.06m,
                ReturnVolatility = 0.12m,
                InflationRate = 0.025m,
                PlannedMonthlyContribution = 2_500m,
                CreatedAt = new DateTime(2026, 9, 27)
            };

            var response = goal.ToResponse();

            Assert.Equal(11, response.Id);
            Assert.Equal("Retirement", response.Name);
            Assert.Equal(30, response.CurrentAge);
            Assert.Equal(60, response.RetirementAge);
            Assert.Equal(1_000_000m, response.TargetSavings);
            Assert.Equal(2_500m, response.MonthlyContribution);
            Assert.Equal(102_500m, response.CurrentSavings);
        }

        [Theory]
        [InlineData("10.25", "10.25%")]
        [InlineData("11.000000", "11.00%")]
        [InlineData("33.333333", "33.33%")]
        [InlineData("0.005", "0.00%")]   // banker's rounding, as before
        [InlineData("0", "0%")]
        public void Percentage_ToProgressResponse_FormatsLikeBefore(string percentage, string expected)
        {
            var response = decimal.Parse(percentage, CultureInfo.InvariantCulture).ToProgressResponse(goalId: 4);

            Assert.Equal(4, response.GoalId);
            Assert.Equal(expected, response.Progress);
        }

        [Fact]
        public void Percentage_ToProgressResponse_IgnoresServerCulture()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.Equal("10.25%", 10.25m.ToProgressResponse(1).Progress);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}

using RetirementPlanner.Infrastructure;

namespace RetirementPlanner.Tests.Infrastructure
{
    public class FieldKeyExtensionsTests
    {
        [Theory]
        [InlineData("RetirementAge", "retirementAge")]
        [InlineData("UserName", "userName")]
        [InlineData("GoalId", "goalId")]
        [InlineData("Items[0].Amount", "items[0].amount")]
        [InlineData("month", "month")]
        public void ToFieldKey_MatchesCamelCaseJsonNames(string propertyName, string expected)
        {
            Assert.Equal(expected, propertyName.ToFieldKey());
        }
    }
}

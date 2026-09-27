using MySqlConnector;

namespace RetirementPlanner.Tests.Integration
{
    [Collection(MySqlCollection.Name)]
    public class MigrationTests(MySqlFixture db)
    {
        // ER_CHECK_CONSTRAINT_VIOLATED; MySqlConnector's enum has no member for it.
        private const MySqlErrorCode CheckConstraintViolated = (MySqlErrorCode)3819;

        [Theory]
        [InlineData("Users")]
        [InlineData("Profiles")]
        [InlineData("Goals")]
        [InlineData("Contributions")]
        [InlineData("SimulationRuns")]
        [InlineData("RefreshTokens")]
        public async Task Migrations_CreateTable(string table)
        {
            var count = await db.ScalarAsync<long>(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @table",
                new { table });

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Migrations_AreJournaledAndRunningAgainIsANoOp()
        {
            db.CreateMigrator().Migrate();

            var applied = await db.ScalarAsync<long>("SELECT COUNT(*) FROM schemaversions");
            Assert.Equal(5, applied);
        }

        [Fact]
        public async Task Migrations_CreateNoStoredProcedures()
        {
            var routines = await db.ScalarAsync<long>(
                "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = DATABASE()");

            Assert.Equal(0, routines);
        }

        [Fact]
        public async Task Contributions_RejectMonthOutsideOneToTwelve()
        {
            var goalId = await CreateGoalAsync();

            var ex = await Assert.ThrowsAsync<MySqlException>(() => db.ExecuteAsync(
                "INSERT INTO Contributions (GoalId, `Year`, `Month`, Amount) VALUES (@goalId, 2026, 13, 100)",
                new { goalId }));

            Assert.Equal(CheckConstraintViolated, ex.ErrorCode);
        }

        [Fact]
        public async Task Contributions_RejectSecondRowForSameGoalAndMonth()
        {
            var goalId = await CreateGoalAsync();
            const string insert = "INSERT INTO Contributions (GoalId, `Year`, `Month`, Amount) VALUES (@goalId, 2026, 5, 100)";
            await db.ExecuteAsync(insert, new { goalId });

            var ex = await Assert.ThrowsAsync<MySqlException>(() => db.ExecuteAsync(insert, new { goalId }));

            Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, ex.ErrorCode);
        }

        [Fact]
        public async Task Goals_RejectRetirementAgeNotAfterCurrentAge()
        {
            var userId = await CreateUserAsync();

            var ex = await Assert.ThrowsAsync<MySqlException>(() => db.ExecuteAsync(
                """
                INSERT INTO Goals (UserId, Name, CurrentAge, RetirementAge, TargetAmount,
                                   ExpectedAnnualReturn, ReturnVolatility, InflationRate)
                VALUES (@userId, 'x', 60, 60, 1000, 0.06, 0.12, 0.025)
                """, new { userId }));

            Assert.Equal(CheckConstraintViolated, ex.ErrorCode);
        }

        [Fact]
        public async Task Users_RejectDuplicateEmail()
        {
            var email = MySqlFixture.UniqueEmail();
            const string insert = "INSERT INTO Users (Email, PasswordHash) VALUES (@email, 'hash')";
            await db.ExecuteAsync(insert, new { email });

            var ex = await Assert.ThrowsAsync<MySqlException>(() => db.ExecuteAsync(insert, new { email }));

            Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, ex.ErrorCode);
        }

        [Fact]
        public async Task SimulationRuns_RejectSuccessProbabilityAboveOne()
        {
            var goalId = await CreateGoalAsync();

            var ex = await Assert.ThrowsAsync<MySqlException>(() => db.ExecuteAsync(
                "INSERT INTO SimulationRuns (GoalId, ParametersJson, SuccessProbability, PercentileBandsJson) VALUES (@goalId, '{}', 1.5, '[]')",
                new { goalId }));

            Assert.Equal(CheckConstraintViolated, ex.ErrorCode);
        }

        [Fact]
        public async Task DeletingUser_CascadesToGoalsAndContributions()
        {
            var goalId = await CreateGoalAsync();
            await db.ExecuteAsync("INSERT INTO Contributions (GoalId, `Year`, `Month`, Amount) VALUES (@goalId, 2026, 1, 50)", new { goalId });

            await db.ExecuteAsync("DELETE u FROM Users u JOIN Goals g ON g.UserId = u.Id WHERE g.Id = @goalId", new { goalId });

            Assert.Equal(0, await db.ScalarAsync<long>("SELECT COUNT(*) FROM Contributions WHERE GoalId = @goalId", new { goalId }));
        }

        private Task<int> CreateUserAsync() => db.ScalarAsync<int>(
            "INSERT INTO Users (Email, PasswordHash) VALUES (@email, 'hash'); SELECT LAST_INSERT_ID();",
            new { email = MySqlFixture.UniqueEmail() });

        private async Task<int> CreateGoalAsync()
        {
            var userId = await CreateUserAsync();
            return await db.ScalarAsync<int>(
                """
                INSERT INTO Goals (UserId, Name, CurrentAge, RetirementAge, TargetAmount,
                                   ExpectedAnnualReturn, ReturnVolatility, InflationRate)
                VALUES (@userId, 'Retirement', 30, 60, 1000000, 0.06, 0.12, 0.025);
                SELECT LAST_INSERT_ID();
                """, new { userId });
        }
    }
}

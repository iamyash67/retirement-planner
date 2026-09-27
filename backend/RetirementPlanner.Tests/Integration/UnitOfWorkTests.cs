using RetirementPlanner.Repositories;

namespace RetirementPlanner.Tests.Integration
{
    [Collection(MySqlCollection.Name)]
    public class UnitOfWorkTests(MySqlFixture db)
    {
        [Fact]
        public async Task GetConnectionAsync_ReturnsTheSameConnectionEveryTime()
        {
            await using var uow = db.CreateUnitOfWork();

            Assert.Same(await uow.GetConnectionAsync(), await uow.GetConnectionAsync());
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_OnException_RollsBackEveryRepositoryWrite()
        {
            var email = MySqlFixture.UniqueEmail();
            await using (var uow = db.CreateUnitOfWork())
            {
                var users = new UserRepository(uow);
                var goals = new GoalRepository(uow);

                await Assert.ThrowsAsync<InvalidOperationException>(() => uow.ExecuteInTransactionAsync<int>(async () =>
                {
                    var userId = await users.CreateAsync(email, "hashed");
                    await goals.CreateAsync(RepositoryTests.NewGoal(userId));
                    throw new InvalidOperationException("fail after both writes");
                }));

                Assert.Null(uow.Transaction);
            }

            Assert.Equal(0, await db.ScalarAsync<long>("SELECT COUNT(*) FROM Users WHERE Email = @email", new { email }));
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_OnSuccess_Commits()
        {
            var email = MySqlFixture.UniqueEmail();
            await using (var uow = db.CreateUnitOfWork())
            {
                var users = new UserRepository(uow);
                await uow.ExecuteInTransactionAsync(() => users.CreateAsync(email, "hashed"));
            }

            Assert.Equal(1, await db.ScalarAsync<long>("SELECT COUNT(*) FROM Users WHERE Email = @email", new { email }));
        }

        [Fact]
        public async Task DisposeAsync_WithUncommittedTransaction_RollsBack()
        {
            var email = MySqlFixture.UniqueEmail();
            await using (var uow = db.CreateUnitOfWork())
            {
                await uow.BeginAsync();
                await new UserRepository(uow).CreateAsync(email, "hashed");
            }

            Assert.Equal(0, await db.ScalarAsync<long>("SELECT COUNT(*) FROM Users WHERE Email = @email", new { email }));
        }

        [Fact]
        public async Task BeginAsync_WhenTransactionActive_Throws()
        {
            await using var uow = db.CreateUnitOfWork();
            await uow.BeginAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.BeginAsync());
        }
    }
}

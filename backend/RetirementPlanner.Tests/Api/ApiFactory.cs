using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Tests.Integration;

namespace RetirementPlanner.Tests.Api
{
    /// <summary>The real API (Program.cs), pointed at the Testcontainers database.</summary>
    public sealed class ApiFactory(MySqlFixture db) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", db.ConnectionString);
        }

        /// <summary>Creates a user with a profile and returns (userId, email).</summary>
        public async Task<(int UserId, string Email)> CreateUserAsync(string password = "pass123")
        {
            await using var scope = Services.CreateAsyncScope();
            var sp = scope.ServiceProvider;
            var email = MySqlFixture.UniqueEmail();

            var userId = await sp.GetRequiredService<IUnitOfWork>().ExecuteInTransactionAsync(async () =>
            {
                var hash = sp.GetRequiredService<IPasswordHasher<User>>().HashPassword(new User { Email = email }, password);
                var id = await sp.GetRequiredService<IUserRepository>().CreateAsync(email, hash);
                await sp.GetRequiredService<IProfileRepository>().CreateAsync(new UserProfile
                {
                    UserId = id, FirstName = "Api", LastName = "Tester", DateOfBirth = new DateOnly(1990, 1, 1)
                });
                return id;
            });
            return (userId, email);
        }
    }
}

using FluentValidation;
using Microsoft.AspNetCore.Identity;
using RetirementPlanner.Data;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services;
using RetirementPlanner.Services.Interfaces;
using RetirementPlanner.Validators;

namespace RetirementPlanner.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers data access, repositories and services. Program.cs and the integration tests
        /// share this method, so the tests resolve exactly what the API resolves.
        /// </summary>
        public static IServiceCollection AddRetirementPlanner(this IServiceCollection services, string connectionString)
        {
            // Data access: one connection and transaction per request, shared by all repositories
            services.AddSingleton<IDbConnectionFactory>(new MySqlConnectionFactory(connectionString));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<IDatabaseMigrator>(sp =>
                new DatabaseMigrator(connectionString, sp.GetRequiredService<ILogger<DatabaseMigrator>>()));
            services.AddScoped<IDataSeeder, DevelopmentDataSeeder>();

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProfileRepository, ProfileRepository>();
            services.AddScoped<IGoalRepository, GoalRepository>();
            services.AddScoped<IContributionRepository, ContributionRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Services
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IGoalService, GoalService>();
            services.AddScoped<IContributionService, ContributionService>();
            services.AddScoped<IAuthService, AuthService>();

            // Request validators (one per request DTO), run by FluentValidationFilter
            services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>(ServiceLifetime.Singleton);

            return services;
        }
    }
}

namespace RetirementPlanner.Data.Interfaces
{
    public interface IDataSeeder
    {
        /// <summary>Inserts the seed data if it is missing. Safe to run on every startup.</summary>
        Task SeedAsync(CancellationToken cancellationToken = default);
    }
}

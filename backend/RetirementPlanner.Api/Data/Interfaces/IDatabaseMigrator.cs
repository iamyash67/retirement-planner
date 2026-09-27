namespace RetirementPlanner.Data.Interfaces
{
    public interface IDatabaseMigrator
    {
        /// <summary>Applies every embedded migration that has not run yet. Throws if one fails.</summary>
        void Migrate();
    }
}

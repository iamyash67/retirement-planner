namespace RetirementPlanner.Tests.TestSupport
{
    /// <summary>A clock the test moves forward explicitly.</summary>
    public sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}

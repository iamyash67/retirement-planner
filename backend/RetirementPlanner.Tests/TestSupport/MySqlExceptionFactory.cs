using System.Reflection;
using MySqlConnector;

namespace RetirementPlanner.Tests.TestSupport
{
    public static class MySqlExceptionFactory
    {
        /// <summary>
        /// MySqlException is sealed and all its constructors are internal, so tests create one through reflection.
        /// </summary>
        public static MySqlException Create(MySqlErrorCode errorCode, string message = "simulated")
        {
            var constructor = typeof(MySqlException).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, [typeof(MySqlErrorCode), typeof(string)])
                ?? throw new InvalidOperationException("MySqlException(MySqlErrorCode, string) constructor not found.");
            return (MySqlException)constructor.Invoke([errorCode, message]);
        }
    }
}

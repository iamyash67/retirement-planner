using Moq;
using RetirementPlanner.Data.Interfaces;

namespace RetirementPlanner.Tests.TestSupport
{
    public static class UnitOfWorkMock
    {
        /// <summary>A unit of work that runs the transactional delegate directly and counts the calls.</summary>
        public static Mock<IUnitOfWork> Create<T>()
        {
            var mock = new Mock<IUnitOfWork>();
            mock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<T>>>(), It.IsAny<CancellationToken>()))
                .Returns((Func<Task<T>> work, CancellationToken _) => work());
            return mock;
        }
    }
}

using ReservationSystem.Models;
using ReservationSystem.Repository;

namespace ReservationSystem.Tests.Fakes
{
    /// <summary>
    /// Minimal <see cref="IUnitOfWork"/> stub. The <see cref="ReservationSystem.Reservation.ReservationManager"/>
    /// methods under test only pass the unit of work through to the repository; they never touch
    /// <see cref="DbContext"/> directly, so returning <c>null</c> here is safe.
    /// </summary>
    public class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public DbContextWrap DbContext
        {
            get { return null; }
        }

        public void SaveChanges()
        {
            SaveChangesCount++;
        }

        public void Dispose()
        {
        }
    }
}

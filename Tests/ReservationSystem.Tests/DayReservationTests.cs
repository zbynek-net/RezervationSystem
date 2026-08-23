using ReservationSystem.Models;
using ReservationSystem.Reservation;
using Xunit;

namespace ReservationSystem.Tests
{
    public class DayReservationTests
    {
        // Regression guard for the fixed constructor bug: the isCancelled argument used to be
        // ignored (always stored true). These cases fail against the old code.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Constructor_RespectsIsCancelledArgument(bool cancelled)
        {
            Assert.Equal(cancelled, new DayReservation(cancelled).IsCancelled);
        }

        [Fact]
        public void DefaultConstructor_IsNotCancelled()
        {
            Assert.False(new DayReservation().IsCancelled);
        }

        [Fact]
        public void IsReserved_Returns3ForReservation_2ForPicked_1ForFree()
        {
            var table = new TableModel(1); // Number = 1
            var tableReservation = new TableReservation(table);
            tableReservation.AddReservation(new ReservationModel { TableId = 1, TimeId = 3, UserId = "u1" });
            tableReservation.AddPicked(new PickedModel { TableId = 1, TimeId = 5, UserId = "u1" });

            var day = new DayReservation();
            day.Add(tableReservation);

            Assert.Equal(3, day.IsReserved(new TimeModel { Id = 3 }, table)); // reserved
            Assert.Equal(2, day.IsReserved(new TimeModel { Id = 5 }, table)); // picked
            Assert.Equal(1, day.IsReserved(new TimeModel { Id = 9 }, table)); // free
        }

        [Fact]
        public void UserFromReservation_ReturnsReservationOwner()
        {
            var table = new TableModel(2);
            var tableReservation = new TableReservation(table);
            tableReservation.AddReservation(new ReservationModel { TableId = 2, TimeId = 6, UserId = "owner" });

            var day = new DayReservation();
            day.Add(tableReservation);

            Assert.Equal("owner", day.UserFromReservation(new TimeModel { Id = 6 }, table));
        }
    }
}

using ReservationSystem.Models;
using ReservationSystem.Reservation;
using Xunit;

namespace ReservationSystem.Tests
{
    public class TableReservationTests
    {
        [Fact]
        public void TableNumber_ComesFromTable()
        {
            var tr = new TableReservation(new TableModel(7));
            Assert.Equal(7, tr.TableNumber);
        }

        [Fact]
        public void IsReservation_TrueOnlyForAddedTimeId()
        {
            var tr = new TableReservation(new TableModel(1));
            tr.AddReservation(new ReservationModel { TimeId = 3, UserId = "u1" });

            Assert.True(tr.IsReservation(3));
            Assert.False(tr.IsReservation(4));
        }

        [Fact]
        public void IsPicked_TrueOnlyForAddedTimeId()
        {
            var tr = new TableReservation(new TableModel(1));
            tr.AddPicked(new PickedModel { TimeId = 2, UserId = "u1" });

            Assert.True(tr.IsPicked(2));
            Assert.False(tr.IsPicked(5));
        }

        [Fact]
        public void GetUser_ReturnsUserId_WhenNameIsNull()
        {
            var tr = new TableReservation(new TableModel(1));
            tr.AddReservation(new ReservationModel { TimeId = 3, UserId = "user-42", Name = null });

            Assert.Equal("user-42", tr.GetUser(3));
        }

        [Fact]
        public void GetUser_ReturnsName_WhenNamePresent()
        {
            var tr = new TableReservation(new TableModel(1));
            tr.AddReservation(new ReservationModel { TimeId = 4, UserId = "user-42", Name = "Group booking" });

            Assert.Equal("Group booking", tr.GetUser(4));
        }

        [Fact]
        public void GetUser_ReturnsNull_WhenNoReservationAtThatTime()
        {
            var tr = new TableReservation(new TableModel(1));
            Assert.Null(tr.GetUser(99));
        }
    }
}

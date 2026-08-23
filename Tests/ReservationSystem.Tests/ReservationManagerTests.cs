using System;
using System.Collections.Generic;
using ReservationSystem.Models;
using ReservationSystem.Reservation;
using ReservationSystem.Tests.Fakes;
using Xunit;

namespace ReservationSystem.Tests
{
    /// <summary>
    /// Exercises the core domain logic in <see cref="ReservationManager"/> using an in-memory
    /// repository - no database required.
    /// </summary>
    public class ReservationManagerTests
    {
        private static ReservationManager NewManager(FakeRepository repo)
        {
            return new ReservationManager(repo);
        }

        [Fact]
        public void GetReservationsForUser_ReturnsOnlyThatUser_OrderedByDate()
        {
            var repo = new FakeRepository();
            repo.Seed(
                new ReservationModel { Id = 1, UserId = "u1", Date = new DateTime(2025, 3, 10), TableId = 1, TimeId = 1 },
                new ReservationModel { Id = 2, UserId = "u1", Date = new DateTime(2025, 3, 5), TableId = 1, TimeId = 2 },
                new ReservationModel { Id = 3, UserId = "u2", Date = new DateTime(2025, 3, 1), TableId = 1, TimeId = 3 });
            var manager = NewManager(repo);

            var result = manager.GetReservationsForUser(new FakeUnitOfWork(), "u1");

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal("u1", r.UserId));
            Assert.Equal(new DateTime(2025, 3, 5), result[0].Date); // ascending by date
            Assert.Equal(new DateTime(2025, 3, 10), result[1].Date);
        }

        [Fact]
        public void GetUsersForDate_ReturnsUsersReservedOnThatDateOnly()
        {
            var day = new DateTime(2025, 6, 1);
            var repo = new FakeRepository();
            repo.Seed(
                new ReservationModel { Id = 1, UserId = "a", Date = day, TableId = 1, TimeId = 1 },
                new ReservationModel { Id = 2, UserId = "b", Date = day, TableId = 2, TimeId = 1 },
                new ReservationModel { Id = 3, UserId = "c", Date = day.AddDays(1), TableId = 1, TimeId = 1 });
            var manager = NewManager(repo);

            var users = manager.GetUsersForDate(new FakeUnitOfWork(), day);

            Assert.Equal(2, users.Count);
            Assert.Contains("a", users);
            Assert.Contains("b", users);
            Assert.DoesNotContain("c", users);
        }

        [Fact]
        public void GetReservationsForDate_BuildsGrid_WithReservationsAndPicks()
        {
            var day = new DateTime(2025, 7, 20);
            var table1 = new TableModel(1) { Id = 1 };
            var table2 = new TableModel(2) { Id = 2 };

            var repo = new FakeRepository();
            repo.Seed(new ReservationModel { Id = 1, UserId = "u1", Date = day, TableId = 1, TimeId = 5 });
            repo.Seed(new PickedModel { Id = 1, UserId = "u1", PickedDate = day, TableId = 2, TimeId = 6, TimeStamp = DateTime.Now });
            var manager = NewManager(repo);

            var result = manager.GetReservationsForDate(
                new FakeUnitOfWork(), day, new List<TableModel> { table1, table2 }, "u1");

            Assert.False(result.IsCancelled);
            Assert.Equal(3, result.IsReserved(new TimeModel { Id = 5 }, table1)); // reserved
            Assert.Equal(2, result.IsReserved(new TimeModel { Id = 6 }, table2)); // picked by this user
            Assert.Equal(1, result.IsReserved(new TimeModel { Id = 5 }, table2)); // free
        }

        [Fact]
        public void GetReservationsForDate_ReturnsCancelled_WhenDayIsCancelled()
        {
            var day = new DateTime(2025, 12, 24);
            var repo = new FakeRepository();
            repo.Seed(new CancelledDayModel { Id = 1, Date = day, Reason = "Holiday" });
            var manager = NewManager(repo);

            var result = manager.GetReservationsForDate(
                new FakeUnitOfWork(), day, new List<TableModel> { new TableModel(1) { Id = 1 } }, "u1");

            Assert.True(result.IsCancelled);
        }

        [Fact]
        public void GetPickedForDateAndUser_ReturnsOnlyThatUsersPicksForThatDate()
        {
            var day = new DateTime(2025, 8, 1);
            var repo = new FakeRepository();
            repo.Seed(
                new PickedModel { Id = 1, UserId = "u1", PickedDate = day, TableId = 1, TimeId = 1, TimeStamp = DateTime.Now },
                new PickedModel { Id = 2, UserId = "u1", PickedDate = day.AddDays(1), TableId = 1, TimeId = 2, TimeStamp = DateTime.Now },
                new PickedModel { Id = 3, UserId = "u2", PickedDate = day, TableId = 1, TimeId = 3, TimeStamp = DateTime.Now });
            var manager = NewManager(repo);

            var picks = manager.GetPickedForDateAndUser(new FakeUnitOfWork(), day, "u1");

            Assert.Single(picks);
            Assert.Equal(1, picks[0].Id);
        }

        [Fact]
        public void IsAfterDeadline_FutureDate_IsAlwaysFalse()
        {
            var repo = new FakeRepository();
            repo.Seed(new SettingModel { Id = 1, Name = "Deadline", Value = "16:00:00" });
            var manager = NewManager(repo);

            // The deadline only applies to "today", so any future date is never past the deadline.
            Assert.False(manager.IsAfterDeadline(new FakeUnitOfWork(), DateTime.Now.Date.AddDays(1)));
        }
    }
}

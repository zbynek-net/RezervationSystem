using System;
using ReservationSystem.Utils;
using Xunit;

namespace ReservationSystem.Tests
{
    public class DateUtilTests
    {
        [Fact]
        public void DateDiff_PastDate_ReturnsZero()
        {
            Assert.Equal(0, DateUtil.DateDiff(DateTime.Now.AddDays(-5)));
        }

        [Fact]
        public void DateDiff_FutureDate_ReturnsWholeDaysRemaining()
        {
            var diff = DateUtil.DateDiff(DateTime.Now.AddDays(10));

            // Truncation of ~9.999 days can land on either 9 or 10 depending on sub-second timing.
            Assert.InRange(diff, 9, 10);
        }

        [Fact]
        public void DateDiff_Int_AddsDaysToToday()
        {
            var expected = DateTime.Now.AddDays(3).Date;
            Assert.Equal(expected, DateUtil.DateDiff(3).Date);
        }
    }
}

using System;
using ReservationSystem.Models;
using ReservationSystem.Utils;
using Xunit;

namespace ReservationSystem.Tests
{
    /// <summary>
    /// Covers <see cref="Extensions.IsIn"/>, which decides whether a calendar day falls inside a
    /// season/date range. The tricky part is the year wrap-around (e.g. a winter season that starts
    /// in November and ends in February).
    /// </summary>
    public class ExtensionsIsInTests
    {
        private static DateRangeModel Range(int startMonth, int startDay, int endMonth, int endDay)
        {
            // Only month/day matter to IsIn, except that EndTime earlier than StartDate (as a full
            // date) is what signals a wrap across the year boundary.
            return new DateRangeModel
            {
                StartDate = new DateTime(2020, startMonth, startDay),
                EndTime = new DateTime(2020, endMonth, endDay)
            };
        }

        [Theory]
        [InlineData(3, 1, 8, 31, 5, 15, true)]   // inside a normal (non-wrapping) range
        [InlineData(3, 1, 8, 31, 1, 10, false)]  // before the range
        [InlineData(3, 1, 8, 31, 9, 1, false)]   // after the range
        [InlineData(3, 1, 8, 31, 3, 1, true)]    // start boundary is inclusive
        [InlineData(3, 1, 8, 31, 8, 31, true)]   // end boundary is inclusive
        public void NormalRange(int sm, int sd, int em, int ed, int dm, int dd, bool expected)
        {
            var range = Range(sm, sd, em, ed);
            Assert.Equal(expected, range.IsIn(new DateTime(2021, dm, dd)));
        }

        [Theory]
        [InlineData(11, 1, true)]   // start boundary of a Nov 1 -> Feb 28 winter range
        [InlineData(12, 15, true)]  // December is inside
        [InlineData(1, 15, true)]   // January (next year) is inside
        [InlineData(2, 28, true)]   // end boundary is inclusive
        [InlineData(6, 15, false)]  // summer is outside
        [InlineData(3, 1, false)]   // just past the wrap end
        public void WrappingRange(int dm, int dd, bool expected)
        {
            // EndTime (Feb) is earlier than StartDate (Nov) => the range wraps the year end.
            var range = Range(11, 1, 2, 28);
            Assert.Equal(expected, range.IsIn(new DateTime(2021, dm, dd)));
        }
    }
}

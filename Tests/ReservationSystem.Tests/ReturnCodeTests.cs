using ReservationSystem.Utils;
using Xunit;

namespace ReservationSystem.Tests
{
    public class ReturnCodeTests
    {
        [Fact]
        public void ToString_SerializesAsLevelMessageReason()
        {
            var rc = new ReturnCode(ReturnCodeLevel.WARNING, "msg", "reason");
            Assert.Equal("1;msg;reason", rc.ToString());
        }

        [Fact]
        public void FromString_RoundTripsAllFields()
        {
            var original = new ReturnCode(ReturnCodeLevel.SUCCESS, "hello", "because");

            var parsed = ReturnCode.FromString(original.ToString());

            Assert.NotNull(parsed);
            Assert.Equal(ReturnCodeLevel.SUCCESS, parsed.ReturnLevel);
            Assert.Equal("hello", parsed.Message);
            Assert.Equal("because", parsed.Reason);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("only;two")]
        [InlineData("a;b;c;d")]
        public void FromString_InvalidInput_ReturnsNull(string input)
        {
            Assert.Null(ReturnCode.FromString(input));
        }

        [Fact]
        public void DefaultConstructor_UsesReloadLevel()
        {
            var rc = new ReturnCode();
            Assert.Equal(ReturnCodeLevel.RELOAD, rc.ReturnLevel);
        }

        [Fact]
        public void Error_SetsErrorLevelAndReason()
        {
            var rc = new ReturnCode();

            rc.Error("boom");

            Assert.Equal(ReturnCodeLevel.ERROR, rc.ReturnLevel);
            Assert.Equal("boom", rc.Reason);
        }
    }
}

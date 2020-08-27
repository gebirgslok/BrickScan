using Xunit;

namespace BrickScan.WpfClient.Tests
{
    public class LanguageOptionTests
    {
        [Fact]
        public void ToStringReturnsDisplayName()
        {
            var option = new LanguageOption("en-US", "English");

            Assert.Equal(option.DisplayName, option.ToString());
        }
    }
}

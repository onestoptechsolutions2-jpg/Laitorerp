using Leitor.Erp.Pages.Shared;
using Xunit;

namespace Leitor.Erp.Tests;

public class PhoneLinksToE164Tests
{
    [Theory]
    [InlineData("0712345678", "+254712345678")]
    [InlineData("254712345678", "+254712345678")]
    [InlineData("+254712345678", "+254712345678")]
    [InlineData("+1 800 555 0199", "+18005550199")]
    public void ToE164_Normalizes_Known_Formats(string input, string expected)
    {
        Assert.Equal(expected, PhoneLinks.ToE164(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a number")]
    public void ToE164_Returns_Null_For_Unusable_Input(string? input)
    {
        Assert.Null(PhoneLinks.ToE164(input));
    }
}

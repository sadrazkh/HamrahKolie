using HamrahKolie.Web.Helpers;

namespace HamrahKolie.Tests.SupportRequests;

public class MaskingTests
{
    [Theory]
    [InlineData("1234567890", "******7890")]
    [InlineData("", "—")]
    [InlineData(null, "—")]
    [InlineData("12", "—")]
    public void MaskNationalId_reveals_only_last_four(string? input, string expected)
        => Assert.Equal(expected, SupportRequestDisplay.MaskNationalId(input));

    [Theory]
    [InlineData("09121112233", "0912***33")]
    [InlineData("", "—")]
    [InlineData(null, "—")]
    public void MaskMobile_masks_middle_digits(string? input, string expected)
        => Assert.Equal(expected, SupportRequestDisplay.MaskMobile(input));
}

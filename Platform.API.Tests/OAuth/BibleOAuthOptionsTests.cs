using FluentAssertions;
using Platform.API.OAuth;
using Xunit;

namespace Platform.API.Tests.OAuth;

public sealed class BibleOAuthOptionsTests
{
    [Fact]
    public void DefaultValues_AreExpected()
    {
        var options = new BibleOAuthOptions();

        options.ClientId.Should().BeEmpty();
        options.Scopes.Should().Be("openid profile email");
    }
}

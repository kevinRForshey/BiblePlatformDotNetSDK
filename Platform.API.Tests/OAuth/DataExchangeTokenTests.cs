using FluentAssertions;
using Platform.API.OAuth;
using Xunit;

namespace Platform.API.Tests.OAuth;

public sealed class DataExchangeTokenTests
{
    [Fact]
    public void DefaultValues_AreEmptyStrings()
    {
        var token = new DataExchangeToken();

        token.Token.Should().BeEmpty();
        token.TokenType.Should().BeEmpty();
    }
}

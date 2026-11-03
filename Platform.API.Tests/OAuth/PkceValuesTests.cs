using FluentAssertions;
using Platform.API.OAuth;
using Xunit;

namespace Platform.API.Tests.OAuth;

public sealed class PkceValuesTests
{
    [Fact]
    public void DefaultValues_AreEmptyCodeVerifierAndChallenge_WithS256Method()
    {
        var pkce = new PkceValues();

        pkce.CodeVerifier.Should().BeEmpty();
        pkce.CodeChallenge.Should().BeEmpty();
        pkce.CodeChallengeMethod.Should().Be("S256");
    }
}

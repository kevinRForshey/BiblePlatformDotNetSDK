using FluentAssertions;
using Platform.API.Configuration;
using Xunit;

namespace Platform.API.Tests.Configuration;

public sealed class BibleApiOptionsTests
{
    [Fact]
    public void AppKey_DefaultsToEmpty()
    {
        var options = new BibleApiOptions();

        options.AppKey.Should().BeEmpty();
    }
}

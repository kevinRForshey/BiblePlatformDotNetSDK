using FluentAssertions;
using Platform.API.Clients;
using Xunit;

namespace Platform.API.Tests.Clients;

public sealed class HighlightsResponseTests
{
    [Fact]
    public void RecentColorEntry_DefaultColor_IsEmpty()
    {
        var entry = new RecentColorEntry();

        entry.Color.Should().BeEmpty();
    }
}

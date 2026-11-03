using FluentAssertions;
using Platform.API.Clients;
using Xunit;

namespace Platform.API.Tests.Clients;

public sealed class CreateOrUpdateHighlightRequestTests
{
    [Fact]
    public void HighlightPayload_DefaultValues_AreEmptyStrings()
    {
        var payload = new HighlightPayload();

        payload.PassageId.Should().BeEmpty();
        payload.Color.Should().BeEmpty();
    }
}

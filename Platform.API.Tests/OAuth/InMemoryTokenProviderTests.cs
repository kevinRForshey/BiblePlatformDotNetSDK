using System;
using System.Threading.Tasks;
using FluentAssertions;
using Platform.API.OAuth;
using Xunit;

namespace Platform.API.Tests.OAuth;

public sealed class InMemoryTokenProviderTests
{
    private static OAuthTokenResponse Token(string accessToken) =>
        new() { AccessToken = accessToken, ExpiresIn = 3600, ReceivedAt = DateTimeOffset.UtcNow };

    [Fact]
    public async Task GetTokenAsync_ReturnsNull_WhenNoTokenStored()
    {
        var provider = new InMemoryTokenProvider();

        var result = await provider.GetTokenAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsStoredToken()
    {
        var provider = new InMemoryTokenProvider();
        await provider.StoreTokenAsync(Token("access-1"));

        var result = await provider.GetTokenAsync();

        result!.AccessToken.Should().Be("access-1");
    }

    [Fact]
    public async Task GetTokenAsync_CanBeCalledMultipleTimes()
    {
        var provider = new InMemoryTokenProvider();
        await provider.GetTokenAsync();

        var task = provider.GetTokenAsync();
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));

        completed.Should().Be(task);
    }

    [Fact]
    public async Task StoreTokenAsync_CanBeCalledMultipleTimes()
    {
        var provider = new InMemoryTokenProvider();
        await provider.StoreTokenAsync(Token("access-1"));

        var task = provider.StoreTokenAsync(Token("access-2"));
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));

        completed.Should().Be(task);
    }

    [Fact]
    public async Task ClearTokenAsync_ClearsStoredToken()
    {
        var provider = new InMemoryTokenProvider();
        await provider.StoreTokenAsync(Token("access-1"));

        await provider.ClearTokenAsync();

        var result = await provider.GetTokenAsync();
        result.Should().BeNull();
    }

    [Fact]
    public async Task ClearTokenAsync_CanBeCalledMultipleTimes()
    {
        var provider = new InMemoryTokenProvider();
        await provider.ClearTokenAsync();

        var task = provider.ClearTokenAsync();
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));

        completed.Should().Be(task);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var provider = new InMemoryTokenProvider();

        var act = () => provider.Dispose();

        act.Should().NotThrow();
    }
}

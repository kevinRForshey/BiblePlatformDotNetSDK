using Microsoft.Extensions.Caching.Hybrid;

namespace Platform.API.Tests.Fakes;

/// <summary>
/// A test double for <see cref="HybridCache"/> that always invokes the factory (no actual
/// caching) and records the <see cref="HybridCacheEntryOptions"/> passed for each key, so
/// callers can assert which TTL a given cache entry was configured with.
/// </summary>
internal sealed class FakeHybridCache : HybridCache
{
    private readonly Dictionary<string, HybridCacheEntryOptions?> _optionsByKey = new();

    public TimeSpan? ExpirationFor(string key)
        => _optionsByKey.TryGetValue(key, out var options) ? options?.Expiration : null;

    public override async ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        _optionsByKey[key] = options;
        return await factory(state, cancellationToken).ConfigureAwait(false);
    }

    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => default;

    public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        => default;

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        => default;
}

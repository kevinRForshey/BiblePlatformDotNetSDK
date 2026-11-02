using Microsoft.AspNetCore.Http;

namespace PlatformTestApp.Tests.Fakes;

/// <summary>
/// Unlike <see cref="FakeSession"/> (a no-op stub used where only <see cref="ISession.Id"/>
/// matters), this actually stores values, for tests that round-trip state through
/// <c>ctx.Session.SetString</c>/<c>GetString</c>.
/// </summary>
internal sealed class InMemorySession : ISession
{
    private readonly Dictionary<string, byte[]> _store = [];

    public InMemorySession(string id) => Id = id;

    public string Id { get; set; }

    public bool IsAvailable => true;

    public IEnumerable<string> Keys => _store.Keys;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);

    public void Set(string key, byte[] value) => _store[key] = value;

    public void Remove(string key) => _store.Remove(key);

    public void Clear() => _store.Clear();
}

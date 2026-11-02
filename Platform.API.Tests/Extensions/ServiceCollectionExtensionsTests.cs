using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Platform.API.Clients;
using Platform.API.Configuration;
using Platform.API.Extensions;
using Platform.API.Http;
using Platform.API.OAuth;
using Platform.API.Tests.Fakes;
using BiblePlatform.UsfmReferences;
using Xunit;

namespace Platform.API.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // AddBibleApiClients — inline options
    // -------------------------------------------------------------------------

    [Fact]
    public void AddBibleApiClients_RegistersIBibleClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<IBibleClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_RegistersIPassageClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<IPassageClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_AttachesAppKeyAndRateLimitingHandlers_ToTypedClientPipeline()
    {
        var sp = BuildProvider(withOAuth: false);
        var factory = sp.GetRequiredService<IHttpMessageHandlerFactory>();

        var chain = new List<HttpMessageHandler>();
        var handler = factory.CreateHandler(nameof(BibleClient));
        while (handler is not null)
        {
            chain.Add(handler);
            handler = (handler as DelegatingHandler)?.InnerHandler;
        }

        chain.Should().ContainSingle(h => h is AppKeyDelegatingHandler);
        chain.Should().ContainSingle(h => h is OutboundRateLimitingHandler);
    }

    [Fact]
    public void AddBibleApiClients_RegistersIHighlightClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<IHighlightClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_RegistersIAppClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<IAppClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_RegistersILanguageClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<ILanguageClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_RegistersIFontClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<IFontClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_RegistersILicenseClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<ILicenseClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_RegistersIOrganizationClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<IOrganizationClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_RegistersIImageClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<IImageClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_RegistersIVerseOfTheDayClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<IVerseOfTheDayClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_RegistersIPermissionClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<IPermissionClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_RegistersISearchClient()
    {
        var sp = BuildProvider(withOAuth: false);
        sp.GetRequiredService<ISearchClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleApiClients_RegistersIUsfmReferenceService_AsSingleton()
    {
        var sp = BuildProvider(withOAuth: false);

        var service1 = sp.GetRequiredService<IUsfmReferenceService>();
        var service2 = sp.GetRequiredService<IUsfmReferenceService>();

        service1.Should().NotBeNull();
        service1.Should().BeOfType<UsfmReferenceService>();
        service1.Should().BeSameAs(service2);
    }

    [Fact]
    public void AddBibleApiClients_ResolvesDistinctInstances_PerScope()
    {
        var sp = BuildProvider(withOAuth: false);

        using var scope1 = sp.CreateScope();
        using var scope2 = sp.CreateScope();

        var client1 = scope1.ServiceProvider.GetRequiredService<IBibleClient>();
        var client2 = scope2.ServiceProvider.GetRequiredService<IBibleClient>();

        // Typed HttpClient registrations create a new instance per resolution
        client1.Should().NotBeSameAs(client2);
    }

    // -------------------------------------------------------------------------
    // AddBibleOAuth
    // -------------------------------------------------------------------------

    [Fact]
    public void AddBibleOAuth_RegistersIBibleOAuthClient()
    {
        var sp = BuildProvider(withOAuth: true);
        sp.GetRequiredService<IBibleOAuthClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleOAuth_RegistersITokenProvider_AsInMemoryTokenProvider()
    {
        var sp = BuildProvider(withOAuth: true);
        sp.GetRequiredService<ITokenProvider>().Should().BeOfType<InMemoryTokenProvider>();
    }

    [Fact]
    public void AddBibleOAuth_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var returned = services.AddBibleApiClients(o => o.AppKey = "key")
                               .AddBibleOAuth(o => o.ClientId = "cid");

        returned.Should().BeSameAs(services);
    }

    [Fact]
    public void AddBibleOAuth_ThrowsInvalidOperationException_WhenApiClientsNotRegisteredFirst()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddBibleOAuth(o => o.ClientId = "cid");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AddBibleApiClients*");
    }

    [Fact]
    public void AddBibleApiClients_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var act = () => services.AddBibleApiClients(o => o.AppKey = "key");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddBibleApiClients_ThrowsArgumentNullException_WhenConfigureIsNull()
    {
        var services = new ServiceCollection();

        var act = () => services.AddBibleApiClients((Action<Configuration.BibleApiOptions>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // -------------------------------------------------------------------------
    // AddBibleApiClients — IConfiguration overload
    // -------------------------------------------------------------------------

    [Fact]
    public void AddBibleApiClients_FromConfiguration_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var act = () => services.AddBibleApiClients(BuildConfiguration("key"));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddBibleApiClients_FromConfiguration_ThrowsArgumentNullException_WhenConfigurationIsNull()
    {
        var services = new ServiceCollection();

        var act = () => services.AddBibleApiClients((IConfiguration)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddBibleApiClients_FromConfiguration_BindsAppKeyFromConfigSection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(BuildConfiguration("config-bound-key"));

        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IOptions<BibleApiOptions>>().Value.AppKey.Should().Be("config-bound-key");
    }

    [Fact]
    public void AddBibleApiClients_FromConfiguration_RegistersAllApiClients()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(BuildConfiguration("config-key"));

        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IBibleClient>().Should().NotBeNull();
        sp.GetRequiredService<IPassageClient>().Should().NotBeNull();
        sp.GetRequiredService<IHighlightClient>().Should().NotBeNull();
        sp.GetRequiredService<IAppClient>().Should().NotBeNull();
        sp.GetRequiredService<ILanguageClient>().Should().NotBeNull();
        sp.GetRequiredService<IFontClient>().Should().NotBeNull();
        sp.GetRequiredService<ILicenseClient>().Should().NotBeNull();
        sp.GetRequiredService<IOrganizationClient>().Should().NotBeNull();
        sp.GetRequiredService<IImageClient>().Should().NotBeNull();
        sp.GetRequiredService<IVerseOfTheDayClient>().Should().NotBeNull();
        sp.GetRequiredService<IPermissionClient>().Should().NotBeNull();
        sp.GetRequiredService<ISearchClient>().Should().NotBeNull();
    }

    private static IConfiguration BuildConfiguration(string appKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{BibleApiOptions.SectionName}:AppKey"] = appKey
            })
            .Build();

    // -------------------------------------------------------------------------
    // AddBibleOAuth — null checks and configured-options propagation
    // -------------------------------------------------------------------------

    [Fact]
    public void AddBibleOAuth_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;

        var act = () => services.AddBibleOAuth(o => o.ClientId = "cid");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddBibleOAuth_ThrowsArgumentNullException_WhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "key");

        var act = () => services.AddBibleOAuth(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddBibleOAuth_AppliesConfiguredOptions_ToResolvedOAuthClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "key");
        services.AddBibleOAuth(o =>
        {
            o.ClientId = "configured-client-id";
            o.RedirectUri = new Uri("https://localhost/callback");
        });

        var sp = services.BuildServiceProvider();
        var oauthClient = sp.GetRequiredService<IBibleOAuthClient>();

        var request = oauthClient.BuildAuthorizationUrl();

        request.AuthorizationUrl.ToString().Should().Contain("client_id=configured-client-id");
    }

    [Fact]
    public void AddBibleOAuth_DisablesAutoRedirect_OnOAuthHttpClientsPrimaryHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "key");
        services.AddBibleOAuth(o => o.ClientId = "cid");

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpMessageHandlerFactory>();

        var handler = factory.CreateHandler(nameof(IBibleOAuthClient));
        while (handler is DelegatingHandler delegating && delegating.InnerHandler is not null)
            handler = delegating.InnerHandler;

        handler.Should().BeOfType<HttpClientHandler>();
        ((HttpClientHandler)handler).AllowAutoRedirect.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // End-to-end: bearer token actually flows through IHighlightClient's/IPermissionClient's pipelines
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddBibleOAuth_AttachesBearerToken_ToLiveHighlightClientRequest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "test-key");
        services.AddBibleOAuth(o =>
        {
            o.ClientId = "test-client";
            o.RedirectUri = new Uri("https://localhost/callback");
        });

        // Intercept the same named pipeline AddBibleOAuth appended the bearer handler to,
        // via the shared constant rather than re-deriving the client name independently.
        var capturingHandler = new CapturingHandler(
            responseBody: """{"data":[]}""");
        services.AddHttpClient(ServiceCollectionExtensions.HighlightClientName)
            .ConfigurePrimaryHttpMessageHandler(() => capturingHandler);

        var sp = services.BuildServiceProvider();

        var tokenProvider = sp.GetRequiredService<ITokenProvider>();
        await tokenProvider.StoreTokenAsync(new OAuthTokenResponse
        {
            AccessToken = "live-access-token",
            ExpiresIn = 3600,
            ReceivedAt = DateTimeOffset.UtcNow
        });

        var highlightClient = sp.GetRequiredService<IHighlightClient>();
        await highlightClient.GetHighlightsAsync(3034, Reference.FromString("JHN.3.16"));

        capturingHandler.LastRequest!.Headers.Authorization.Should().NotBeNull();
        capturingHandler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturingHandler.LastRequest.Headers.Authorization!.Parameter.Should().Be("live-access-token");
    }

    [Fact]
    public async Task AddBibleOAuth_AttachesBearerToken_ToLivePermissionClientRequest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "test-key");
        services.AddBibleOAuth(o =>
        {
            o.ClientId = "test-client";
            o.RedirectUri = new Uri("https://localhost/callback");
        });

        // Intercept the same named pipeline AddBibleOAuth appended the bearer handler to,
        // via the shared constant rather than re-deriving the client name independently.
        var capturingHandler = new CapturingHandler(
            responseBody: """{"permissions":[]}""");
        services.AddHttpClient(ServiceCollectionExtensions.PermissionClientName)
            .ConfigurePrimaryHttpMessageHandler(() => capturingHandler);

        var sp = services.BuildServiceProvider();

        var tokenProvider = sp.GetRequiredService<ITokenProvider>();
        await tokenProvider.StoreTokenAsync(new OAuthTokenResponse
        {
            AccessToken = "live-access-token",
            ExpiresIn = 3600,
            ReceivedAt = DateTimeOffset.UtcNow
        });

        var permissionClient = sp.GetRequiredService<IPermissionClient>();
        await permissionClient.GetPermissionsAsync("test-app-id");

        capturingHandler.LastRequest!.Headers.Authorization.Should().NotBeNull();
        capturingHandler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturingHandler.LastRequest.Headers.Authorization!.Parameter.Should().Be("live-access-token");
    }

    // -------------------------------------------------------------------------
    // Custom ITokenProvider replacement
    // -------------------------------------------------------------------------

    [Fact]
    public void AddBibleOAuth_AllowsCustomTokenProvider_Override()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "key");

        // Register custom provider BEFORE AddBibleOAuth
        services.AddSingleton<ITokenProvider, CustomTokenProvider>();
        services.AddBibleOAuth(o => o.ClientId = "cid");

        // The first registered singleton wins
        var sp = services.BuildServiceProvider();
        sp.GetRequiredService<ITokenProvider>().Should().BeOfType<CustomTokenProvider>();
    }

    // -------------------------------------------------------------------------
    // AddBibleCaching
    // -------------------------------------------------------------------------

    [Fact]
    public void AddBibleCaching_ThrowsInvalidOperationException_WhenApiClientsNotRegisteredFirst()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddBibleCaching();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AddBibleApiClients*");
    }

    [Fact]
    public void AddBibleCaching_ReplacesIBibleClient_WithCachingBibleClient()
    {
        var sp = BuildCachingProvider();
        sp.GetRequiredService<IBibleClient>().Should().BeOfType<CachingBibleClient>();
    }

    [Fact]
    public void AddBibleCaching_ReplacesIPassageClient_WithCachingPassageClient()
    {
        var sp = BuildCachingProvider();
        sp.GetRequiredService<IPassageClient>().Should().BeOfType<CachingPassageClient>();
    }

    [Fact]
    public void AddBibleCaching_RegistersHybridCache()
    {
        var sp = BuildCachingProvider();
        sp.GetRequiredService<HybridCache>().Should().NotBeNull();
    }

    [Fact]
    public void AddBibleCaching_RegistersDefaultBibleCacheOptions_WhenConfigureOptionsOmitted()
    {
        var sp = BuildCachingProvider();

        var opts = sp.GetRequiredService<BibleCacheOptions>();

        opts.VersionsTtl.Should().Be(TimeSpan.FromHours(24));
        opts.BooksTtl.Should().Be(TimeSpan.FromHours(24));
        opts.PassageTtl.Should().Be(TimeSpan.FromDays(7));
    }

    [Fact]
    public void AddBibleCaching_AppliesConfiguredOptions_WhenProvided()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "key");
        services.AddBibleCaching(o =>
        {
            o.VersionsTtl = TimeSpan.FromMinutes(1);
            o.BooksTtl = TimeSpan.FromMinutes(2);
            o.PassageTtl = TimeSpan.FromMinutes(3);
        });

        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<BibleCacheOptions>();

        opts.VersionsTtl.Should().Be(TimeSpan.FromMinutes(1));
        opts.BooksTtl.Should().Be(TimeSpan.FromMinutes(2));
        opts.PassageTtl.Should().Be(TimeSpan.FromMinutes(3));
    }

    [Fact]
    public void AddBibleCaching_DoesNotRegisterDistributedCache_WhenRedisConnectionStringNotProvided()
    {
        var sp = BuildCachingProvider();
        sp.GetService<IDistributedCache>().Should().BeNull();
    }

    [Fact]
    public void AddBibleCaching_DoesNotRegisterDistributedCache_WhenRedisConnectionStringIsWhitespace()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "key");
        services.AddBibleCaching(o => o.RedisConnectionString = "   ");

        var sp = services.BuildServiceProvider();

        sp.GetService<IDistributedCache>().Should().BeNull();
    }

    [Fact]
    public void AddBibleCaching_RegistersDistributedCache_WhenRedisConnectionStringProvided()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "key");
        services.AddBibleCaching(o => o.RedisConnectionString = "localhost:6379");

        services.Any(d => d.ServiceType == typeof(IDistributedCache)).Should().BeTrue();
    }

    [Fact]
    public void AddBibleCaching_RegistersMemoryCache_WithSizeLimit_WhenMaxL1EntriesSet()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "key");
        services.AddBibleCaching(o => o.MaxL1Entries = 100);

        var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IOptions<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions>>()
            .Value.SizeLimit.Should().Be(100);
    }

    [Fact]
    public void AddBibleCaching_RegistersMemoryCache_WithoutSizeLimit_WhenMaxL1EntriesNotSet()
    {
        var sp = BuildCachingProvider();

        sp.GetRequiredService<IOptions<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions>>()
            .Value.SizeLimit.Should().BeNull();
    }

    [Fact]
    public void AddBibleCaching_ConfiguresHybridCache_WithFourMegabyteMaximumPayload()
    {
        var sp = BuildCachingProvider();

        sp.GetRequiredService<IOptions<HybridCacheOptions>>()
            .Value.MaximumPayloadBytes.Should().Be(4 * 1024 * 1024);
    }

    [Fact]
    public void AddBibleCaching_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "key");

        var returned = services.AddBibleCaching();

        returned.Should().BeSameAs(services);
    }

    private static ServiceProvider BuildCachingProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "key");
        services.AddBibleCaching();

        return services.BuildServiceProvider();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static ServiceProvider BuildProvider(bool withOAuth)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBibleApiClients(o => o.AppKey = "test-key");

        if (withOAuth)
        {
            services.AddBibleOAuth(o =>
            {
                o.ClientId = "test-client";
                o.RedirectUri = new Uri("https://localhost/callback");
            });
        }

        return services.BuildServiceProvider();
    }

    private sealed class CustomTokenProvider : ITokenProvider
    {
        public Task<OAuthTokenResponse?> GetTokenAsync(
            CancellationToken ct = default)
            => Task.FromResult<OAuthTokenResponse?>(null);

        public Task StoreTokenAsync(OAuthTokenResponse token,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task ClearTokenAsync(
            CancellationToken ct = default)
            => Task.CompletedTask;
    }
}

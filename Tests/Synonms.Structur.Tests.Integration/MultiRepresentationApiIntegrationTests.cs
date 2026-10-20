using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StructurHttpHeaders = Synonms.Structur.Api.Core.Http.HttpHeaders;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.DependencyInjection;
using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Hosting;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.Tests.Integration;

public sealed class MultiRepresentationApiIntegrationTests
{
    [Fact]
    public async Task GetAll_GivenTwoRepresentationPaths_ReturnsEachSelectedResourceContract()
    {
        await using MultiRepresentationApiHost host = await MultiRepresentationApiHost.StartAsync();

        using HttpResponseMessage publicResponse = await host.Client.GetAsync("/public-records", TestContext.Current.CancellationToken);
        using HttpClient authorizedClient = host.GetAuthorizedClient();
        using HttpResponseMessage adminResponse = await authorizedClient.GetAsync("/admin-records", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        using JsonDocument publicDocument = JsonDocument.Parse(await publicResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using JsonDocument adminDocument = JsonDocument.Parse(await adminResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        JsonElement publicResource = Assert.Single(publicDocument.RootElement.GetProperty("resources").EnumerateArray());
        JsonElement adminResource = Assert.Single(adminDocument.RootElement.GetProperty("resources").EnumerateArray());

        Assert.Equal("Published", publicResource.GetProperty("publicName").GetString());
        Assert.False(publicResource.TryGetProperty("internalNotes", out _));
        Assert.Equal("Do not expose", adminResource.GetProperty("internalNotes").GetString());
        Assert.False(adminResource.TryGetProperty("publicName", out _));
    }

    [Fact]
    public async Task GetAll_GivenDifferentRegistrationPolicies_AuthorizesOnlyTheSelectedRepresentation()
    {
        await using MultiRepresentationApiHost host = await MultiRepresentationApiHost.StartAsync();

        using HttpResponseMessage publicResponse = await host.Client.GetAsync("/public-records", TestContext.Current.CancellationToken);
        using HttpResponseMessage protectedResponse = await host.Client.GetAsync("/admin-records", TestContext.Current.CancellationToken);
        using HttpResponseMessage authorizedResponse = await host.GetAuthorizedClient().GetAsync("/admin-records", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);
    }

    [Fact]
    public async Task GetAll_GivenRequestedApiVersion_AllowsSupportedAndRejectsUnsupportedVersions()
    {
        await using MultiRepresentationApiHost host = await MultiRepresentationApiHost.StartAsync();
        using HttpRequestMessage supportedRequest = new(HttpMethod.Get, "/public-records");
        supportedRequest.Headers.Add(StructurHttpHeaders.ApiVersion, "1.1");
        using HttpRequestMessage unsupportedRequest = new(HttpMethod.Get, "/public-records");
        unsupportedRequest.Headers.Add(StructurHttpHeaders.ApiVersion, "2.0");

        using HttpResponseMessage supportedResponse = await host.Client.SendAsync(supportedRequest, TestContext.Current.CancellationToken);
        using HttpResponseMessage unsupportedResponse = await host.Client.SendAsync(unsupportedRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, supportedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unsupportedResponse.StatusCode);
    }

    [Fact]
    public async Task OpenApi_GivenMultipleRegistrations_DescribesEachPathAndEnabledOperation()
    {
        await using MultiRepresentationApiHost host = await MultiRepresentationApiHost.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        JsonElement paths = document.RootElement.GetProperty("paths");
        JsonElement publicCollection = paths.GetProperty("/public-records");
        JsonElement adminCollection = paths.GetProperty("/admin-records");

        Assert.True(publicCollection.TryGetProperty("get", out _));
        Assert.False(publicCollection.TryGetProperty("post", out _));
        Assert.True(adminCollection.TryGetProperty("get", out _));
        Assert.True(adminCollection.TryGetProperty("post", out _));
        Assert.Contains("/public-records/{id}", paths.EnumerateObject().Select(path => path.Name));
        Assert.Contains("/admin-records/{id}", paths.EnumerateObject().Select(path => path.Name));
    }

    [Fact]
    public async Task Post_GivenWritableRegistration_ValidatesAndDispatchesItsSelectedContract()
    {
        await using MultiRepresentationApiHost host = await MultiRepresentationApiHost.StartAsync();
        using HttpClient authorizedClient = host.GetAuthorizedClient();

        using HttpResponseMessage unauthorizedResponse = await host.Client.PostAsync(
            "/admin-records",
            new StringContent("{\"internalNotes\":\"Denied\"}", System.Text.Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        using HttpResponseMessage invalidResponse = await authorizedClient.PostAsync(
            "/admin-records",
            new StringContent("{\"internalNotes\":\" \"}", System.Text.Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        using HttpResponseMessage validResponse = await authorizedClient.PostAsync(
            "/admin-records",
            new StringContent("{\"internalNotes\":\"Validated admin input\"}", System.Text.Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, validResponse.StatusCode);
        Assert.IsType<InternalRecordCreatedEvent>(Assert.Single(host.Store.Events));
        Assert.Equal("Validated admin input", host.Store.Aggregates[^1].InternalNotes);
    }

    [Fact]
    public async Task Post_GivenReadOnlyRegistration_RejectsWriteWithoutDispatchingAnEvent()
    {
        await using MultiRepresentationApiHost host = await MultiRepresentationApiHost.StartAsync();
        using HttpClient authorizedClient = host.GetAuthorizedClient();
        int initialAggregateCount = host.Store.Aggregates.Count;

        using HttpResponseMessage response = await authorizedClient.PostAsync(
            "/public-records",
            new StringContent("{\"publicName\":\"Must not be writable\"}", System.Text.Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(initialAggregateCount, host.Store.Aggregates.Count);
        Assert.Empty(host.Store.Events);
        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(
            response.StatusCode == HttpStatusCode.MethodNotAllowed,
            $"Expected 405 but received {(int)response.StatusCode} ({response.StatusCode}): {responseBody}");
    }
}

internal sealed class MultiRepresentationApiHost : IAsyncDisposable
{
    private readonly WebApplication _application;

    private MultiRepresentationApiHost(WebApplication application, HttpClient client)
    {
        _application = application;
        Client = client;
    }

    public HttpClient Client { get; }

    public MultiRepresentationStore Store => _application.Services.GetRequiredService<MultiRepresentationStore>();

    public HttpClient GetAuthorizedClient()
    {
        HttpClient client = _application.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "integration");
        return client;
    }

    public static async Task<MultiRepresentationApiHost> StartAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        builder.WebHost.UseTestServer();

        StructurOptions options = new()
        {
            Assemblies = [typeof(MultiRepresentationAggregate).Assembly],
            OpenApiDocumentPath = "/openapi/v1.json",
            CorsConfiguration = cors => cors.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(),
            DefaultAuthenticationScheme = "Test",
            AuthenticationConfigurationAction = authentication =>
                authentication.AddScheme<AuthenticationSchemeOptions, IntegrationTestAuthenticationHandler>("Test", _ => { }),
            AuthorizationConfiguration = authorization =>
            {
                authorization.AddPolicy("ReadMultiRepresentationAggregate", policy => policy.RequireAuthenticatedUser());
                authorization.AddPolicy("CreateMultiRepresentationAggregate", policy => policy.RequireAuthenticatedUser());
            }
        };

        builder.Services.AddStructur(options);
        builder.Services.AddSingleton(new MultiRepresentationStore());
        builder.Services.AddSingleton<IReadAggregateRepository<MultiRepresentationAggregate>, MultiRepresentationRepository>();
        builder.Services.AddSingleton<IWriteAggregateRepository<MultiRepresentationAggregate>, MultiRepresentationRepository>();
        builder.Services.AddSingleton<IDomainEventRepository<MultiRepresentationAggregate>, MultiRepresentationEventRepository>();

        WebApplication application = builder.Build();
        application.UseStructur(options);
        await application.StartAsync();

        return new MultiRepresentationApiHost(application, application.GetTestClient());
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _application.DisposeAsync();
    }
}

internal sealed class IntegrationTestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.Authorization != "Test integration")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, "integration-test")], Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

[StructurResource(
    typeof(PublicRecordResource),
    "public-records",
    allowAnonymous: true,
    isCreateDisabled: true,
    isUpdateDisabled: true,
    isDeleteDisabled: true,
    isDefaultRepresentation: true,
    SupportedApiVersions = ["1.0", "1.1"])]
[StructurResource(
    typeof(InternalRecordResource),
    "admin-records",
    isUpdateDisabled: true,
    isDeleteDisabled: true)]
public sealed class MultiRepresentationAggregate : AggregateRoot<MultiRepresentationAggregate>
{
    private MultiRepresentationAggregate()
    {
    }

    public string PublicName { get; private set; } = string.Empty;
    public string InternalNotes { get; private set; } = string.Empty;

    public static MultiRepresentationAggregate Create(string publicName, string internalNotes) => new()
    {
        PublicName = publicName,
        InternalNotes = internalNotes
    };

    public static Result<MultiRepresentationAggregate> Create(ResourceEnvelope<InternalRecordResource> envelope)
    {
        InternalRecordResource resource = envelope.Data;
        if (string.IsNullOrWhiteSpace(resource.InternalNotes))
        {
            return Result<MultiRepresentationAggregate>.Failure(new DomainRuleFault("Internal notes are required."));
        }

        MultiRepresentationAggregate aggregate = new()
        {
            Id = (EntityId<MultiRepresentationAggregate>)envelope.Metadata.Id,
            PublicName = resource.InternalNotes,
            InternalNotes = resource.InternalNotes
        };

        return Result<MultiRepresentationAggregate>.Success(aggregate);
    }
}

public sealed class PublicRecordResource
{
    public string PublicName { get; set; } = string.Empty;
}

public sealed class InternalRecordResource
{
    public string InternalNotes { get; set; } = string.Empty;
}

public sealed class InternalRecordDomainEventFactory : IDomainEventFactory<MultiRepresentationAggregate, InternalRecordResource>
{
    public Task<Result<DomainEvent<MultiRepresentationAggregate>>> GenerateCreatedEvent(ResourceEnvelope<InternalRecordResource> envelope, CancellationToken cancellationToken = default)
    {
        InternalRecordResource resource = envelope.Data;
        if (string.IsNullOrWhiteSpace(resource.InternalNotes))
        {
            return Task.FromResult(Result<DomainEvent<MultiRepresentationAggregate>>.Failure(new DomainRuleFault("Internal notes are required.")));
        }

        DomainEvent<MultiRepresentationAggregate> domainEvent =
            new InternalRecordCreatedEvent(envelope);
        return Task.FromResult(Result<DomainEvent<MultiRepresentationAggregate>>.Success(domainEvent));
    }

    public Task<Result<DomainEvent<MultiRepresentationAggregate>>> GenerateDeletedEvent(EntityId<MultiRepresentationAggregate> aggregateId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<DomainEvent<MultiRepresentationAggregate>>.Failure(new DomainRuleFault("Deletion is not enabled.")));

    public Task<Result<DomainEvent<MultiRepresentationAggregate>>> GenerateUpdatedEvent(ResourceEnvelope<InternalRecordResource> envelope, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<DomainEvent<MultiRepresentationAggregate>>.Failure(new DomainRuleFault("Updates are not enabled.")));
}

internal sealed class InternalRecordCreatedEvent(ResourceEnvelope<InternalRecordResource> envelope)
    : AggregateCreatedDomainEvent<MultiRepresentationAggregate, InternalRecordResource>(envelope, Guid.Empty)
{
    public override Result<MultiRepresentationAggregate> CreateAggregate(ResourceEnvelope<InternalRecordResource> envelope) =>
        MultiRepresentationAggregate.Create(envelope);

    public override void Replay(Synonms.Structur.Domain.Projections.Projection projection)
    {
    }
}

internal sealed class MultiRepresentationStore
{
    public List<MultiRepresentationAggregate> Aggregates { get; } =
    [
        MultiRepresentationAggregate.Create("Published", "Do not expose")
    ];

    public List<DomainEvent<MultiRepresentationAggregate>> Events { get; } = [];
}

internal sealed class MultiRepresentationRepository(MultiRepresentationStore store) :
    IReadAggregateRepository<MultiRepresentationAggregate>,
    IWriteAggregateRepository<MultiRepresentationAggregate>
{
    public Task<bool> AnyAsync(Expression<Func<MultiRepresentationAggregate, bool>> predicate, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Aggregates.AsQueryable().Any(predicate));

    public Task<Maybe<MultiRepresentationAggregate>> FindAsync(EntityId<MultiRepresentationAggregate> id, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Aggregates.SingleOrDefault(aggregate => aggregate.Id == id) is { } aggregate
            ? Maybe<MultiRepresentationAggregate>.Some(aggregate)
            : Maybe<MultiRepresentationAggregate>.None);

    public Task<Maybe<MultiRepresentationAggregate>> FindFirstAsync(Expression<Func<MultiRepresentationAggregate, bool>> predicate, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Aggregates.AsQueryable().FirstOrDefault(predicate) is { } aggregate
            ? Maybe<MultiRepresentationAggregate>.Some(aggregate)
            : Maybe<MultiRepresentationAggregate>.None);

    public Task<List<MultiRepresentationAggregate>> ListAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Aggregates.ToList());

    public Task<List<MultiRepresentationAggregate>> ListAsync(Expression<Func<MultiRepresentationAggregate, bool>> predicate, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Aggregates.AsQueryable().Where(predicate).ToList());

    public IQueryable<MultiRepresentationAggregate> Query() => store.Aggregates.AsQueryable();

    public IQueryable<MultiRepresentationAggregate> Query(Expression<Func<MultiRepresentationAggregate, bool>> predicate) =>
        store.Aggregates.AsQueryable().Where(predicate);

    public Task<PaginatedList<MultiRepresentationAggregate>> ReadAllAsync(
        int offset,
        int limit,
        Func<IQueryable<MultiRepresentationAggregate>, IQueryable<MultiRepresentationAggregate>> sortFunc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(PaginatedList<MultiRepresentationAggregate>.Create(sortFunc(Query()), offset, limit));

    public Task<PaginatedList<MultiRepresentationAggregate>> ReadAsync(
        Expression<Func<MultiRepresentationAggregate, bool>> predicate,
        int offset,
        int limit,
        Func<IQueryable<MultiRepresentationAggregate>, IQueryable<MultiRepresentationAggregate>> sortFunc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(PaginatedList<MultiRepresentationAggregate>.Create(sortFunc(Query(predicate)), offset, limit));

    public Task<List<TResult>> SelectAsync<TResult>(
        Expression<Func<MultiRepresentationAggregate, bool>> predicate,
        Expression<Func<MultiRepresentationAggregate, TResult>> selector,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Aggregates.AsQueryable().Where(predicate).Select(selector).ToList());

    public Task AddAsync(MultiRepresentationAggregate entity, CancellationToken cancellationToken = default)
    {
        store.Aggregates.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<MultiRepresentationAggregate> entities, CancellationToken cancellationToken = default)
    {
        store.Aggregates.AddRange(entities);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(MultiRepresentationAggregate entity, CancellationToken cancellationToken = default)
    {
        store.Aggregates.Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(EntityId<MultiRepresentationAggregate> id, CancellationToken cancellationToken = default)
    {
        store.Aggregates.RemoveAll(aggregate => aggregate.Id == id);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Expression<Func<MultiRepresentationAggregate, bool>> predicate, CancellationToken cancellationToken = default)
    {
        Func<MultiRepresentationAggregate, bool> compiledPredicate = predicate.Compile();
        store.Aggregates.RemoveAll(aggregate => compiledPredicate(aggregate));
        return Task.CompletedTask;
    }

    public Task UpdateAsync(MultiRepresentationAggregate entity, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class MultiRepresentationEventRepository(MultiRepresentationStore store) : IDomainEventRepository<MultiRepresentationAggregate>
{
    public Task<Maybe<Fault>> CreateAsync(DomainEvent<MultiRepresentationAggregate> domainEvent, CancellationToken cancellationToken = default)
    {
        store.Events.Add(domainEvent);
        return Task.FromResult(Maybe<Fault>.None);
    }

    public Task<IEnumerable<DomainEvent<MultiRepresentationAggregate>>> ReadAllAsync(
        EntityId<MultiRepresentationAggregate> aggregateId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<DomainEvent<MultiRepresentationAggregate>>>(store.Events.Where(domainEvent => domainEvent.AggregateId == aggregateId));
}

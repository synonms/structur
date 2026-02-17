using System.Net;
using System.Text.Json;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Testing.Assertions;
using Synonms.Structur.Testing.Extensions;
using Xunit;

namespace Synonms.Structur.Testing.Tests;

public static class GetByIdTest<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public static GetByIdTestBuilder<TAggregateRoot, TResource> Create(StructurTestFixture testFixture, IGetByIdTestFeature<TAggregateRoot, TResource> testFeature) => 
        new(testFixture, testFeature);
}

public class GetByIdTestBuilder<TAggregateRoot, TResource> 
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public GetByIdTestBuilder(StructurTestFixture testFixture, IGetByIdTestFeature<TAggregateRoot, TResource> testFeature)
    {
        Arrange = new PreArrangeStep(testFixture, testFeature);
    }

    public PreArrangeStep Arrange { get; }
    
    public class PreArrangeStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IGetByIdTestFeature<TAggregateRoot, TResource> _testFeature;
        private readonly EntityId<TAggregateRoot> _id = EntityId<TAggregateRoot>.New();
        
        public PreArrangeStep(StructurTestFixture testFixture, IGetByIdTestFeature<TAggregateRoot, TResource> testFeature)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
        }

        public PostArrangeStep WithoutAggregate() =>
            new(_testFixture, _testFeature, _id, null);

        public PostArrangeStep WithAggregate()
        {
            ArrangeAggregateInfo<TAggregateRoot> arrangeAggregateInfo = _testFeature.GenerateUniqueAggregate(_id);

            TAggregateRoot persistedAggregateRoot = _testFeature.PersistAggregateAsync(_testFixture.ServiceScopeFactory, arrangeAggregateInfo).Result;

            return new PostArrangeStep(_testFixture, _testFeature, _id, persistedAggregateRoot);
        }
    }
    
    public class PostArrangeStep
    {
        public PostArrangeStep(StructurTestFixture testFixture, IGetByIdTestFeature<TAggregateRoot, TResource> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot)
        {
            Act = new ActStep(testFixture, testFeature, id, aggregateRoot);
        }
        
        public ActStep Act { get; }
    }
    
    public class ActStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IGetByIdTestFeature<TAggregateRoot, TResource> _testFeature;
        private readonly TAggregateRoot? _aggregateRoot;
        private readonly EntityId<TAggregateRoot> _id;

        public ActStep(StructurTestFixture testFixture, IGetByIdTestFeature<TAggregateRoot, TResource> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _id = id;
            _aggregateRoot = aggregateRoot;
        }

        public AssertStep Assert =>
            new(_testFixture, _testFeature, _id, _aggregateRoot);

        public ActStep WithAuthenticatedUser(string userId, params string[] permissions)
        {
            _testFixture.HttpClient.WithAuthenticatedUser(userId, permissions);
            
            return this;
        }
    }
    
    public class AssertStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IGetByIdTestFeature<TAggregateRoot, TResource> _testFeature;
        private readonly EntityId<TAggregateRoot> _id;
        private readonly TAggregateRoot? _aggregateRoot;
        private readonly string _getByIdPath;

        public AssertStep(StructurTestFixture testFixture, IGetByIdTestFeature<TAggregateRoot, TResource> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _id = id;
            _aggregateRoot = aggregateRoot;

            _getByIdPath = _testFeature.CollectionPath + "/" + id.Value;
        }
        
        public void SucceedsWith(HttpStatusCode httpStatusCode)
        {
            Assert.True(_aggregateRoot is not null, "Entity not arranged when testing success path - call Arrange.WithAggregate() in test body.");
            
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.GetAsync(_getByIdPath).Result;

            httpResponseMessage.AssertSuccess(httpStatusCode);

            string content = httpResponseMessage.Content.ReadAsStringAsync().Result;

            ResourceDocument<TResource>? resourceDocument = JsonSerializer.Deserialize<ResourceDocument<TResource>>(content, _testFixture.JsonSerializerOptions);

            if (resourceDocument is null)
            {
                Assert.Fail($"Failed to deserialise {nameof(ResourceDocument<TResource>)} from response: {content}");
                return;
            }

            _testFeature.ValidateResource(_aggregateRoot!, resourceDocument.Resource);
            
            // TODO: Test Resource Links
//                AssertThat.Links(resource.Links).Presents(/*TODO*/);

            Dictionary<string, Link> expectedLinks = new()
            {
                [IanaLinkRelationConstants.Self] = Link.SelfLink(new Uri("/" + _getByIdPath, UriKind.Relative))
            };

            AssertThat.Links(resourceDocument.Links).Presents(expectedLinks);
        }
        
        public void FailsWith(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.GetAsync(_getByIdPath).Result;

            httpResponseMessage.AssertFailure(httpStatusCode);
        }
    }
}
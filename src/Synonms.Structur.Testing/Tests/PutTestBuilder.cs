using System.Net;
using System.Text;
using System.Text.Json;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Testing.Extensions;
using Xunit;

namespace Synonms.Structur.Testing.Tests;

public static class PutTest<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public static PutTestBuilder<TAggregateRoot, TResource> Create(StructurTestFixture testFixture, IPutTestFeature<TAggregateRoot, TResource> testFeature) => 
        new(testFixture, testFeature);
}

public class PutTestBuilder<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public PutTestBuilder(StructurTestFixture testFixture, IPutTestFeature<TAggregateRoot, TResource> testFeature)
    {
        Arrange = new ArrangeEntityStep(testFixture, testFeature);
    }

    public ArrangeEntityStep Arrange { get; }
    
    public class ArrangeEntityStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IPutTestFeature<TAggregateRoot, TResource> _testFeature;
        private readonly EntityId<TAggregateRoot> _id = EntityId<TAggregateRoot>.New();

        public ArrangeEntityStep(StructurTestFixture testFixture, IPutTestFeature<TAggregateRoot, TResource> testFeature)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
        }

        public ArrangeTemplateStep WithoutAggregate() =>
            new(_testFixture, _testFeature, _id, null);

        public ArrangeTemplateStep WithAggregate()
        {
            ArrangeAggregateInfo<TAggregateRoot> arrangeAggregateInfo = _testFeature.GenerateUniqueAggregate(_id);
            
            TAggregateRoot persistedAggregateRoot = _testFeature.PersistAggregateAsync(_testFixture.ServiceScopeFactory, arrangeAggregateInfo).Result;

            return new ArrangeTemplateStep(_testFixture, _testFeature, _id, persistedAggregateRoot);
        }
    }
    
    public class ArrangeTemplateStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IPutTestFeature<TAggregateRoot, TResource> _testFeature;
        private readonly EntityId<TAggregateRoot> _id;
        private readonly TAggregateRoot? _aggregateRoot;

        public ArrangeTemplateStep(StructurTestFixture testFixture, IPutTestFeature<TAggregateRoot, TResource> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _id = id;
            _aggregateRoot = aggregateRoot;
        }

        public PostArrangeStep WithValidResource()
        {
            TResource resource = _testFeature.GenerateValidResource(_id);
            
            return new PostArrangeStep(_testFixture, _testFeature, _id, _aggregateRoot, resource);
        }
        
        public PostArrangeStep WithInvalidResource()
        {
            TResource resource = _testFeature.GenerateInvalidResource(_id);
            
            return new PostArrangeStep(_testFixture, _testFeature, _id, _aggregateRoot, resource);
        }
    }
    
    public class PostArrangeStep
    {
        public PostArrangeStep(StructurTestFixture testFixture, IPutTestFeature<TAggregateRoot, TResource> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot, TResource resource)
        {
            Act = new ActStep(testFixture, testFeature, id, aggregateRoot, resource);
        }
        
        public ActStep Act { get; }
    }
    
    public class ActStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IPutTestFeature<TAggregateRoot, TResource> _testFeature;
        private readonly TResource _resource;
        private readonly TAggregateRoot? _aggregateRoot;
        private readonly EntityId<TAggregateRoot> _id;
        private EntityTag? _entityTag = null;

        public ActStep(StructurTestFixture testFixture, IPutTestFeature<TAggregateRoot, TResource> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot, TResource resource)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _resource = resource;
            _id = id;
            _aggregateRoot = aggregateRoot;

            if (aggregateRoot is not null)
            {
                _entityTag = aggregateRoot.EntityTag;
            }
        }

        public AssertStep Assert =>
            new(_testFixture, _testFeature, _id, _aggregateRoot, _resource, _entityTag);

        public ActStep WithAuthenticatedUser(string userId, params string[] permissions)
        {
            _testFixture.HttpClient.WithAuthenticatedUser(userId, permissions);
            
            return this;
        }
        
        public ActStep WithoutVersion()
        {
            // TODO: Update HttpClient
            _entityTag = null;

            return this;
        }

        public ActStep WithEntityTag(EntityTag entityTag)
        {
            // TODO: Update HttpClient
            _entityTag = entityTag;

            return this;
        }
    }
    
    public class AssertStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IPutTestFeature<TAggregateRoot, TResource> _testFeature;
        private readonly EntityId<TAggregateRoot> _id;
        private readonly TAggregateRoot? _aggregateRoot;
        private readonly TResource _resource;
        private readonly EntityTag? _entityTag;
        private readonly string _putPath;

        public AssertStep(StructurTestFixture testFixture, IPutTestFeature<TAggregateRoot, TResource> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot, TResource resource, EntityTag? entityTag)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _id = id;
            _aggregateRoot = aggregateRoot;
            _resource = resource;
            _entityTag = entityTag;

            _putPath = _testFeature.CollectionPath + "/" + id.Value;
        }

        public void SucceedsWith(HttpStatusCode httpStatusCode)
        {
            Assert.True(_aggregateRoot is not null, "Aggregate not arranged when testing success path - call Arrange.WithAggregate() in test body.");
            
            string json = JsonSerializer.Serialize(_resource, _testFixture.JsonSerializerOptions);
            StringContent content = new(json, Encoding.UTF8, _testFixture.MediaType);
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.PutAsync(_putPath, content).Result;

            httpResponseMessage.AssertSuccess(httpStatusCode);

            TAggregateRoot? retrievedAggregateRoot = _testFeature.RetrieveAggregateAsync(_testFixture.ServiceScopeFactory, _id).Result;

            if (retrievedAggregateRoot is null)
            {
                Assert.Fail($"Unable to retrieve aggregate id [{_id}].");
                return;
            }

            _testFeature.ValidateUpdatedAggregate(retrievedAggregateRoot, _resource);
        }
        
        public void FailsWith(HttpStatusCode httpStatusCode)
        {
            string json = JsonSerializer.Serialize(_resource, _testFixture.JsonSerializerOptions);
            StringContent content = new(json, Encoding.UTF8, _testFixture.MediaType);
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.PutAsync(_putPath, content).Result;

            httpResponseMessage.AssertFailure(httpStatusCode);
        }
    }
}
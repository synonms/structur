using System.Net;
using System.Text;
using System.Text.Json;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Testing.Extensions;
using Xunit;

namespace Synonms.Structur.Testing.Tests;

public static class PostTest<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public static PostTestBuilder<TAggregateRoot, TResource> Create(StructurTestFixture testFixture, IPostTestFeature<TAggregateRoot, TResource> testFeature) => 
        new(testFixture, testFeature);
}

public class PostTestBuilder<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public PostTestBuilder(StructurTestFixture testFixture, IPostTestFeature<TAggregateRoot, TResource> testFeature)
    {
        Arrange = new PreArrangeStep(testFixture, testFeature);
    }

    public PreArrangeStep Arrange { get; }
    
    public class PreArrangeStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IPostTestFeature<TAggregateRoot, TResource> _testFeature;
        private readonly EntityId<TAggregateRoot> _id = EntityId<TAggregateRoot>.New();

        public PreArrangeStep(StructurTestFixture testFixture, IPostTestFeature<TAggregateRoot, TResource> testFeature)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
        }
        
        public PostArrangeStep WithValidResource(params object[]? prerequisiteEntities) =>
            WithValidResource(new ArrangeEntitiesInfo(prerequisiteEntities));
        
        public PostArrangeStep WithValidResource(ArrangeEntitiesInfo prerequisiteEntities)
        {
            _testFeature.PersistPrerequisitesAsync(prerequisiteEntities).Wait();

            TResource resource = _testFeature.GenerateValidResource(_id);
            
            return new PostArrangeStep(_testFixture, _testFeature, _id, resource);
        }

        public PostArrangeStep WithInvalidResource(params object[]? prerequisiteEntities) =>
            WithInvalidResource(new ArrangeEntitiesInfo(prerequisiteEntities));

        public PostArrangeStep WithInvalidResource(ArrangeEntitiesInfo prerequisiteEntities)
        {
            _testFeature.PersistPrerequisitesAsync(prerequisiteEntities).Wait();
            
            TResource resource = _testFeature.GenerateInvalidResource(_id);
            
            return new PostArrangeStep(_testFixture, _testFeature, _id, resource);
        }
    }
    
    public class PostArrangeStep
    {
        public PostArrangeStep(StructurTestFixture testFixture, IPostTestFeature<TAggregateRoot, TResource> testFeature, EntityId<TAggregateRoot> id, TResource resource)
        {
            Act = new ActStep(testFixture, testFeature, id, resource);
        }
        
        public ActStep Act { get; }
    }
    
    public class ActStep
    {
        private readonly StructurTestFixture _testFixture;

        public ActStep(StructurTestFixture testFixture, IPostTestFeature<TAggregateRoot, TResource> testFeature, EntityId<TAggregateRoot> id, TResource resource)
        {
            _testFixture = testFixture;

            Assert = new AssertStep(testFixture, testFeature, id, resource);
        }

        public AssertStep Assert { get; }

        public ActStep WithApiVersion(int apiVersion)
        {
            _testFixture.HttpClient.WithApiVersion(apiVersion);

            return this;
        }

        public ActStep WithAuthenticatedUser(string userId, params string[] permissions)
        {
            _testFixture.HttpClient.WithAuthenticatedUser(userId, permissions);
            
            return this;
        }
        
        public ActStep WithCorrelationId(Guid correlationId)
        {
            _testFixture.HttpClient.WithCorrelationId(correlationId);

            return this;
        }
    }
    
    public class AssertStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IPostTestFeature<TAggregateRoot, TResource> _testFeature;
        private readonly EntityId<TAggregateRoot> _id;
        private readonly TResource _resource;
        private readonly string _postPath;

        public AssertStep(StructurTestFixture testFixture, IPostTestFeature<TAggregateRoot, TResource> testFeature, EntityId<TAggregateRoot> id, TResource resource)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _id = id;
            _resource = resource;

            _postPath = testFeature.CollectionPath;
        }

        public void SucceedsWith(HttpStatusCode httpStatusCode)
        {
            string json = JsonSerializer.Serialize(_resource, _testFixture.JsonSerializerOptions);
            StringContent content = new(json, Encoding.UTF8, _testFixture.MediaType);
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.PostAsync(_postPath, content).Result;

            httpResponseMessage.AssertSuccess(httpStatusCode);

            Guid? newResourceId = httpResponseMessage.Headers.Location.ExtractTrailingId();

            if (newResourceId is null)
            {
                Assert.Fail($"Unable to determine new resource id from location [{httpResponseMessage.Headers.Location}].");
                return;
            }

            if (newResourceId != _id.Value)
            {
                Assert.Fail($"Resource id from location {newResourceId}] does not match arranged Id {_id.Value}.");
                return;
            }

            TAggregateRoot? retrievedAggregateRoot = _testFeature.RetrieveAggregateAsync(_testFixture.ServiceScopeFactory, _id).Result;

            if (retrievedAggregateRoot is null)
            {
                Assert.Fail($"Unable to retrieve aggregate id [{_id}].");
                return;
            }

            _testFeature.ValidateCreatedAggregate(retrievedAggregateRoot, _resource);
        }
        
        public void FailsWith(HttpStatusCode httpStatusCode)
        {
            string json = JsonSerializer.Serialize(_resource, _testFixture.JsonSerializerOptions);
            StringContent content = new(json, Encoding.UTF8, _testFixture.MediaType);
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.PostAsync(_postPath, content).Result;

            httpResponseMessage.AssertFailure(httpStatusCode);
        }
    }
}
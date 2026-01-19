using System.Net;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Testing.Extensions;
using Xunit;

namespace Synonms.Structur.Testing.Tests;

public static class DeleteTest<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public static DeleteTestBuilder<TAggregateRoot> Create(StructurTestFixture testFixture, IDeleteTestFeature<TAggregateRoot> testFeature) => 
        new(testFixture, testFeature);
}

public class DeleteTestBuilder<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public DeleteTestBuilder(StructurTestFixture testFixture, IDeleteTestFeature<TAggregateRoot> testFeature)
    {
        Arrange = new PreArrangeStep(testFixture, testFeature);
    }

    public PreArrangeStep Arrange { get; }
    
    public class PreArrangeStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IDeleteTestFeature<TAggregateRoot> _testFeature;
        private readonly EntityId<TAggregateRoot> _id = EntityId<TAggregateRoot>.New();
        
        public PreArrangeStep(StructurTestFixture testFixture, IDeleteTestFeature<TAggregateRoot> testFeature)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
        }

        public PostArrangeStep WithoutAggregate() =>
            new(_testFixture, _testFeature, _id);

        public PostArrangeStep WithAggregate()
        {
            ArrangeAggregateInfo<TAggregateRoot> arrangeAggregateInfo = _testFeature.GenerateUniqueAggregate(_id);
            
            TAggregateRoot persistedAggregateRoot = _testFeature.PersistAggregateAsync(_testFixture.ServiceScopeFactory, arrangeAggregateInfo).Result;

            return new PostArrangeStep(_testFixture, _testFeature, _id, persistedAggregateRoot);
        }
    }
    
    public class PostArrangeStep
    {
        public PostArrangeStep(StructurTestFixture testFixture, IDeleteTestFeature<TAggregateRoot> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot = null)
        {
            Act = new ActStep(testFixture, testFeature, id, aggregateRoot);
        }
        
        public ActStep Act { get; }
    }
    
    public class ActStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IDeleteTestFeature<TAggregateRoot> _testFeature;
        private readonly EntityId<TAggregateRoot> _id;
        private readonly TAggregateRoot? _aggregateRoot;
        private EntityTag? _entityTag = null;

        public ActStep(StructurTestFixture testFixture, IDeleteTestFeature<TAggregateRoot> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _id = id;
            _aggregateRoot = aggregateRoot;

            if (aggregateRoot is not null)
            {
                _entityTag = aggregateRoot.EntityTag;
            }
        }

        public AssertStep Assert =>
            new(_testFixture, _testFeature, _id, _aggregateRoot, _entityTag);

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
        private readonly IDeleteTestFeature<TAggregateRoot> _testFeature;
        private readonly EntityId<TAggregateRoot> _id;
        private readonly TAggregateRoot? _aggregateRoot;
        private readonly EntityTag? _entityTag;
        private readonly string _deletePath;

        public AssertStep(StructurTestFixture testFixture, IDeleteTestFeature<TAggregateRoot> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot, EntityTag? entityTag)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _id = id;
            _aggregateRoot = aggregateRoot;
            _entityTag = entityTag;

            _deletePath = _testFeature.CollectionPath + "/" + _id.Value;
        }
        
        public void SucceedsWith(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.DeleteAsync(_deletePath).Result;

            httpResponseMessage.AssertSuccess(httpStatusCode);

            TAggregateRoot? retrievedAggregateRoot = _testFeature.RetrieveAggregateAsync(_testFixture.ServiceScopeFactory, _id).Result;

            Assert.NotNull(retrievedAggregateRoot);
            Assert.True(retrievedAggregateRoot.IsDeleted);
        }
        
        public void FailsWith(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.DeleteAsync(_deletePath).Result;

            httpResponseMessage.AssertFailure(httpStatusCode);
        }
    }
}
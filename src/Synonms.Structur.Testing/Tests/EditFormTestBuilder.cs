using System.Net;
using System.Text.Json;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Forms;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Testing.Assertions;
using Synonms.Structur.Testing.Extensions;
using Xunit;

namespace Synonms.Structur.Testing.Tests;

public static class EditFormTest<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public static EditFormTestBuilder<TAggregateRoot> Create(StructurTestFixture testFixture, IEditFormTestFeature<TAggregateRoot> testFeature) => 
        new(testFixture, testFeature);
}

public class EditFormTestBuilder<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public EditFormTestBuilder(StructurTestFixture testFixture, IEditFormTestFeature<TAggregateRoot> testFeature)
    {
        Arrange = new PreArrangeStep(testFixture, testFeature);
    }

    public PreArrangeStep Arrange { get; }
    
    public class PreArrangeStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IEditFormTestFeature<TAggregateRoot> _testFeature;
        private readonly EntityId<TAggregateRoot> _id = EntityId<TAggregateRoot>.New();

        public PreArrangeStep(StructurTestFixture testFixture, IEditFormTestFeature<TAggregateRoot> testFeature)
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
        public PostArrangeStep(StructurTestFixture testFixture, IEditFormTestFeature<TAggregateRoot> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot)
        {
            Act = new ActStep(testFixture, testFeature, id, aggregateRoot);
        }
        
        public ActStep Act { get; }
    }
    
    public class ActStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IEditFormTestFeature<TAggregateRoot> _testFeature;
        private readonly EntityId<TAggregateRoot> _id;
        private readonly TAggregateRoot? _aggregateRoot;

        public ActStep(StructurTestFixture testFixture, IEditFormTestFeature<TAggregateRoot> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _id = id;
            _aggregateRoot = aggregateRoot;
        }

        public AssertStep Assert =>
            new(_testFixture, _testFeature, _id, _aggregateRoot);
        
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

        // public ActStep WithEntityTag(EntityVersion entityVersion)
        // {
        //     _httpClient.WithEntityTag(entityVersion);
        //     
        //     return this;
        // }
        
        public ActStep WithCorrelationId(Guid correlationId)
        {
            _testFixture.HttpClient.WithCorrelationId(correlationId);

            return this;
        }
    }
    
    public class AssertStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IEditFormTestFeature<TAggregateRoot> _testFeature;
        private readonly EntityId<TAggregateRoot> _id;
        private readonly TAggregateRoot? _aggregateRoot;
        private readonly string _editFormPath;

        public AssertStep(StructurTestFixture testFixture, IEditFormTestFeature<TAggregateRoot> testFeature, EntityId<TAggregateRoot> id, TAggregateRoot? aggregateRoot)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _id = id;
            _aggregateRoot = aggregateRoot;

            _editFormPath = testFeature.CollectionPath + "/" + id.Value + "/" + IanaLinkRelationConstants.Forms.Edit;
        }

        public void SucceedsWith(HttpStatusCode httpStatusCode)
        {
            Assert.True(_aggregateRoot is not null, "Aggregate not arranged when testing success path - call Arrange.WithAggregate() in test body.");

            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.GetAsync(_editFormPath).Result;

            httpResponseMessage.AssertSuccess(httpStatusCode);

            string content = httpResponseMessage.Content.ReadAsStringAsync().Result;

            FormDocument? formDocument = JsonSerializer.Deserialize<FormDocument>(content, _testFixture.JsonSerializerOptions);

            if (formDocument is null)
            {
                Assert.Fail($"Failed to deserialise {nameof(FormDocument)} from response: {content}");
                return;
            }

            _testFeature.ValidateEditForm(formDocument.Form, _aggregateRoot);

            Dictionary<string, Link> expectedLinks = new()
            {
                [IanaLinkRelationConstants.Self] = Link.SelfLink(new Uri("/" + _editFormPath, UriKind.Relative))
            };

            AssertThat.Links(formDocument.Links).Presents(expectedLinks);
        }
        
        public void FailsWith(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.GetAsync(_editFormPath).Result;

            httpResponseMessage.AssertFailure(httpStatusCode);
        }
    }
}
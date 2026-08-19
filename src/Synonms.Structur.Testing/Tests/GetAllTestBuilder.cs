using System.Net;
using System.Text.Json;
using Synonms.Structur.Api.Core.Content;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Testing.Assertions;
using Synonms.Structur.Testing.Extensions;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Xunit;

namespace Synonms.Structur.Testing.Tests;

public static class GetAllTest<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public static GetAllTestBuilder<TAggregateRoot, TResource> Create(StructurTestFixture testFixture, IGetAllTestFeature<TAggregateRoot, TResource> testFeature) => 
        new(testFixture, testFeature);
}

public class GetAllTestBuilder<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public GetAllTestBuilder(StructurTestFixture testFixture, IGetAllTestFeature<TAggregateRoot, TResource> testFeature)
    {
        Arrange = new PreArrangeStep(testFixture, testFeature);
    }

    public PreArrangeStep Arrange { get; }
    
    public class PreArrangeStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IGetAllTestFeature<TAggregateRoot, TResource> _testFeature;

        public PreArrangeStep(StructurTestFixture testFixture, IGetAllTestFeature<TAggregateRoot, TResource> testFeature)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
        }

        public PostArrangeStep WithoutAggregates() =>
            new(_testFixture, _testFeature, []);

        public PostArrangeStep WithAggregates(int count)
        {
            List<ArrangeAggregateInfo<TAggregateRoot>> arrangeAggregateInfos = Enumerable.Range(0, count)
                .Select(_ => _testFeature.GenerateUniqueAggregate(EntityId<TAggregateRoot>.New()))
                .ToList();
                
            List<TAggregateRoot> persistedAggregateRoots = arrangeAggregateInfos
                .Select(arrangeAggregateInfo => _testFeature.PersistAggregateAsync(_testFixture.ServiceScopeFactory, arrangeAggregateInfo).Result)
                .ToList();

            return new PostArrangeStep(_testFixture, _testFeature, persistedAggregateRoots);
        }
    }
    
    public class PostArrangeStep
    {
        public PostArrangeStep(StructurTestFixture testFixture, IGetAllTestFeature<TAggregateRoot, TResource> testFeature, IEnumerable<TAggregateRoot> aggregateRoots)
        {
            Act = new ActStep(testFixture, testFeature, aggregateRoots);
        }
        
        public ActStep Act { get; }
    }
    
    public class ActStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IGetAllTestFeature<TAggregateRoot, TResource> _testFeature;
        private readonly IEnumerable<TAggregateRoot> _aggregateRoots;
        private int _offset = 0;
        private int _pageLimit = Pagination.DefaultPageLimit;

        public ActStep(StructurTestFixture testFixture, IGetAllTestFeature<TAggregateRoot, TResource> testFeature, IEnumerable<TAggregateRoot> aggregateRoots)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _aggregateRoots = aggregateRoots;
        }

        public AssertStep Assert => new (_testFixture, _testFeature, _aggregateRoots, _offset, _pageLimit);

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

        public ActStep WithOffset(int offset)
        {
            _offset = offset;

            return this;
        }
        
        public ActStep WithPageLimit(int pageLimit)
        {
            _pageLimit = pageLimit;

            return this;
        }
    }
    
    public class AssertStep
    {
        private readonly StructurTestFixture _testFixture;
        private readonly IGetAllTestFeature<TAggregateRoot, TResource> _testFeature;
        private readonly string _getAllPath;
        private readonly string _createFormPath;
        private readonly IEnumerable<TAggregateRoot> _aggregateRoots;
        private readonly int _offset;
        private readonly int _pageLimit;

        public AssertStep(StructurTestFixture testFixture, IGetAllTestFeature<TAggregateRoot, TResource> testFeature, IEnumerable<TAggregateRoot> aggregateRoots, int offset, int pageLimit)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            _aggregateRoots = aggregateRoots;
            _offset = offset;
            _pageLimit = pageLimit;

            _getAllPath = offset == 0 ? _testFeature.CollectionPath : _testFeature.CollectionPath + "?offset=" + offset;
            _createFormPath = _testFeature.CollectionPath + "/" + IanaLinkRelationConstants.Forms.Create;
        }

        public void SucceedsWith(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.GetAsync(_getAllPath).Result;

            httpResponseMessage.AssertSuccess(httpStatusCode);
            
            string content = httpResponseMessage.Content.ReadAsStringAsync().Result;

            ResourceCollectionDocument<TResource>? resourceCollectionDocument = JsonSerializer.Deserialize<ResourceCollectionDocument<TResource>>(content, _testFixture.JsonSerializerOptions);

            if (resourceCollectionDocument is null)
            {
                Assert.Fail($"Failed to deserialise {nameof(ResourceCollectionDocument<TResource>)} from response: {content}");
                return;
            }

            int matchedResourceCount = 0;
            
            foreach (TAggregateRoot aggregateRoot in _aggregateRoots)
            {
                TResource? resource = resourceCollectionDocument.Resources.SingleOrDefault(x => x.Id.Equals(aggregateRoot.Id.Value));

                if (resource is null)
                {
                    if (_pageLimit >= _aggregateRoots.Count())
                    {
                        // We expect to all aggregates to be present
                        Assert.True(resource is not null, $"Unable to find resource for entity Id=[{aggregateRoot.Id}] in document.");
                    }
                    
                    continue;
                }

                matchedResourceCount++;

                _testFeature.ValidateResource(aggregateRoot, resource);
                
                // TODO: Test Resource Links
//                AssertThat.Links(resource.Links).Presents(/*TODO*/);
            }

            if (_pageLimit > _aggregateRoots.Count())
            {
                Assert.Equal(_aggregateRoots.Count(), matchedResourceCount);
            }
            else
            {
                Assert.Equal(_pageLimit, matchedResourceCount);
            }
            
            Dictionary<string, Link> expectedLinks = new()
            {
                [IanaLinkRelationConstants.Self] = Link.SelfLink(new Uri("/" + _getAllPath, UriKind.Relative))
            };

            if (_testFixture.MediaType == MediaTypes.Ion)
            {
                expectedLinks.Add(IanaLinkRelationConstants.Forms.Create, Link.CreateFormLink(new Uri("/" + _createFormPath, UriKind.Relative)));
            }

            AssertThat.Links(resourceCollectionDocument.Links).Presents(expectedLinks);
            
            // TODO: Test Pagination Links
//            AssertThat.LinksFromPagination(resourceCollectionDocument.Pagination).Presents(/*TODO*/);
        }
        
        public void FailsWith(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.GetAsync(_getAllPath).Result;

            httpResponseMessage.AssertFailure(httpStatusCode);
        }
    }
}
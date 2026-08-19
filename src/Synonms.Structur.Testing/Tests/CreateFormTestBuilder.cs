using System.Net;
using System.Text.Json;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Forms;
using Synonms.Structur.Testing.Assertions;
using Synonms.Structur.Testing.Extensions;
using Xunit;

namespace Synonms.Structur.Testing.Tests;

public static class CreateFormTest
{
    public static CreateFormTestBuilder Create(StructurTestFixture testFixture, ICreateFormTestFeature testFeature) => 
        new(testFixture, testFeature);
}

public class CreateFormTestBuilder
{
    public CreateFormTestBuilder(StructurTestFixture testFixture, ICreateFormTestFeature testFeature)
    {
        Arrange = new ArrangeStep(testFixture, testFeature);
    }
    
    public ArrangeStep Arrange { get; }
    
    public class ArrangeStep
    {
        public ArrangeStep(StructurTestFixture testFixture, ICreateFormTestFeature testFeature)
        {
            Act = new ActStep(testFixture, testFeature);
        }
        
        public ActStep Act { get; }
    }
    
    public class ActStep
    {
        private readonly StructurTestFixture _testFixture;

        public ActStep(StructurTestFixture testFixture, ICreateFormTestFeature testFeature)
        {
            _testFixture = testFixture;
            
            Assert = new AssertStep(testFixture, testFeature);
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
        private readonly ICreateFormTestFeature _testFeature;
        private readonly string _createFormPath;

        public AssertStep(StructurTestFixture testFixture, ICreateFormTestFeature testFeature)
        {
            _testFixture = testFixture;
            _testFeature = testFeature;
            
            _createFormPath = _testFeature.CollectionPath + "/" + IanaLinkRelationConstants.Forms.Create;
        }

        public void SucceedsWith(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.GetAsync(_createFormPath).Result;

            httpResponseMessage.AssertSuccess(httpStatusCode);

            string content = httpResponseMessage.Content.ReadAsStringAsync().Result;

            FormDocument? formDocument = JsonSerializer.Deserialize<FormDocument>(content, _testFixture.JsonSerializerOptions);

            if (formDocument is null)
            {
                Assert.Fail($"Failed to deserialise {nameof(FormDocument)} from response: {content}");
                return;
            }

            Assert.Equal(new Uri("/" + _testFeature.CollectionPath, UriKind.Relative), formDocument.Form.Target.Uri);
            Assert.Equal(IanaLinkRelationConstants.Forms.Create, formDocument.Form.Target.Relation);
            Assert.Equal(IanaHttpMethodConstants.Post, formDocument.Form.Target.Method);
            
            _testFeature.ValidateCreateForm(formDocument.Form);
            
            Dictionary<string, Link> expectedLinks = new()
            {
                [IanaLinkRelationConstants.Self] = Link.SelfLink(new Uri("/" + _createFormPath, UriKind.Relative))
            };
            
            AssertThat.Links(formDocument.Links).Presents(expectedLinks);
        }
        
        public void FailsWith(HttpStatusCode httpStatusCode)
        {
            HttpResponseMessage httpResponseMessage = _testFixture.HttpClient.GetAsync(_createFormPath).Result;

            httpResponseMessage.AssertFailure(httpStatusCode);
        }
    }
}
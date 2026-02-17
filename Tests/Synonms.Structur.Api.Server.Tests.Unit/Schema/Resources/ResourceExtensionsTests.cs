using NSubstitute;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema.Forms;
using Synonms.Structur.Api.Server.Lookups;
using Synonms.Structur.Api.Server.Schema.Resources;
using Synonms.Structur.Api.Server.Tests.Unit.Shared;
using Xunit;

namespace Synonms.Structur.Api.Server.Tests.Unit.Schema.Resources;

public class ResourceExtensionsTests
{
    private readonly ILookupOptionsProvider _mockLookupOptionsProvider = Substitute.For<ILookupOptionsProvider>();
    
    public ResourceExtensionsTests()
    {
        _mockLookupOptionsProvider
            .Get(Arg.Any<string>())
            .Returns(Enumerable.Empty<FormFieldOption>());
    }

    [Fact]
    public void GenerateCreateForm_AddsFormFieldsAndSetsTargetLink()
    {
        TestResource resource = new();
        Uri targetUri = new("tests", UriKind.Relative);
        
        Form createForm = resource.GenerateCreateForm<TestAggregateRoot, TestResource>(targetUri, _mockLookupOptionsProvider);

        Assert.Equal(11, createForm.Fields.Count());

        Assert.Equal(targetUri, createForm.Target.Uri);
        Assert.Equal(IanaLinkRelationConstants.Forms.Create, createForm.Target.Relation);
        Assert.Equal(IanaHttpMethodConstants.Post, createForm.Target.Method);
    }
    
    [Fact]
    public void GenerateEditForm_AddsFormFieldsAndSetsTargetLink()
    {
        TestResource resource = new();
        Uri targetUri = new("tests", UriKind.Relative);
        
        Form editForm = resource.GenerateEditForm<TestAggregateRoot, TestResource>(targetUri, _mockLookupOptionsProvider);

        Assert.Equal(11, editForm.Fields.Count());

        Assert.Equal(targetUri, editForm.Target.Uri);
        Assert.Equal(IanaLinkRelationConstants.Forms.Edit, editForm.Target.Relation);
        Assert.Equal(IanaHttpMethodConstants.Put, editForm.Target.Method);
    }
}
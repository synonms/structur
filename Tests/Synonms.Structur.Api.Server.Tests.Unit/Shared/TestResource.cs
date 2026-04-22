using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;

namespace Synonms.Structur.Api.Server.Tests.Unit.Shared;

public class TestResource : Resource
{
    public TestResource()
    {
    }

    public TestResource(Guid id, Link selfLink) : base(id, selfLink)
    {
    }
    
    public override string GetCollectionPath() => "tests";

    public Guid? SomeRelatedAggregateId { get; set; }

    public string? SomeString { get; set; }
    
    public int? SomeInt { get; set; }

    public bool? SomeBool { get; set; }

    public List<string>? SomeList { get; set; }

    public DateOnly? SomeDate { get; set; }
    
    public DateTime? SomeDateTime { get; set; }
    
    public TestChildResource? ChildResource { get; set; }
    
    public List<TestChildResource>? ChildResources { get; set; }
}

public class TestChildResource : ChildResource
{
    public TestChildResource()
    {
    }

    public TestChildResource(Guid id) : base(id)
    {
    }
    
    public string? Property1 { get; set; }

    public int? Property2 { get; set; }
}
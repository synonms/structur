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

    public string? SomeString { get; set; }
    
    public int? SomeInt { get; set; }

    public decimal? SomeDecimal { get; set; }

    public bool? SomeBool { get; set; }

    public string[]? SomeArray { get; set; }
    
    public IEnumerable<string>? SomeEnumerable { get; set; }

    public DateOnly? SomeDate { get; set; }
    
    public TimeOnly? SomeTime { get; set; }
    
    public DateTime? SomeDateTime { get; set; }
    
    public TimeSpan? SomeTimeSpan { get; set; }
    
    public IEnumerable<TestChildResource>? ChildResources { get; set; }
}

public class TestChildResource : ChildResource
{
    public string Property1 { get; set; } = string.Empty;

    public int Property2 { get; set; }
}
using Synonms.Structur.Application.Schema.Resources;

namespace Synonms.Structur.Application.Tests.Unit.Shared;

internal class TestResource : Resource
{
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

internal class TestChildResource : ChildResource
{
    public string Property1 { get; set; } = string.Empty;

    public int Property2 { get; set; }
}
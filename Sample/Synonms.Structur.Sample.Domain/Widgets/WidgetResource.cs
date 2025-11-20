using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Schema.Resources;

namespace Synonms.Structur.Sample.Domain.Widgets;

public class WidgetResource : Resource
{
    public WidgetResource()
    {
    }

    public WidgetResource(Guid id, Link selfLink)
        : base(id, selfLink)
    {
    }

    [StructurRequired]
    [StructurMaxLength(Widget.NameMaxLength)]
    public string Name { get; set; } = string.Empty;
}
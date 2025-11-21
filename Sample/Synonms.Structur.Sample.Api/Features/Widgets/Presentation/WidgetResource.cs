using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Sample.Api.Features.Widgets.Domain;

namespace Synonms.Structur.Sample.Api.Features.Widgets.Presentation;

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
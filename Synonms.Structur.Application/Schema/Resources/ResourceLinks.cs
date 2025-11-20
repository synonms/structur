namespace Synonms.Structur.Application.Schema.Resources;

public class ResourceLinks : Dictionary<string, Link>
{
    public ResourceLinks()
    {
    }
    
    public ResourceLinks(IDictionary<string, Link> links) : base(links)
    {
    }
}
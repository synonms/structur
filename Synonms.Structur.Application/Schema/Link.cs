using Synonms.Structur.Application.Iana;

namespace Synonms.Structur.Application.Schema;

public class Link
{
    public Link(Uri uri, string relation, string method)
    {
        Uri = uri;
        Relation = relation;
        Method = method;
    }

    public Uri Uri { get; }
    
    public string Relation { get; }
    
    public string Method { get; }
    
    public string[]? Accepts { get; init; }

    public static Link CollectionLink(Uri uri) =>
        new (uri, IanaLinkRelationConstants.Collection, IanaHttpMethodConstants.Get);

    public static Link CreateFormLink(Uri uri) => 
        new (uri, IanaLinkRelationConstants.Forms.Create, IanaHttpMethodConstants.Get);
    
    public static Link CreateFormTargetLink(Uri uri) =>
        new (uri, IanaLinkRelationConstants.Forms.Create, IanaHttpMethodConstants.Post);

    public static Link EditFormLink(Uri uri) => 
        new (uri, IanaLinkRelationConstants.Forms.Edit, IanaHttpMethodConstants.Get);
    
    public static Link EditFormTargetLink(Uri uri) =>
        new (uri, IanaLinkRelationConstants.Forms.Edit, IanaHttpMethodConstants.Put);

    public static Link EmptyLink() =>
        new (new Uri("http://localhost"), string.Empty, string.Empty);

    public static Link ItemLink(Uri uri) =>
        new (uri, IanaLinkRelationConstants.Item, IanaHttpMethodConstants.Get);

    public static Link DeleteSelfLink(Uri uri) =>
        new (uri, IanaLinkRelationConstants.Self, IanaHttpMethodConstants.Delete);

    public static Link PageLink(Uri uri) =>
        new (uri, IanaLinkRelationConstants.Collection, IanaHttpMethodConstants.Get);
    
    public static Link RelationLink(Uri uri) =>
        new (uri, IanaLinkRelationConstants.Related, IanaHttpMethodConstants.Get);
    
    public static Link SelfLink(Uri uri) =>
        new (uri, IanaLinkRelationConstants.Self, IanaHttpMethodConstants.Get);
}
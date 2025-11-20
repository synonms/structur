namespace Synonms.Structur.Application.Iana;

/// <summary>
/// https://www.iana.org/assignments/link-relations/link-relations.xhtml
/// </summary>
public static class IanaLinkRelationConstants
{
    public const string Collection = "collection";
    public const string Edit = "edit";  // TODO: Use this for target URI of forms? 
    public const string Item = "item";
    public const string Related = "related";
    public const string Search = "search";
    public const string Self = "self";
    public const string Service = "service";
    public const string Status = "status";

    public static class Forms
    {
        public const string Create = "create-form";
        public const string Edit = "edit-form";
    }

    public static class Pagination
    {
        public const string First = "first";
        public const string Last = "last";
        public const string Next = "next";
        public const string Previous = "previous";
    }
}
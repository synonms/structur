using System.Reflection;
using Synonms.Structur.Api.Core.Schema.Forms;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Lookups;
using Synonms.Structur.Api.Server.Schema.Resources;

namespace Synonms.Structur.Api.Server.Schema.Forms;

public static class ObjectExtensions
{
    private static readonly string[] FormsIgnorePropertyNames =
    {
        "Id",
        "SupportedVersions",
        "SelfLink",
        "Links"
    };

    public static IEnumerable<FormField> GetFormFields(this object instance, ILookupOptionsProvider lookupOptionsProvider) =>
        instance.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x =>
                FormsIgnorePropertyNames.Contains(x.Name) is false
                && x.PropertyType.IsForRelatedEntityCollectionLink() is false)
            .Select(propertyInfo => propertyInfo.CreateFormField(instance, lookupOptionsProvider));
}

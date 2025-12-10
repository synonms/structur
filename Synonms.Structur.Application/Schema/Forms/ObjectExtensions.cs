using System.Reflection;
using Synonms.Structur.Application.Lookups;
using Synonms.Structur.Application.Schema.Resources;

namespace Synonms.Structur.Application.Schema.Forms;

public static class ObjectExtensions
{
    private static readonly string[] FormsIgnorePropertyNames =
    {
        nameof(Resource.Id),
/*        nameof(Resource.IsDeleted),
        nameof(Resource.CreatedAt),
        nameof(Resource.UpdatedAt),*/
        nameof(Resource.SelfLink),
        nameof(Resource.Links)
    };
    
    public static IEnumerable<FormField> GetFormFields(this object instance, ILookupOptionsProvider lookupOptionsProvider) =>
        instance.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => 
                FormsIgnorePropertyNames.Contains(x.Name) is false 
                && x.PropertyType.IsForRelatedEntityCollectionLink() is false
                && x.PropertyType.IsForEmbeddedResource() is false
                && x.PropertyType.IsForEmbeddedResourceCollection() is false)
            .Select(propertyInfo => propertyInfo.CreateFormField(instance, lookupOptionsProvider));
}
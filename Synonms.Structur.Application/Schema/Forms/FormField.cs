namespace Synonms.Structur.Application.Schema.Forms;

public class FormField
{
    public FormField(string name, Type valueType, Type? elementValueType = null)
    {
        Name = name;
        ValueType = valueType;
        ElementValueType = elementValueType;
    }

    /// <summary>
    /// Description of the field that may be used to enhance usability, for example, as a tool tip.
    /// Optional.
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// A Form object that reflects the required object structure of each element in a collection.
    /// </summary>
    public IEnumerable<FormField>? ElementForm { get; set; }

    /// <summary>
    /// Specifies the mandatory data type that each element in a collection must adhere to.
    /// Optional - null infers type of `string`.
    /// </summary>
    public string? ElementType { get; init; }
    
    /// <summary>
    /// A Form object that reflects the required object structure of the Field value.
    /// This allows content authors to define complex data/content graphs that may be submitted to a single linked resource location.
    /// Required for nested resources only.
    /// </summary>
    public IEnumerable<FormField>? Form { get; set; }
    
    /// <summary>
    /// Indicates whether or not the field value may be modified or submitted to a linked resource location.
    /// Optional - null infers a value of `true`.
    /// </summary>
    public bool? IsEnabled { get; init; }

    /// <summary>
    /// Indicates whether or not the field value may be modified before it is submitted to the form’s linked resource location.
    /// Optional - null infers a value of `true`.
    /// </summary>
    public bool? IsMutable { get; init; }

    /// <summary>
    /// Indicates whether or not the field value may equal null before is submitted to the form’s linked resource location.
    /// Optional - null infers a value of `false`.
    /// </summary>
    public bool? IsRequired { get; init; }
    
    /// <summary>
    /// Indicates whether or not the field value is considered sensitive information and should be kept secret.
    /// Optional - null infers a value of `false`.
    /// </summary>
    public bool? IsSecret { get; init; }

    /// <summary>
    /// Indicates whether or not the field should be made visible by a user agent.
    /// Fields that are not visible are usually used to retain a default value that must be submitted to the form’s linked resource location.
    /// Optional - null infers a value of 'true'.
    /// </summary>
    public bool? IsVisible { get; init; }

    /// <summary>
    /// A human-readable string that may be used to enhance usability.
    /// Optional.
    /// </summary>
    public string? Label { get; init; }
    
    /// <summary>
    /// Indicates that the field value must be less than or equal to the specified max value.
    /// Optional - generally only applicable to numeric and temporal fields.
    /// </summary>
    public object? Max { get; init; }
    
    /// <summary>
    /// A non-negative integer that specifies the maximum number of characters the field value may contain.
    /// Optional - generally only applicable to string fields.
    /// </summary>
    public int? MaxLength { get; init; }
    
    /// <summary>
    /// A non-negative integer that specifies the maximum number of field values that may be submitted when the field type value is an array.
    /// Optional - generally only applicable to array fields.
    /// </summary>
    public int? MaxSize { get; init; }
    
    /// <summary>
    /// Indicates that the field value must be more than or equal to the specified min value.
    /// Optional - generally only applicable to numeric and temporal fields.
    /// </summary>
    public object? Min { get; init; }
    
    /// <summary>
    /// A non-negative integer that specifies the minimum number of characters the field value may contain.
    /// Optional - generally only applicable to string fields.
    /// </summary>
    public int? MinLength { get; init; }
    
    /// <summary>
    /// A non-negative integer that specifies the minimum number of field values that may be submitted when the field type value is an array.
    /// Optional - generally only applicable to array fields.
    /// </summary>
    public int? MinSize { get; init; }
    
    /// <summary>
    /// A string name assigned to the field.
    /// Mandatory.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Defines a set of options which constrain the field value.
    /// Optional - generally used to generate dropdown lists.
    /// </summary>
    public IEnumerable<FormFieldOption>? Options { get; init; }
    
    /// <summary>
    /// A regular expression used to validate the field value.
    /// Optional
    /// </summary>
    public string? Pattern { get; init; }
    
    /// <summary>
    /// A short hint string that describes the expected field value.
    /// Optional.
    /// </summary>
    public string? Placeholder { get; init; }
    
    /// <summary>
    /// Specifies the mandatory data type that the value member value must adhere to.
    /// Optional - null infers type of `string`.
    /// </summary>
    public string? Type { get; init; }
    
    /// <summary>
    /// Reflects the value assigned to the field, e.g. for existing resources or forms with pre-populated/default values.
    /// Optional.
    /// </summary>
    public object? Value { get; init; }
    
    public Type ValueType { get; }

    public Type? ElementValueType { get; }
}
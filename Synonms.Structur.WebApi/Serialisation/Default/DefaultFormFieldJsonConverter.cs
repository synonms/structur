using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Application.Schema.Forms;

namespace Synonms.Structur.WebApi.Serialisation.Default;

public class DefaultFormFieldJsonConverter : JsonConverter<FormField>
{
    public override FormField? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        string? name = jsonDocument.RootElement.GetProperty(DefaultPropertyNames.FormFields.Name).GetString();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new JsonException("Unable to determine FormField name.");
        }

        string? type = jsonDocument.RootElement.TryGetProperty(DefaultPropertyNames.FormFields.Type, out JsonElement typeElement) ? typeElement.GetString() : null;
        Type? valueType = type.GetFormFieldValueType();

        if (valueType is null)
        {
            throw new JsonException("Unable to determine FormField value type.");
        }
        
        string? elementType = jsonDocument.RootElement.GetOptionalString(DefaultPropertyNames.FormFields.ElementType);
        Type? elementValueType = elementType.GetFormFieldValueType();

        if (valueType == typeof(IEnumerable<>))
        {
            if (elementValueType is null)
            {
                throw new JsonException("Unable to determine FormField element value type for IEnumerable field.");
            }

            valueType = valueType.MakeGenericType(elementValueType);
        }
        
        string? description = jsonDocument.RootElement.GetOptionalString(DefaultPropertyNames.FormFields.Description);
        string? label = jsonDocument.RootElement.GetOptionalString(DefaultPropertyNames.FormFields.Label);
        string? pattern = jsonDocument.RootElement.GetOptionalString(DefaultPropertyNames.FormFields.Pattern);
        string? placeholder = jsonDocument.RootElement.GetOptionalString(DefaultPropertyNames.FormFields.Placeholder);
        bool? isEnabled = jsonDocument.RootElement.GetOptionalBool(DefaultPropertyNames.FormFields.Enabled);
        bool? isMutable = jsonDocument.RootElement.GetOptionalBool(DefaultPropertyNames.FormFields.Mutable);
        bool? isRequired = jsonDocument.RootElement.GetOptionalBool(DefaultPropertyNames.FormFields.Required);
        bool? isSecret = jsonDocument.RootElement.GetOptionalBool(DefaultPropertyNames.FormFields.Secret);
        bool? isVisible = jsonDocument.RootElement.GetOptionalBool(DefaultPropertyNames.FormFields.Visible);
        int? maxLength = jsonDocument.RootElement.GetOptionalInt(DefaultPropertyNames.FormFields.MaxLength);
        int? maxSize = jsonDocument.RootElement.GetOptionalInt(DefaultPropertyNames.FormFields.MaxSize);
        int? minLength = jsonDocument.RootElement.GetOptionalInt(DefaultPropertyNames.FormFields.MinLength);
        int? minSize = jsonDocument.RootElement.GetOptionalInt(DefaultPropertyNames.FormFields.MinSize);
        object? max = jsonDocument.RootElement.GetOptionalValue(DefaultPropertyNames.FormFields.Max, valueType); 
        object? min = jsonDocument.RootElement.GetOptionalValue(DefaultPropertyNames.FormFields.Min, valueType); 
        object? value = jsonDocument.RootElement.GetOptionalValue(DefaultPropertyNames.FormFields.Value, valueType);
        IEnumerable<FormField>? elementForm = TryGetForm(jsonDocument.RootElement, DefaultPropertyNames.FormFields.ElementForm, options); 
        IEnumerable<FormField>? form = TryGetForm(jsonDocument.RootElement, DefaultPropertyNames.FormFields.Form, options);
        IEnumerable<FormFieldOption>? formFieldOptions = TryGetOptions(jsonDocument.RootElement, valueType);
        
        FormField formField = new(name, valueType, elementValueType)
        {
            ElementType = elementType,
            ElementForm = elementForm,
            Form = form,
            Description = description,
            Label = label,
            Pattern = pattern,
            Placeholder = placeholder,
            IsEnabled = isEnabled,
            IsMutable = isMutable,
            IsRequired = isRequired,
            IsSecret = isSecret,
            IsVisible = isVisible,
            Max = max,
            MaxLength = maxLength,
            MaxSize = maxSize,
            Min = min,
            MinLength = minLength,
            MinSize = minSize,
            Type = type,
            Value = value,
            Options = formFieldOptions
        };
        
        return formField;
    }

    public override void Write(Utf8JsonWriter writer, FormField value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString(DefaultPropertyNames.FormFields.Name, value.Name);
        writer.WriteString(DefaultPropertyNames.FormFields.Type, value.Type ?? DataTypes.String);

        if (string.IsNullOrEmpty(value.ElementType) is false)
        {
            writer.WriteString(DefaultPropertyNames.FormFields.ElementType, value.ElementType);
        }

        if (value.ElementForm is not null)
        {
            writer.WritePropertyName(DefaultPropertyNames.FormFields.ElementForm);
            writer.WriteStartObject();
            writer.WritePropertyName(DefaultPropertyNames.Value);
            JsonSerializer.Serialize(writer, value.ElementForm, options);
            writer.WriteEndObject();
        }

        if (value.Form is not null)
        {
            writer.WritePropertyName(DefaultPropertyNames.FormFields.Form);
            writer.WriteStartObject();
            writer.WritePropertyName(DefaultPropertyNames.Value);
            JsonSerializer.Serialize(writer, value.Form, options);
            writer.WriteEndObject();
        }

        if (string.IsNullOrEmpty(value.Description) is false)
        {
            writer.WriteString(DefaultPropertyNames.FormFields.Description, value.Description);
        }

        if (value.IsEnabled is not null)
        {
            writer.WriteBoolean(DefaultPropertyNames.FormFields.Enabled, value.IsEnabled ?? true);
        }

        if (value.IsMutable is not null)
        {
            writer.WriteBoolean(DefaultPropertyNames.FormFields.Mutable, value.IsMutable ?? true);
        }

        if (value.IsRequired is not null)
        {
            writer.WriteBoolean(DefaultPropertyNames.FormFields.Required, value.IsRequired ?? false);
        }

        if (value.IsSecret is not null)
        {
            writer.WriteBoolean(DefaultPropertyNames.FormFields.Secret, value.IsSecret ?? false);
        }

        if (value.IsVisible is not null)
        {
            writer.WriteBoolean(DefaultPropertyNames.FormFields.Visible, value.IsVisible ?? true);
        }

        if (string.IsNullOrEmpty(value.Label) is false)
        {
            writer.WriteString(DefaultPropertyNames.FormFields.Label, value.Label);
        }

        if (value.Max is not null && value.Max.GetType().IsAssignableTo(value.ValueType))
        {
            writer.WritePropertyName(DefaultPropertyNames.FormFields.Max);
            JsonSerializer.Serialize(writer, value.Max, value.ValueType, options);
        }

        if (value.MaxLength is not null && value.MaxLength > 0)
        {
            writer.WriteNumber(DefaultPropertyNames.FormFields.MaxLength, value.MaxLength.Value);
        }

        if (value.MaxSize is not null && value.MaxSize > 0)
        {
            writer.WriteNumber(DefaultPropertyNames.FormFields.MaxSize, value.MaxSize.Value);
        }

        if (value.Min is not null && value.Min.GetType().IsAssignableTo(value.ValueType))
        {
            writer.WritePropertyName(DefaultPropertyNames.FormFields.Min);
            JsonSerializer.Serialize(writer, value.Min, value.ValueType, options);
        }

        if (value.MinLength is not null && value.MinLength > 0)
        {
            writer.WriteNumber(DefaultPropertyNames.FormFields.MinLength, value.MinLength.Value);
        }

        if (value.MinSize is not null && value.MinSize > 0)
        {
            writer.WriteNumber(DefaultPropertyNames.FormFields.MinSize, value.MinSize.Value);
        }

        if (string.IsNullOrEmpty(value.Pattern) is false)
        {
            writer.WriteString(DefaultPropertyNames.FormFields.Pattern, value.Pattern);
        }

        if (string.IsNullOrEmpty(value.Placeholder) is false)
        {
            writer.WriteString(DefaultPropertyNames.FormFields.Placeholder, value.Placeholder);
        }

        if (value.Value is not null && value.Value.GetType().IsAssignableTo(value.ValueType))
        {
            writer.WritePropertyName(DefaultPropertyNames.FormFields.Value);
            JsonSerializer.Serialize(writer, value.Value, value.ValueType, options);
        }

        if (value.Options is not null)
        {
            writer.WritePropertyName(DefaultPropertyNames.FormFields.Options);
            writer.WriteStartObject();
            writer.WritePropertyName(DefaultPropertyNames.Value);
            JsonSerializer.Serialize(writer, value.Options, options);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static IEnumerable<FormField>? TryGetForm(JsonElement rootElement, string formName, JsonSerializerOptions options)
    {
        if (rootElement.TryGetProperty(formName, out JsonElement formElement))
        {
            if (formElement.TryGetProperty(DefaultPropertyNames.Value, out JsonElement valueElement))
            {
                return JsonSerializer.Deserialize<IEnumerable<FormField>>(valueElement.ToString(), options);
            }
        }

        return null; 
    }

    private static IEnumerable<FormFieldOption>? TryGetOptions(JsonElement rootElement, Type valueType)
    {
        if (rootElement.TryGetProperty(DefaultPropertyNames.FormFields.Options, out JsonElement formFieldOptionsElement))
        {
            List<FormFieldOption> formFieldOptions = new();
            
            foreach (JsonElement formFieldOptionElement in formFieldOptionsElement.GetProperty(DefaultPropertyNames.Value).EnumerateArray())
            {
                object? optionValue = formFieldOptionElement.GetOptionalValue(DefaultPropertyNames.Value, valueType);

                if (optionValue is null)
                {
                    throw new JsonException($"Unable to determine FormFieldOption value for type [{valueType}] in element '{formFieldOptionElement.ToString()}'.");
                }
                
                string? optionLabel = formFieldOptionElement.GetOptionalString(DefaultPropertyNames.FormFields.Label);
                bool? isOptionEnabled = formFieldOptionElement.GetOptionalBool(DefaultPropertyNames.FormFields.Enabled);

                FormFieldOption formFieldOption = new(optionValue)
                {
                    Label = optionLabel,
                    IsEnabled = isOptionEnabled ?? true
                };
                
                formFieldOptions.Add(formFieldOption);
            }

            return formFieldOptions;
        }

        return null;
    }
}
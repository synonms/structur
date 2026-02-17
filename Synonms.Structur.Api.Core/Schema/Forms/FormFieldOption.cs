namespace Synonms.Structur.Api.Core.Schema.Forms;

public class FormFieldOption
{
    public FormFieldOption(object value)
    {
        Value = value;
    }

    public bool IsEnabled { get; init; } = true;

    public string? Label { get; init; }

    public object Value { get; init; }
}
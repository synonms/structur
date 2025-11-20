namespace Synonms.Structur.Application.Schema.Errors;

public class Error
{
    public Error(Guid id, string code, string title, string detail, ErrorSource source)
    {
        Id = id;
        Code = code;
        Title = title;
        Detail = detail;
        Source = source;
    }

    public Guid Id { get; }

    public string Code { get; }

    public string Title { get; }

    public string Detail { get; }

    public ErrorSource Source { get; }
}
namespace Synonms.Structur.Core.Faults;

public class Fault
{
    public Fault(string code, string title, string detail, FaultSource source, params object?[] arguments)
    {
        Id = Guid.NewGuid();
        Code = code;
        Title = title;
        Detail = detail;
        Source = source;
        Arguments = arguments;
    }

    public Guid Id { get; }

    public string Code { get; }

    public string Title { get; }

    public string Detail { get; }

    public FaultSource Source { get; }
        
    public object?[] Arguments { get; }

    public override string ToString() =>
        GetPlaceholders().Aggregate(Detail, (current, parameter) => current.Replace(parameter.Key, parameter.Value?.ToString() ?? string.Empty));

    internal Dictionary<string, object?> GetPlaceholders()
    {
        bool isPlaceholderOpen = false;
        string currentPlaceholder = string.Empty;
        List<string> completedPlaceholders = [];

        foreach (char c in Detail)
        {
            if (c == '{')
            {
                isPlaceholderOpen = true;
                continue;
            }
            
            if (c == '}')
            {
                isPlaceholderOpen = false;
                if (!string.IsNullOrWhiteSpace(currentPlaceholder))
                {
                    // TODO: Check for dodgy characters
                    completedPlaceholders.Add("{" + currentPlaceholder + "}");
                    currentPlaceholder = string.Empty;
                }
                continue;
            }

            if (isPlaceholderOpen)
            {
                currentPlaceholder += c;
            }
        }

        if (completedPlaceholders.Count != Arguments.Length)
        {
            throw new InvalidOperationException($"Number of placeholders ({completedPlaceholders.Count}) does not match number of supplied arguments ({Arguments.Length}).");
        }
        
        Dictionary<string, object?> result = new();
        
        for(int x = 0; x < completedPlaceholders.Count; x++)
        {
            result.Add(completedPlaceholders[x], Arguments[x]);
        }

        return result;
    }
}
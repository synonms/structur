namespace Synonms.Structur.Application.Pipeline;

public class QueryParameters
{
    public static class Names
    {
        public const string Offset = "offset";
        public const string Sort = "sort";
        public const string Limit = "limit";
    }

    public static readonly string[] ReservedKeys = [ Names.Offset, Names.Sort, Names.Limit ];

    private readonly IDictionary<string, object> _parameters = new Dictionary<string, object>();

    public QueryParameters()
    {
    }

    public QueryParameters(IDictionary<string, object> parameters)
    {
        _parameters = parameters;
    }

    public object this[string key]
    {
        get => _parameters[key];
        set => _parameters[key] = value;
    }
    
    public void Add(string key, object value) =>
        _parameters.Add(key, value);

    public bool Any() =>
        _parameters.Any();

    public IReadOnlyDictionary<string, object> GetAll() =>
        _parameters.AsReadOnly();

    public IReadOnlyDictionary<string, object> GetFiltersOnly() =>
        _parameters
            .Where(kvp => ReservedKeys.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase) is false)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value); 

    public object? Get(string key) =>
        _parameters.TryGetValue(key, out object? value) ? value : null;
    
    public bool TryGetAs<T>(string key, out T? outValue)
    {
        outValue = default(T);

        if (_parameters.TryGetValue(key, out object? value) is false)
        {
            return false;
        }

        if (value is not T valueAsT)
        {
            return false;
        }
        
        outValue = valueAsT;
        return true;
    }

    public string ToQueryString() =>
        "?" + string.Join('&', _parameters.Select(x => x.Key + "=" + x.Value));
}
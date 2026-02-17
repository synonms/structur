namespace Synonms.Structur.Core.Collections;

public class BiDirectionalLookup<TLeft, TRight>
    where TLeft : notnull
    where TRight : notnull
{
    private readonly Dictionary<TLeft, TRight> _leftToRightMapping = new();
    private readonly Dictionary<TRight, TLeft> _rightToLeftMapping = new();

    public BiDirectionalLookup()
    {
    }
    
    public BiDirectionalLookup(IEnumerable<KeyValuePair<TLeft, TRight>> collection)
    {
        foreach ((TLeft left, TRight right) in collection)
        {
            Add(left, right);
        }
    }
    
    public void Add(TLeft left, TRight right)
    {
        if (_leftToRightMapping.ContainsKey(left))
        {
            throw new ArgumentException("Duplicate left key");
        }

        if (_rightToLeftMapping.ContainsKey(right))
        {
            throw new ArgumentException("Duplicate right key");
        }

        _leftToRightMapping.Add(left, right);
        _rightToLeftMapping.Add(right, left);
    }

    public bool TryGetByFirst(TLeft first, out TRight second) =>
        _leftToRightMapping.TryGetValue(first, out second);

    public bool TryGetBySecond(TRight second, out TLeft first) =>
        _rightToLeftMapping.TryGetValue(second, out first);
}
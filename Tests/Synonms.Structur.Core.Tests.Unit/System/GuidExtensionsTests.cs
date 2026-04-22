using Synonms.Structur.Core.System;
using Xunit;

namespace Synonms.Structur.Core.Tests.Unit.System;

public class GuidExtensionsTests
{
    private const int NoOfUnchangedCharacters = 24;

    [Fact]
    public void ToComb_NewGuid_ReturnsNewGuidWithExpectedUnchangedCharacters()
    {
        Guid originalGuid = Guid.NewGuid();

        Guid combGuid = originalGuid.ToComb();
        
        Assert.NotEqual(combGuid, originalGuid);
        Assert.Equal(combGuid.ToString()[..NoOfUnchangedCharacters], originalGuid.ToString()[..NoOfUnchangedCharacters]);
    }

    [Fact]
    public void ToComb_EmptyGuid_ReturnsNewGuidWithExpectedUnchangedCharacters()
    {
        Guid originalGuid = Guid.Empty;

        Guid combGuid = originalGuid.ToComb();
        
        Assert.NotEqual(combGuid, originalGuid);
        Assert.Equal(combGuid.ToString()[..NoOfUnchangedCharacters], originalGuid.ToString()[..NoOfUnchangedCharacters]);
    }
    
    [Fact]
    public async Task ToComb_ConsecutiveCalls_GeneratesSequentialGuids()
    {
        const int numberOfIds = 100;

        Dictionary<int, string> generatedIds = new (numberOfIds);

        for (int i = 0; i < numberOfIds; i++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(3), TestContext.Current.CancellationToken);
            generatedIds.Add(i, Guid.NewGuid().ToComb().ToString().Substring(NoOfUnchangedCharacters));
        }

        List<string> sortedIds = generatedIds.Values.OrderBy(x => x).ToList();

        Assert.Equal(numberOfIds, sortedIds.Count);
        
        for (int i = 0; i < numberOfIds; i++)
        {
            Assert.Equal(sortedIds[i], generatedIds[i]);
        }
    }
}
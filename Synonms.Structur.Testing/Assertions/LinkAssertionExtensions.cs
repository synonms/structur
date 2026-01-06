using System.Text.Json;
using Synonms.Structur.Application.Schema;
using Xunit;

namespace Synonms.Structur.Testing.Assertions;

public static class LinksAssertionExtensions
{
    public static void Presents(this IReadOnlyDictionary<string, Link> actualLinks, IReadOnlyDictionary<string, Link> expectedLinks)
    {
        foreach ((string expectedKey, Link expectedLink) in expectedLinks)
        {
            if (actualLinks.ContainsKey(expectedKey) is false)
            {
                Assert.Fail($"Unable to find link '{expectedKey}' in collection [{JsonSerializer.Serialize(actualLinks)}].");
                return;
            }
            
            Link actualLink = actualLinks[expectedKey];

            Assert.Equal(expectedLink.Uri, actualLink.Uri);
            Assert.Equal(expectedLink.Relation, actualLink.Relation);
            Assert.Equal(expectedLink.Method, actualLink.Method);
            Assert.Equal(expectedLink.Accepts, actualLink.Accepts);
        }

        foreach (string actualKey in actualLinks.Keys)
        {
            Assert.True(expectedLinks.ContainsKey(actualKey), $"Unexpected link '{actualKey}' found in collection [{actualLinks}].");
        }
    }
}
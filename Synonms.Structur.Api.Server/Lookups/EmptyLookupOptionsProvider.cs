using Synonms.Structur.Api.Core.Schema.Forms;

namespace Synonms.Structur.Api.Server.Lookups;

public class EmptyLookupOptionsProvider : ILookupOptionsProvider
{
    public IEnumerable<FormFieldOption> Get(string discriminator) => 
        [];
}
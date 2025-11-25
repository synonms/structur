using Synonms.Structur.Application.Schema.Forms;

namespace Synonms.Structur.Application.Lookups;

public class EmptyLookupOptionsProvider : ILookupOptionsProvider
{
    public IEnumerable<FormFieldOption> Get(string discriminator) => 
        [];
}
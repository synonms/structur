using Synonms.Structur.Application.Schema.Forms;

namespace Synonms.Structur.Application.Lookups;

public interface ILookupOptionsProvider
{
    IEnumerable<FormFieldOption> Get(string discriminator);
}
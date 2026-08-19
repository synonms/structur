using Synonms.Structur.Api.Core.Schema.Forms;

namespace Synonms.Structur.Api.Server.Lookups;

public interface ILookupOptionsProvider
{
    IEnumerable<FormFieldOption> Get(string discriminator);
}
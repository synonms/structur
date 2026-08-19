using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Errors;
using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Api.Server.Schema.Errors;

public interface IErrorCollectionDocumentFactory
{
    ErrorCollectionDocument Create(Fault fault, Link requestedDocumentLink);
}
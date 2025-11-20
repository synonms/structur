using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Application.Schema.Errors;

public interface IErrorCollectionDocumentFactory
{
    ErrorCollectionDocument Create(Fault fault, Link requestedDocumentLink);
}
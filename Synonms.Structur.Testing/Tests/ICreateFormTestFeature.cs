using Synonms.Structur.Api.Core.Schema.Forms;

namespace Synonms.Structur.Testing.Tests;

public interface ICreateFormTestFeature
{
    string CollectionPath { get; }

    void ValidateCreateForm(Form form);
}
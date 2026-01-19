using Synonms.Structur.Application.Schema.Forms;

namespace Synonms.Structur.Testing.Tests;

public interface ICreateFormTestFeature
{
    string CollectionPath { get; }

    void ValidateCreateForm(Form form);
}
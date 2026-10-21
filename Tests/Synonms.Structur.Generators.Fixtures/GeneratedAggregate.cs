using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Persistence;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Generators.Fixtures;

[GenerateStorage("generator-fixture", 1)]
public sealed partial class GeneratedAggregate : AggregateRoot<GeneratedAggregate>
{
    public GeneratedAggregate() : base(EntityId<GeneratedAggregate>.New(), Guid.NewGuid(), UserAction.Rehydrate(new UserActionDto
    {
        ActionAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        ActionById = Guid.Empty,
        ActionByName = ""
    }))
    {
        Name = Moniker.Rehydrate("fixture");
        Children = new List<GeneratedChild> { new("child") };
        Contact = EmailContact.Rehydrate(new EmailContactData
        {
            Type = "Personal",
            Address = "person@example.test",
            IsPrimary = true
        });
        State = FixtureState.Enabled;
        Legacy = ManualValue.Create("handwritten");
        Optional = "stored";
    }

    [StoredState("display_name")]
    public Moniker Name { get; private set; }

    [StoredState("children")]
    public IReadOnlyList<GeneratedChild> Children { get; private set; }

    [StoredState("contact")]
    public EmailContact? Contact { get; private set; }

    [StoredState("optional")]
    [StorageNullPolicy(true, false)]
    public string? Optional { get; private set; }

    [StoredState("state")]
    [StorageOverride(typeof(string), typeof(FixtureStateCodec))]
    public FixtureState State { get; private set; }

    [StoredState("legacy")]
    [StorageOverride(typeof(ManualValueRecord), typeof(ManualValueCodec))]
    public ManualValue Legacy { get; private set; }

    [NotStored]
    public string Computed => Name.Value + Children.Count;
}

public enum FixtureState
{
    Disabled,
    Enabled
}

public sealed class FixtureStateCodec : IStorageValueCodec<FixtureState, string>
{
    public string Capture(FixtureState value) => value.ToString();

    public FixtureState Restore(string value) => Enum.Parse<FixtureState>(value);
}

public sealed class ManualValue
{
    private ManualValue(string value) => Value = value;

    public string Value { get; }

    public static ManualValue Create(string value) => new(value);
}

public sealed class ManualValueRecord
{
    public string Value { get; set; } = "";
}

public sealed class ManualValueCodec : IStorageValueCodec<ManualValue, ManualValueRecord>
{
    public ManualValueRecord Capture(ManualValue value) => new() { Value = value.Value };

    public ManualValue Restore(ManualValueRecord value) => ManualValue.Create(value.Value);
}

[GenerateStorage("generated-child", 1)]
public sealed partial class GeneratedChild : AggregateMember<GeneratedChild>
{
    public GeneratedChild(string name) : base(EntityId<GeneratedChild>.New())
    {
        Name = name;
    }

    [StoredState("label")]
    public string Name { get; private set; }

    [NotStored]
    public string Computed => Name.ToUpperInvariant();
}

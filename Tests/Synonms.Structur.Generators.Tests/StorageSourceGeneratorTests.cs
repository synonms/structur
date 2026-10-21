using System.Collections.Immutable;
using System.Reflection;
using MongoDB.Bson;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Persistence;
using Synonms.Structur.Generators;
using Synonms.Structur.Generators.Fixtures;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;
using Synonms.Structur.Infrastructure.MongoDb.Aggregates;

namespace Synonms.Structur.Generators.Tests;

public class StorageSourceGeneratorTests
{
    [Fact]
    public void GeneratedTypedBridgeCapturesAndRestoresRawNestedAndComplexState()
    {
        GeneratedAggregate original = new();
        GeneratedAggregateStorageMapper mapper = new();
        GeneratedAggregateStorageRecord captured = mapper.Capture(original);

        Assert.Equal(original.Id.Value, captured.Id);
        Assert.Equal("fixture", captured.Name);
        Assert.Equal("child", captured.Children[0].Name);
        Assert.Equal("person@example.test", captured.Contact!.Address);
        Assert.Equal("Enabled", captured.State);
        Assert.Equal("handwritten", captured.Legacy.Value);
        Assert.Equal(original.EntityTag.Value, captured.EntityTag);

        GeneratedAggregate restored = mapper.Restore(captured);
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.EntityTag, restored.EntityTag);
        Assert.Equal(original.Name.Value, restored.Name.Value);
        Assert.Equal(original.State, restored.State);
        Assert.Equal(original.Legacy.Value, restored.Legacy.Value);
        Assert.Equal(original.Children[0].Id, restored.Children[0].Id);
        Assert.Equal(original.Contact!.ToData().Type, restored.Contact!.ToData().Type);
        Assert.Equal(original.Contact.ToData().Address, restored.Contact.ToData().Address);
        Assert.Equal(original.Contact.ToData().IsPrimary, restored.Contact.ToData().IsPrimary);
        Assert.NotSame(captured.Children, mapper.Capture(restored).Children);
        Assert.NotSame(captured.Children[0], mapper.Capture(restored).Children[0]);

        MongoDatabaseConfiguration configuration = GeneratedAggregateStorageDescriptor.Register(
            "fixture", new Dictionary<Type, string> { [typeof(GeneratedAggregate)] = "generated" });
        MongoDbAggregateRecordMapper<GeneratedAggregate> bsonMapper = new(configuration);
        BsonDocument document = bsonMapper.Capture(original);
        Assert.Equal("fixture", document["display_name"].AsString);
        Assert.Equal("person@example.test", document["contact"]["Address"].AsString);
        Assert.Equal("Enabled", document["state"].AsString);
        Assert.Equal("handwritten", document["legacy"]["Value"].AsString);
        Assert.Equal(original.Id.Value, document["_id"].AsBsonBinaryData.ToGuid());
        Assert.Equal("stored", document["optional"].AsString);
        document.Remove("optional");
        GeneratedAggregate restoredFromMissing = bsonMapper.Restore(document);
        Assert.Equal(original.Id, restoredFromMissing.Id);
        Assert.Null(restoredFromMissing.Optional);
        document["optional"] = BsonNull.Value;
        Assert.Throws<InvalidOperationException>(() => bsonMapper.Restore(document));
    }

    [Theory]
    [InlineData("object", "STOR001")]
    [InlineData("unclassified", "STOR002")]
    [InlineData("duplicate-name", "STOR003")]
    [InlineData("non-partial", "STOR004")]
    [InlineData("missing-rehydrate", "STOR005")]
    [InlineData("unsupported-collection", "STOR006")]
    [InlineData("schema-conflict", "STOR007")]
    public void InvalidSchemasReportTheirStableDiagnostic(string scenario, string diagnosticId)
    {
        GeneratorRun run = Run(SourceFor(scenario));
        Assert.Contains(run.Diagnostics, diagnostic => diagnostic.Id == diagnosticId);
    }

    [Fact]
    public void GeneratedOutputHasStableHintsAndByteForByteDeterministicText()
    {
        GeneratorRun first = Run(SourceFor("valid"));
        GeneratorRun second = Run(SourceFor("valid"));

        Assert.DoesNotContain(first.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(first.GeneratedSources.Select(source => source.HintName), second.GeneratedSources.Select(source => source.HintName));
        Assert.Equal(first.GeneratedSources.Select(source => source.SourceText), second.GeneratedSources.Select(source => source.SourceText));
        Assert.All(first.GeneratedSources, source =>
        {
            Assert.DoesNotContain(Environment.CurrentDirectory, source.SourceText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DateTime.Now", source.SourceText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void UnrelatedSourceChangesDoNotAlterGeneratedStorageOutput()
    {
        GeneratorRun baseline = Run(SourceFor("valid"));
        GeneratorRun withUnrelatedDeclaration = Run(SourceFor("valid") +
            "\npublic sealed class Unrelated { public int Value => 1; }");

        Assert.Equal(baseline.GeneratedSources.Select(source => source.SourceText),
            withUnrelatedDeclaration.GeneratedSources.Select(source => source.SourceText));
    }

    private static string SourceFor(string scenario)
    {
        string attributes = scenario switch
        {
            "schema-conflict" => """
                [GenerateStorage("duplicate", 1)]
                public sealed partial class FirstRoot : AggregateRoot<FirstRoot>
                {
                    public FirstRoot() : base(new EntityId<FirstRoot>(Guid.Empty), Guid.Empty, UserAction.Empty) { }
                }
                [GenerateStorage("duplicate", 1)]
                public sealed partial class SecondRoot : AggregateRoot<SecondRoot>
                {
                    public SecondRoot() : base(new EntityId<SecondRoot>(Guid.Empty), Guid.Empty, UserAction.Empty) { }
                }
                """,
            "non-partial" => """
                [GenerateStorage("test", 1)]
                public sealed class TestRoot : AggregateRoot<TestRoot>
                {
                    public TestRoot() : base(new EntityId<TestRoot>(Guid.Empty), Guid.Empty, UserAction.Empty) { }
                    [StoredState("name")] public string Name { get; private set; } = "";
                }
                """,
            "missing-rehydrate" => """
                [GenerateStorage("test", 1)]
                public sealed partial class TestRoot : AggregateRoot<TestRoot>
                {
                    public TestRoot() : base(new EntityId<TestRoot>(Guid.Empty), Guid.Empty, UserAction.Empty) { }
                    [StoredState("value")] public Broken Value { get; private set; } = new();
                }
                public sealed class BrokenData { public string Text { get; set; } = ""; }
                public sealed class Broken : Synonms.Structur.Domain.ValueObjects.Abstractions.IComplexValueObject<Broken, BrokenData>
                {
                    public BrokenData ToData() => new();
                }
                """,
            "object" => """
                [GenerateStorage("test", 1)]
                public sealed partial class TestRoot : AggregateRoot<TestRoot>
                {
                    public TestRoot() : base(new EntityId<TestRoot>(Guid.Empty), Guid.Empty, UserAction.Empty) { }
                    [StoredState("value")] public object Value { get; private set; } = new();
                }
                """,
            "unclassified" => """
                [GenerateStorage("test", 1)]
                public sealed partial class TestRoot : AggregateRoot<TestRoot>
                {
                    public TestRoot() : base(new EntityId<TestRoot>(Guid.Empty), Guid.Empty, UserAction.Empty) { }
                    [StoredState("name")] public string Name { get; private set; } = "";
                    public string Computed { get; } = "";
                }
                """,
            "duplicate-name" => """
                [GenerateStorage("test", 1)]
                public sealed partial class TestRoot : AggregateRoot<TestRoot>
                {
                    public TestRoot() : base(new EntityId<TestRoot>(Guid.Empty), Guid.Empty, UserAction.Empty) { }
                    [StoredState("same")] public string First { get; private set; } = "";
                    [StoredState("same")] public string Second { get; private set; } = "";
                }
                """,
            "unsupported-collection" => """
                [GenerateStorage("test", 1)]
                public sealed partial class TestRoot : AggregateRoot<TestRoot>
                {
                    public TestRoot() : base(new EntityId<TestRoot>(Guid.Empty), Guid.Empty, UserAction.Empty) { }
                    [StoredState("values")] public Dictionary<string, int> Values { get; private set; } = new();
                }
                """,
            _ => """
                [GenerateStorage("test", 1)]
                public sealed partial class TestRoot : AggregateRoot<TestRoot>
                {
                    public TestRoot() : base(new EntityId<TestRoot>(Guid.Empty), Guid.Empty, UserAction.Empty) { }
                    [StoredState("name")] public string Name { get; private set; } = "";
                    [NotStored] public string Computed => Name;
                }
                """
        };

        return """
            using Synonms.Structur.Core.Entities;
            using Synonms.Structur.Domain.Aggregates;
            using Synonms.Structur.Domain.Persistence;
            using Synonms.Structur.Domain.ValueObjects;
            namespace GeneratorDiagnosticFixture;
            """ + attributes;
    }

    private static GeneratorRun Run(string source)
    {
        CSharpParseOptions parseOptions = new(LanguageVersion.Preview);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "StorageGeneratorDiagnosticFixture",
            new[] { syntaxTree },
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new StorageSourceGenerator().AsSourceGenerator() }, parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> diagnostics);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        Diagnostic[] allDiagnostics = diagnostics.Concat(result.Diagnostics).Distinct().ToArray();
        return new GeneratorRun(result.GeneratedSources.Select(sourceResult =>
            new GeneratedText(sourceResult.HintName, sourceResult.SourceText.ToString())).ToArray(), allDiagnostics);
    }

    private static IEnumerable<MetadataReference> References()
    {
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
        string? trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        foreach (string path in (trustedAssemblies ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (paths.Add(path))
            {
                yield return MetadataReference.CreateFromFile(path);
            }
        }

        Assembly[] frameworkAssemblies =
        [
            typeof(AggregateRoot<>).Assembly,
            typeof(EntityId<>).Assembly,
            typeof(GenerateStorageAttribute).Assembly,
            typeof(Synonms.Structur.Infrastructure.Persistence.IAggregateRecordMapper<,>).Assembly,
            typeof(MongoDatabaseConfiguration).Assembly
        ];
        foreach (string path in frameworkAssemblies.Select(assembly => assembly.Location).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (paths.Add(path))
            {
                yield return MetadataReference.CreateFromFile(path);
            }
        }
    }

    private sealed record GeneratedText(string HintName, string SourceText);

    private sealed record GeneratorRun(IReadOnlyList<GeneratedText> GeneratedSources, IReadOnlyList<Diagnostic> Diagnostics);
}

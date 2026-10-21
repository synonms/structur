using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Synonms.Structur.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class StorageSourceGenerator : IIncrementalGenerator
{
    private const string GenerateAttribute = "Synonms.Structur.Domain.Persistence.GenerateStorageAttribute";
    private const string StoredAttribute = "Synonms.Structur.Domain.Persistence.StoredStateAttribute";
    private const string NotStoredAttribute = "Synonms.Structur.Domain.Persistence.NotStoredAttribute";
    private const string NullPolicyAttribute = "Synonms.Structur.Domain.Persistence.StorageNullPolicyAttribute";
    private const string OverrideAttribute = "Synonms.Structur.Domain.Persistence.StorageOverrideAttribute";

    private static readonly DiagnosticDescriptor UnsupportedGraph = Descriptor("STOR001", "Unsupported storage graph",
        "Storage graph '{0}' is unsupported: {1}. Use a manual IAggregateRecordMapper<TAggregate,TRecord>.");
    private static readonly DiagnosticDescriptor Unclassified = Descriptor("STOR002", "Unclassified persisted state",
        "Property '{0}' must be classified with [StoredState] or [NotStored]; use a manual mapping for non-generated state.");
    private static readonly DiagnosticDescriptor StorageName = Descriptor("STOR003", "Storage name collision",
        "Storage name or generated type '{0}' is invalid or collides; use a unique explicit storage name or a manual mapping.");
    private static readonly DiagnosticDescriptor Restoration = Descriptor("STOR004", "Restoration binding unavailable",
        "Type or property '{0}' requires a non-generic partial class and an accessible restoration binding; use a manual mapping.");
    private static readonly DiagnosticDescriptor Rehydration = Descriptor("STOR005", "Trusted rehydration contract missing",
        "Property '{0}' has no supported trusted Rehydrate contract or declared null shape; add an explicit restoration contract or use a manual mapping.");
    private static readonly DiagnosticDescriptor Collection = Descriptor("STOR006", "Unsupported collection or override",
        "Property '{0}' uses an unsupported collection or invalid storage override: {1}; use a manual mapping.");
    private static readonly DiagnosticDescriptor SchemaConflict = Descriptor("STOR007", "Storage schema conflict",
        "Storage schema '{0}' is invalid or registered more than once; use a unique stable schema name/version or a manual mapping.");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<INamedTypeSymbol> candidates =
            context.SyntaxProvider.ForAttributeWithMetadataName(
                GenerateAttribute,
                static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax,
                static (attributeContext, _) => (INamedTypeSymbol)attributeContext.TargetSymbol);

        IncrementalValuesProvider<GenerationResult> results =
            candidates.Select(static (type, _) => GeneratorModel.Create(type));
        context.RegisterSourceOutput(results, static (productionContext, result) =>
        {
            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic);
            }

            if (result.Model is not null)
            {
                productionContext.AddSource(result.Model.HintName, SourceText.From(Emitter.Emit(result.Model), Encoding.UTF8));
            }
        });

        context.RegisterSourceOutput(results.Collect(), static (productionContext, allResults) =>
        {
            Dictionary<string, GenerationModel> schemas = new(StringComparer.Ordinal);
            foreach (GenerationModel model in allResults.Where(result => result.Model is not null)
                         .Select(result => result.Model!)
                         .OrderBy(model => model.Aggregate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal))
            {
                string key = model.SchemaName + ":" + model.SchemaVersion;
                if (schemas.ContainsKey(key))
                {
                    productionContext.ReportDiagnostic(Diagnostic.Create(SchemaConflict,
                        model.Aggregate.Locations.FirstOrDefault(), model.SchemaName));
                }
                else
                {
                    schemas.Add(key, model);
                }
            }
        });
    }

    private static DiagnosticDescriptor Descriptor(string id, string title, string message) =>
        new(id, title, message, "Structur.Persistence", DiagnosticSeverity.Error, isEnabledByDefault: true);

    private sealed class GenerationResult
    {
        public GenerationResult(GenerationModel? model, List<Diagnostic> diagnostics)
        {
            Model = model;
            Diagnostics = diagnostics;
        }

        public GenerationModel? Model { get; }
        public List<Diagnostic> Diagnostics { get; }
    }

    private sealed class GenerationModel
    {
        public INamedTypeSymbol Aggregate { get; set; } = null!;
        public string SchemaName { get; set; } = "";
        public int SchemaVersion { get; set; }
        public string RecordName { get; set; } = "";
        public string MapperName { get; set; } = "";
        public string DescriptorName { get; set; } = "";
        public List<StateProperty> Properties { get; set; } = new();
        public List<RecordModel> Records { get; set; } = new();
        public string HintName { get; set; } = "";
        public List<StoragePath> Paths { get; } = new();
    }

    private sealed class RecordModel
    {
        public string Name { get; set; } = "";
        public string BaseType { get; set; } = "";
        public INamedTypeSymbol DomainType { get; set; } = null!;
        public List<StateProperty> Properties { get; set; } = new();
        public bool IsAggregate { get; set; }
        public bool IsMember { get; set; }
    }

    private sealed class StateProperty
    {
        public IPropertySymbol Symbol { get; set; } = null!;
        public string StorageName { get; set; } = "";
        public bool AllowMissing { get; set; }
        public bool AllowNull { get; set; }
        public TypeShape Shape { get; set; } = null!;
        public string DomainPath { get; set; } = "";
    }

    private sealed class TypeShape
    {
        public string RawType { get; set; } = "";
        public bool IsNullable { get; set; }
        public bool IsReferenceType { get; set; }
        public Func<string, string> Capture { get; set; } = null!;
        public Func<string, string> Restore { get; set; } = null!;
        public List<RecordModel> NestedRecords { get; set; } = new();
        public string CodecName { get; set; } = "";
    }

    private sealed class StoragePath
    {
        public string DomainPath { get; set; } = "";
        public string RecordPath { get; set; } = "";
        public string CodecName { get; set; } = "";
    }

    private static class GeneratorModel
    {
        public static GenerationResult Create(INamedTypeSymbol aggregate)
        {
            List<Diagnostic> diagnostics = [];
            AttributeData? generation = aggregate.GetAttributes().FirstOrDefault(attribute =>
                attribute.AttributeClass?.ToDisplayString() == GenerateAttribute);
            string schema = generation?.ConstructorArguments.ElementAtOrDefault(0).Value as string ?? "";
            int version = generation?.ConstructorArguments.Length > 1 &&
                          generation.ConstructorArguments[1].Value is int schemaVersion ? schemaVersion : 1;

            if (aggregate.TypeParameters.Length != 0 || aggregate.ContainingType is not null ||
                !HasPartialDeclaration(aggregate) || aggregate.IsAbstract ||
                aggregate.DeclaredAccessibility != Accessibility.Public)
            {
                diagnostics.Add(Diagnostic.Create(Restoration, aggregate.Locations.FirstOrDefault(), aggregate.Name));
                return new GenerationResult(null, diagnostics);
            }

            if (!IsAggregateRoot(aggregate))
            {
                if (IsAggregateMember(aggregate))
                {
                    return new GenerationResult(null, diagnostics);
                }

                diagnostics.Add(Diagnostic.Create(UnsupportedGraph, aggregate.Locations.FirstOrDefault(),
                    aggregate.Name, "the opted-in type is not a concrete AggregateRoot<TSelf>"));
                return new GenerationResult(null, diagnostics);
            }

            if (string.IsNullOrWhiteSpace(schema) || version < 1)
            {
                diagnostics.Add(Diagnostic.Create(SchemaConflict, aggregate.Locations.FirstOrDefault(), schema));
                return new GenerationResult(null, diagnostics);
            }

            HashSet<string> generatedNames = new(StringComparer.Ordinal);
            string recordName = aggregate.Name + "StorageRecord";
            generatedNames.Add(recordName);
            List<RecordModel> records = [];
            List<StateProperty> properties = ReadProperties(aggregate, aggregate, "", diagnostics, generatedNames, records);
            if (diagnostics.Count > 0)
            {
                return new GenerationResult(null, diagnostics);
            }

            RecordModel root = new()
            {
                Name = recordName,
                BaseType = "global::Synonms.Structur.Infrastructure.Persistence.AggregateRootRecord<" +
                           aggregate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ">",
                DomainType = aggregate,
                Properties = properties,
                IsAggregate = true
            };
            records.Insert(0, root);
            GenerationModel model = new()
            {
                Aggregate = aggregate,
                SchemaName = schema,
                SchemaVersion = version,
                RecordName = recordName,
                MapperName = aggregate.Name + "StorageMapper",
                DescriptorName = aggregate.Name + "StorageDescriptor",
                Properties = properties,
                Records = records,
                HintName = aggregate.ToDisplayString().Replace('.', '_').Replace('+', '_') + ".Storage.g.cs"
            };
            foreach (RecordModel record in model.Records)
            {
                string firstPath = record.Properties.FirstOrDefault()?.DomainPath ?? "";
                int lastSeparator = firstPath.LastIndexOf('.');
                string pathPrefix = record.IsAggregate || lastSeparator < 0 ? "" : firstPath.Substring(0, lastSeparator);
                CollectPaths(model, record, pathPrefix);
            }
            return new GenerationResult(model, diagnostics);
        }

        private static List<StateProperty> ReadProperties(INamedTypeSymbol owner, INamedTypeSymbol root,
            string prefix, List<Diagnostic> diagnostics, HashSet<string> generatedNames, List<RecordModel> records)
        {
            List<StateProperty> properties = [];
            foreach (IFieldSymbol field in owner.GetMembers().OfType<IFieldSymbol>()
                         .Where(field => !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared))
            {
                diagnostics.Add(Diagnostic.Create(Restoration, field.Locations.FirstOrDefault(),
                    owner.Name + "." + field.Name));
            }

            foreach (IPropertySymbol property in owner.GetMembers().OfType<IPropertySymbol>()
                         .Where(property => !property.IsStatic && !property.IsIndexer)
                         .OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                bool stored = property.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == StoredAttribute);
                bool excluded = property.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == NotStoredAttribute);
                AttributeData? overrideAttribute = property.GetAttributes().FirstOrDefault(attribute =>
                    attribute.AttributeClass?.ToDisplayString() == OverrideAttribute);

                if (stored == excluded || (overrideAttribute is not null && !stored))
                {
                    diagnostics.Add(Diagnostic.Create(Unclassified, property.Locations.FirstOrDefault(),
                        owner.Name + "." + property.Name));
                    continue;
                }

                if (!stored)
                {
                    continue;
                }

                if (property.GetMethod is null || property.SetMethod is null || property.IsReadOnly ||
                    property.SetMethod.IsInitOnly && property.ContainingType.TypeKind != TypeKind.Class)
                {
                    diagnostics.Add(Diagnostic.Create(Restoration, property.Locations.FirstOrDefault(),
                        owner.Name + "." + property.Name));
                    continue;
                }

                string storageName = property.GetAttributes()
                    .First(attribute => attribute.AttributeClass?.ToDisplayString() == StoredAttribute)
                    .ConstructorArguments.ElementAtOrDefault(0).Value as string ?? "";
                if (string.IsNullOrWhiteSpace(storageName) || storageName.Contains('.') || storageName.Contains('$') ||
                    storageName.Any(char.IsControl) || storageName == "_id" || storageName == "_t" ||
                    properties.Any(candidate => candidate.StorageName == storageName))
                {
                    diagnostics.Add(Diagnostic.Create(StorageName, property.Locations.FirstOrDefault(),
                        owner.Name + "." + storageName));
                    continue;
                }

                string path = string.IsNullOrEmpty(prefix) ? property.Name : prefix + "." + property.Name;
                TypeShape? shape = CreateShape(property.Type, property, path, diagnostics, generatedNames, records, overrideAttribute);
                if (shape is not null)
                {
                    AttributeData? nullPolicy = property.GetAttributes().FirstOrDefault(attribute =>
                        attribute.AttributeClass?.ToDisplayString() == NullPolicyAttribute);
                    bool allowMissing = nullPolicy is not null &&
                                        nullPolicy.ConstructorArguments.Length > 0 &&
                                        nullPolicy.ConstructorArguments[0].Value is bool explicitMissing
                        ? explicitMissing
                        : shape.IsNullable;
                    bool allowNull = nullPolicy is not null &&
                                     nullPolicy.ConstructorArguments.Length > 1 &&
                                     nullPolicy.ConstructorArguments[1].Value is bool explicitNull
                        ? explicitNull
                        : shape.IsNullable;
                    properties.Add(new StateProperty
                    {
                        Symbol = property,
                        StorageName = storageName,
                        AllowMissing = allowMissing,
                        AllowNull = allowNull,
                        Shape = shape,
                        DomainPath = path
                    });
                }
            }

            return properties;
        }

        private static TypeShape? CreateShape(ITypeSymbol inputType, IPropertySymbol property, string path,
            List<Diagnostic> diagnostics, HashSet<string> generatedNames, List<RecordModel> records,
            AttributeData? overrideAttribute)
        {
            if (overrideAttribute is not null)
            {
                return CreateOverride(inputType, property, path, diagnostics, overrideAttribute);
            }

            bool nullable = inputType.NullableAnnotation == NullableAnnotation.Annotated;
            ITypeSymbol type = inputType;
            if (type is INamedTypeSymbol nullableType && nullableType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                type = nullableType.TypeArguments[0];
                nullable = true;
            }

            if (type is IArrayTypeSymbol)
            {
                diagnostics.Add(Diagnostic.Create(Collection, property.Locations.FirstOrDefault(), path,
                    "arrays are not supported"));
                return null;
            }

            if (IsScalar(type))
            {
                string raw = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + (nullable && type.IsReferenceType ? "?" : "");
                if (nullable && type.IsValueType)
                {
                    raw += "?";
                }

                return DirectShape(raw, nullable, "identity", type.IsReferenceType);
            }

            if (type is INamedTypeSymbol named && TryFindContract(named, "Synonms.Structur.Domain.ValueObjects.Abstractions.IScalarValueObject`2", out INamedTypeSymbol? scalarContract))
            {
                if (!HasRehydrate(named, scalarContract!.TypeArguments[1]))
                {
                    diagnostics.Add(Diagnostic.Create(Rehydration, property.Locations.FirstOrDefault(), path));
                    return null;
                }

                ITypeSymbol rawType = scalarContract.TypeArguments[1];
                if (!IsScalar(rawType))
                {
                    diagnostics.Add(Diagnostic.Create(UnsupportedGraph, property.Locations.FirstOrDefault(), path,
                        "scalar value object raw type " + rawType.ToDisplayString() + " is not an approved scalar"));
                    return null;
                }

                string raw = rawType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (nullable)
                {
                    raw += rawType.IsReferenceType ? "?" : "?";
                }

                string source = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string capture = nullable
                    ? "(((" + source + ")(" + "$VALUE" + ")) is null ? null : ((" + source + ")(" + "$VALUE" + ")).Value)"
                    : "((" + source + ")(" + "$VALUE" + ")).Value";
                string restore = nullable
                    ? "($VALUE) is null ? null : " + source + ".Rehydrate(" +
                      (rawType.IsValueType ? "$VALUE.Value" : "$VALUE!") + ")"
                    : source + ".Rehydrate($VALUE)";
                return new TypeShape
                {
                    RawType = raw,
                    IsNullable = nullable,
                    IsReferenceType = rawType.IsReferenceType,
                    Capture = expression => capture.Replace("$VALUE", expression),
                    Restore = expression => restore.Replace("$VALUE", expression),
                    NestedRecords = [],
                    CodecName = "scalar-rehydrate:" + source
                };
            }

            if (type is INamedTypeSymbol complex && TryFindContract(complex,
                    "Synonms.Structur.Domain.ValueObjects.Abstractions.IComplexValueObject`2", out INamedTypeSymbol? complexContract))
            {
                if (!HasRehydrate(complex, complexContract!.TypeArguments[1]))
                {
                    diagnostics.Add(Diagnostic.Create(Rehydration, property.Locations.FirstOrDefault(), path));
                    return null;
                }

                return CreateComplexShape(complex, complexContract!.TypeArguments[1], nullable, property, path,
                    diagnostics, generatedNames, records);
            }

            if (type is INamedTypeSymbol collection && IsCollection(collection) && !IsSupportedCollection(collection))
            {
                diagnostics.Add(Diagnostic.Create(Collection, property.Locations.FirstOrDefault(), path,
                    "only List<T> and IReadOnlyList<T> are supported"));
                return null;
            }

            if (type is INamedTypeSymbol supportedCollection && IsSupportedCollection(supportedCollection))
            {
                TypeShape? element = CreateShape(supportedCollection.TypeArguments[0], property, path + "[]",
                    diagnostics, generatedNames, records, null);
                if (element is null)
                {
                    return null;
                }

                string raw = "global::System.Collections.Generic.List<" + element.RawType + ">" +
                             (nullable ? "?" : "");
                string capturedList(string expression) => "global::System.Linq.Enumerable.ToList(global::System.Linq.Enumerable.Select(" +
                    expression + ", item => " + element.Capture("item") + "))";
                string restoredList(string expression) => "global::System.Linq.Enumerable.ToList(global::System.Linq.Enumerable.Select(" +
                    expression + ", item => " + element.Restore("item") + "))";
                return new TypeShape
                {
                    RawType = raw,
                    IsNullable = nullable,
                    IsReferenceType = true,
                    Capture = expression => nullable
                        ? "(" + expression + " is null ? null : " + capturedList(expression) + ")"
                        : capturedList(expression),
                    Restore = expression => nullable
                        ? "(" + expression + " is null ? null : " + restoredList(expression) + ")"
                        : restoredList(expression),
                    NestedRecords = element.NestedRecords,
                    CodecName = "list:" + element.CodecName
                };
            }

            if (type is INamedTypeSymbol member && IsAggregateMember(member))
            {
                return CreateMemberShape(member, nullable, property, path, diagnostics, generatedNames, records);
            }

            if (type is INamedTypeSymbol unsupportedCollection &&
                (unsupportedCollection.IsGenericType ||
                 unsupportedCollection.ContainingNamespace.ToDisplayString().StartsWith("System.Collections", StringComparison.Ordinal)))
            {
                diagnostics.Add(Diagnostic.Create(Collection, property.Locations.FirstOrDefault(), path,
                    "only List<T> and IReadOnlyList<T> are supported"));
                return null;
            }

            diagnostics.Add(Diagnostic.Create(UnsupportedGraph, property.Locations.FirstOrDefault(), path,
                "type " + type.ToDisplayString() + " is not an approved raw scalar, scalar/complex value object, or opted-in aggregate member"));
            return null;
        }

        private static TypeShape CreateOverride(ITypeSymbol inputType, IPropertySymbol property, string path,
            List<Diagnostic> diagnostics, AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length != 2 ||
                attribute.ConstructorArguments[0].Value is not ITypeSymbol rawType ||
                attribute.ConstructorArguments[1].Value is not INamedTypeSymbol codecType)
            {
                diagnostics.Add(Diagnostic.Create(Collection, property.Locations.FirstOrDefault(), path, "override must declare raw and codec types"));
                return null!;
            }

            bool validCodec = codecType.InstanceConstructors.Any(constructor => constructor.Parameters.Length == 0 &&
                constructor.DeclaredAccessibility == Accessibility.Public) &&
                codecType.TypeKind == TypeKind.Class &&
                codecType.DeclaredAccessibility == Accessibility.Public &&
                codecType.AllInterfaces.Any(contract => contract.OriginalDefinition.ToDisplayString() ==
                    "Synonms.Structur.Domain.Persistence.IStorageValueCodec<TDomain, TRaw>" &&
                    SymbolEqualityComparer.Default.Equals(contract.TypeArguments[0], inputType) &&
                    SymbolEqualityComparer.Default.Equals(contract.TypeArguments[1], rawType));
            bool supportedRawType = IsScalar(rawType) ||
                                    rawType is INamedTypeSymbol rawRecord && rawRecord.TypeKind == TypeKind.Class &&
                                    !rawRecord.IsAbstract && rawRecord.TypeParameters.Length == 0 &&
                                    rawRecord.DeclaredAccessibility == Accessibility.Public &&
                                    rawRecord.InstanceConstructors.Any(constructor =>
                                        constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public);
            if (!validCodec || !supportedRawType)
            {
                diagnostics.Add(Diagnostic.Create(Collection, property.Locations.FirstOrDefault(), path,
                    "codec must be public, parameterlessly constructible, typed for this property, and use an approved scalar or explicit record DTO"));
                return null!;
            }

            bool nullable = property.NullableAnnotation == NullableAnnotation.Annotated;
            string raw = rawType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) +
                         (nullable ? "?" : "");
            string codec = codecType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string codecInstance = "global::Synonms.Structur.Domain.Persistence.StorageValueCodecCache<" + codec + ">.Instance";
            return new TypeShape
            {
                RawType = raw,
                IsNullable = nullable,
                IsReferenceType = rawType.IsReferenceType,
                Capture = expression => nullable
                    ? "(" + expression + " is null ? null : " + codecInstance + ".Capture(" + expression + "))"
                    : codecInstance + ".Capture(" + expression + ")",
                Restore = expression => nullable
                    ? "(" + expression + " is null ? null : " + codecInstance + ".Restore(" +
                      (rawType.IsValueType ? expression + ".Value" : expression + "!") + "))"
                    : codecInstance + ".Restore(" + expression + ")",
                NestedRecords = [],
                CodecName = codec
            };
        }

        private static TypeShape? CreateComplexShape(INamedTypeSymbol complex, ITypeSymbol dataType, bool nullable,
            IPropertySymbol property, string path, List<Diagnostic> diagnostics, HashSet<string> generatedNames,
            List<RecordModel> records)
        {
            if (dataType is not INamedTypeSymbol data || data.TypeKind != TypeKind.Class)
            {
                diagnostics.Add(Diagnostic.Create(Rehydration, property.Locations.FirstOrDefault(), path));
                return null;
            }

            if (data.GetMembers().OfType<IFieldSymbol>().Any(field =>
                    !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared))
            {
                diagnostics.Add(Diagnostic.Create(UnsupportedGraph, property.Locations.FirstOrDefault(), path,
                    "complex value-object data contracts may contain public raw properties only"));
                return null;
            }

            string recordName = new string(path.Select(character =>
                char.IsLetterOrDigit(character) ? character : '_').ToArray()) + "StorageData";
            if (!generatedNames.Add(recordName))
            {
                diagnostics.Add(Diagnostic.Create(StorageName, property.Locations.FirstOrDefault(), recordName));
                return null;
            }

            List<StateProperty> dataProperties = [];
            foreach (IPropertySymbol dataProperty in data.GetMembers().OfType<IPropertySymbol>()
                         .Where(candidate => !candidate.IsStatic && !candidate.IsIndexer)
                         .OrderBy(candidate => candidate.Name, StringComparer.Ordinal))
            {
                if (dataProperty.GetMethod?.DeclaredAccessibility != Accessibility.Public ||
                    dataProperty.SetMethod?.DeclaredAccessibility != Accessibility.Public)
                {
                    diagnostics.Add(Diagnostic.Create(Rehydration, property.Locations.FirstOrDefault(),
                        path + "." + dataProperty.Name));
                    return null;
                }

                TypeShape? shape = CreateShape(dataProperty.Type, property, path + "." + dataProperty.Name,
                    diagnostics, generatedNames, records, null);
                if (shape is null)
                {
                    diagnostics.Add(Diagnostic.Create(Rehydration, property.Locations.FirstOrDefault(), path + "." + dataProperty.Name));
                    return null;
                }

                dataProperties.Add(new StateProperty
                {
                    Symbol = dataProperty,
                    StorageName = dataProperty.Name,
                    AllowMissing = shape.IsNullable,
                    AllowNull = shape.IsNullable,
                    Shape = shape,
                    DomainPath = path + "." + dataProperty.Name
                });
            }

            if (dataProperties.Count == 0)
            {
                diagnostics.Add(Diagnostic.Create(Rehydration, property.Locations.FirstOrDefault(), path));
                return null;
            }

            string domainType = complex.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string dataFqn = data.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string rawType = "global::" + (property.ContainingType.ContainingNamespace.IsGlobalNamespace ? "" : property.ContainingType.ContainingNamespace.ToDisplayString() + ".") +
                             recordName + (nullable ? "?" : "");
            RecordModel nested = new()
            {
                Name = recordName,
                BaseType = "",
                DomainType = complex,
                Properties = dataProperties,
                IsAggregate = false,
                IsMember = false
            };
            records.Add(nested);
            string capture = "new " + rawType.TrimEnd('?') + " { " +
                string.Join(", ", dataProperties.Select(item => item.Symbol.Name + " = " +
                    item.Shape.Capture("data." + item.Symbol.Name))) + " }";
            // Keep ToData single-evaluation and do not call a validation factory during restoration.
            string captureExpression = nullable
                ? "(" + "$VALUE" + " is null ? null : " + CaptureComplex(capture) + ")"
                : CaptureComplex(capture);
            string restoreObject = "new " + dataFqn + " { " +
                string.Join(", ", dataProperties.Select(item => item.Symbol.Name + " = " +
                    item.Shape.Restore("record." + item.Symbol.Name))) + " }";
            string restoreExpression = nullable
                ? "($VALUE) is null ? null : " + domainType + ".Rehydrate(" + restoreObject.Replace("record.", "$VALUE.") + ")"
                : domainType + ".Rehydrate(" + restoreObject.Replace("record.", "$VALUE.") + ")";
            return new TypeShape
            {
                RawType = rawType,
                IsNullable = nullable,
                IsReferenceType = true,
                Capture = expression => captureExpression.Replace("$VALUE", expression),
                Restore = expression => restoreExpression.Replace("$VALUE", expression),
                NestedRecords = [nested],
                CodecName = "complex-rehydrate:" + domainType
            };
        }

        private static string CaptureComplex(string initializer) =>
            "__StructurCaptureData(" + "$VALUE" + ".ToData(), data => " + initializer + ")";

        private static TypeShape? CreateMemberShape(INamedTypeSymbol member, bool nullable,
            IPropertySymbol property, string path, List<Diagnostic> diagnostics, HashSet<string> generatedNames,
            List<RecordModel> records)
        {
            if (member.GetAttributes().All(attribute => attribute.AttributeClass?.ToDisplayString() != GenerateAttribute))
            {
                diagnostics.Add(Diagnostic.Create(UnsupportedGraph, property.Locations.FirstOrDefault(), path,
                    "aggregate members must opt in with [GenerateStorage]"));
                return null;
            }

            if (!HasPartialDeclaration(member) || member.TypeParameters.Length != 0 || member.ContainingType is not null ||
                member.DeclaredAccessibility != Accessibility.Public)
            {
                diagnostics.Add(Diagnostic.Create(Restoration, property.Locations.FirstOrDefault(), member.Name));
                return null;
            }
            if (!SymbolEqualityComparer.Default.Equals(member.ContainingNamespace, property.ContainingType.ContainingNamespace))
            {
                diagnostics.Add(Diagnostic.Create(Restoration, property.Locations.FirstOrDefault(), member.Name));
                return null;
            }

            string recordName = member.Name + "StorageRecord";
            if (!generatedNames.Add(recordName))
            {
                diagnostics.Add(Diagnostic.Create(StorageName, property.Locations.FirstOrDefault(), recordName));
                return null;
            }

            List<StateProperty> memberProperties = ReadProperties(member, member, path, diagnostics, generatedNames, records);
            records.Add(new RecordModel
            {
                Name = recordName,
                BaseType = "global::Synonms.Structur.Infrastructure.Persistence.AggregateMemberRecord<" +
                           member.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ">",
                DomainType = member,
                Properties = memberProperties,
                IsAggregate = false,
                IsMember = true
            });

            string domainType = member.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string raw = "global::" + (property.ContainingType.ContainingNamespace.IsGlobalNamespace ? "" : property.ContainingType.ContainingNamespace.ToDisplayString() + ".") +
                         recordName + (nullable ? "?" : "");
            return new TypeShape
            {
                RawType = raw,
                IsNullable = nullable,
                Capture = expression => nullable
                    ? "(" + expression + " is null ? null : " + domainType + ".__StructurCapture(" + expression + "))"
                    : domainType + ".__StructurCapture(" + expression + ")",
                Restore = expression => nullable
                    ? "(" + expression + " is null ? null : " + domainType + ".__StructurRestore(" + expression + "))"
                    : domainType + ".__StructurRestore(" + expression + ")",
                NestedRecords = [],
                CodecName = "member:" + domainType
            };
        }

        private static TypeShape DirectShape(string rawType, bool nullable, string codec, bool isReferenceType) => new()
        {
            RawType = rawType,
            IsNullable = nullable,
            IsReferenceType = isReferenceType,
            Capture = expression => expression,
            Restore = expression => expression,
            NestedRecords = [],
            CodecName = codec + (nullable ? ":nullable" : "")
        };

        private static bool IsScalar(ITypeSymbol type) =>
            type.SpecialType is SpecialType.System_String or SpecialType.System_Boolean or SpecialType.System_Int32 or
                SpecialType.System_Int64 or SpecialType.System_Double or SpecialType.System_Decimal or
                SpecialType.System_DateTime ||
            type.ToDisplayString() is "System.Guid" or "System.DateOnly";

        private static bool IsCollection(INamedTypeSymbol type) =>
            type.ContainingNamespace.ToDisplayString() == "System.Collections.Generic" ||
            type.AllInterfaces.Any(contract => contract.Name == "IEnumerable") ||
            type.Name == "IEnumerable";

        private static bool IsSupportedCollection(INamedTypeSymbol type) =>
            type.IsGenericType &&
            (type.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.List<T>" ||
             type.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IReadOnlyList<T>");

        private static bool HasRehydrate(INamedTypeSymbol type, ITypeSymbol rawType) =>
            type.GetMembers("Rehydrate").OfType<IMethodSymbol>().Any(method =>
                method.IsStatic && method.Parameters.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, rawType) &&
                SymbolEqualityComparer.Default.Equals(method.ReturnType, type));

        private static bool TryFindContract(INamedTypeSymbol type, string metadataName, out INamedTypeSymbol? contract)
        {
            contract = type.AllInterfaces.FirstOrDefault(candidate =>
                candidate.OriginalDefinition.ToDisplayString() == metadataName.Replace('`', '<') &&
                candidate.OriginalDefinition.MetadataName == metadataName.Substring(metadataName.LastIndexOf('.') + 1));
            // MetadataName is stable across compiler display formats; avoid relying on generic display syntax.
            if (contract is not null)
            {
                return true;
            }

            string expectedName = metadataName.Substring(metadataName.LastIndexOf('.') + 1);
            contract = type.AllInterfaces.FirstOrDefault(candidate =>
                candidate.OriginalDefinition.MetadataName == expectedName &&
                candidate.OriginalDefinition.ContainingNamespace.ToDisplayString() ==
                metadataName.Substring(0, metadataName.LastIndexOf('.')));
            return contract is not null;
        }

        private static bool IsAggregateRoot(INamedTypeSymbol type)
        {
            INamedTypeSymbol? baseType = type.BaseType;
            return baseType is not null &&
                   baseType.OriginalDefinition.ToDisplayString() ==
                   "Synonms.Structur.Domain.Aggregates.AggregateRoot<TAggregateRoot>" &&
                   baseType.TypeArguments.Length == 1 &&
                   SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[0], type);
        }

        private static bool IsAggregateMember(INamedTypeSymbol type)
        {
            INamedTypeSymbol? baseType = type.BaseType;
            return baseType is not null &&
                   baseType.OriginalDefinition.ToDisplayString() ==
                   "Synonms.Structur.Domain.Aggregates.AggregateMember<TAggregateMember>" &&
                   baseType.TypeArguments.Length == 1 &&
                   SymbolEqualityComparer.Default.Equals(baseType.TypeArguments[0], type);
        }

        private static bool HasPartialDeclaration(INamedTypeSymbol type) =>
            type.DeclaringSyntaxReferences.Any(reference =>
                reference.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax declaration &&
                declaration.Modifiers.Any(modifier => modifier.Text == "partial"));

        private static void CollectPaths(GenerationModel model, RecordModel record, string prefix)
        {
            foreach (StateProperty property in record.Properties)
            {
                model.Paths.Add(new StoragePath
                {
                    DomainPath = property.DomainPath,
                    RecordPath = string.IsNullOrEmpty(prefix) ? property.StorageName : prefix + "." + property.StorageName,
                    CodecName = property.Shape.CodecName
                });
            }
        }
    }

    private static class Emitter
    {
        public static string Emit(GenerationModel model)
        {
            StringBuilder source = new();
            source.AppendLine("// <auto-generated/>");
            source.AppendLine("#nullable enable");
            if (!model.Aggregate.ContainingNamespace.IsGlobalNamespace)
            {
                source.Append("namespace ").Append(model.Aggregate.ContainingNamespace.ToDisplayString()).AppendLine(";");
            }

            foreach (RecordModel record in model.Records)
            {
                EmitRecord(source, record);
            }

            foreach (RecordModel record in model.Records.Where(record => record.IsMember))
            {
                EmitMemberBridge(source, record);
            }

            EmitRootBridge(source, model);
            EmitMapperAndDescriptor(source, model);
            return source.ToString();
        }

        private static void EmitRecord(StringBuilder source, RecordModel record)
        {
            source.Append("public sealed class ").Append(record.Name);
            if (!string.IsNullOrEmpty(record.BaseType))
            {
                source.Append(" : ").Append(record.BaseType);
            }
            source.AppendLine();
            source.AppendLine("{");
            foreach (StateProperty property in record.Properties)
            {
                source.Append("    public ").Append(property.Shape.RawType).Append(' ')
                    .Append(property.Symbol.Name).Append(" { get; set; }");
                if (property.Shape.RawType.EndsWith("[]", StringComparison.Ordinal) ||
                    property.Shape.RawType.StartsWith("global::System.Collections.Generic.List<", StringComparison.Ordinal))
                {
                    source.Append(" = new();");
                }
                else if (!property.Shape.RawType.EndsWith("?", StringComparison.Ordinal) &&
                         property.Shape.IsReferenceType)
                {
                    source.Append(" = default!;");
                }
                source.AppendLine();
            }
            source.AppendLine("}");
            source.AppendLine();
        }

        private static void EmitMemberBridge(StringBuilder source, RecordModel record)
        {
            INamedTypeSymbol type = record.DomainType;
            string typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string recordName = record.Name;
            source.Append("partial class ").Append(type.Name).AppendLine();
            source.AppendLine("{");
            source.Append("    internal static ").Append(recordName).Append(" __StructurCapture(").Append(typeName).AppendLine(" value)");
            source.AppendLine("    {");
            source.Append("        ").Append(recordName).Append(" record = new() { Id = value.Id.Value");
            foreach (StateProperty property in record.Properties)
            {
                source.Append(", ").Append(property.Symbol.Name).Append(" = ").Append(property.Shape.Capture("value." + property.Symbol.Name));
            }
            source.AppendLine(" };");
            source.AppendLine("        return record;");
            source.AppendLine("    }");
            source.Append("    internal static ").Append(typeName).Append(" __StructurRestore(").Append(recordName)
                .Append(" record) => new ").Append(type.Name).AppendLine("(new __StructurStorageConstructor(record));");
            source.Append("    private ").Append(type.Name).Append("(__StructurStorageConstructor marker) : base(new global::Synonms.Structur.Core.Entities.EntityId<")
                .Append(typeName).AppendLine(">(marker.Record.Id))");
            source.AppendLine("    {");
            foreach (StateProperty property in record.Properties)
            {
                source.Append("        ").Append(property.Symbol.Name).Append(" = ").Append(property.Shape.Restore("marker.Record." + property.Symbol.Name)).AppendLine(";");
            }
            source.AppendLine("    }");
            source.AppendLine("    private static TStorage __StructurCaptureData<TData, TStorage>(TData data, global::System.Func<TData, TStorage> capture) => capture(data);");
            source.Append("    private sealed class __StructurStorageConstructor(").Append(recordName).AppendLine(" record)");
            source.AppendLine("    {");
            source.AppendLine("        public " + recordName + " Record { get; } = record;");
            source.AppendLine("    }");
            source.AppendLine("}");
            source.AppendLine();
        }

        private static void EmitRootBridge(StringBuilder source, GenerationModel model)
        {
            INamedTypeSymbol root = model.Aggregate;
            string rootName = root.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            source.Append("partial class ").Append(root.Name).AppendLine();
            source.AppendLine("{");
            source.Append("    internal static ").Append(model.RecordName).Append(" __StructurCapture(").Append(rootName).AppendLine(" value)");
            source.AppendLine("    {");
            source.Append("        ").Append(model.RecordName).AppendLine(" record = new()");
            source.AppendLine("        {");
            source.AppendLine("            Id = value.Id.Value,");
            source.AppendLine("            TenantId = value.TenantId,");
            source.AppendLine("            EntityTag = value.EntityTag.Value,");
            source.AppendLine("            CreatedAction = __StructurCaptureAction(value.CreatedAction),");
            source.AppendLine("            UpdatedAction = value.UpdatedAction is null ? null : __StructurCaptureAction(value.UpdatedAction),");
            source.AppendLine("            DeletedAction = value.DeletedAction is null ? null : __StructurCaptureAction(value.DeletedAction),");
            foreach (StateProperty property in model.Properties)
            {
                source.Append("            ").Append(property.Symbol.Name).Append(" = ")
                    .Append(property.Shape.Capture("value." + property.Symbol.Name)).AppendLine(",");
            }
            source.AppendLine("        };");
            source.AppendLine("        return record;");
            source.AppendLine("    }");
            source.Append("    internal static ").Append(rootName).Append(" __StructurRestore(").Append(model.RecordName)
                .Append(" record) => new ").Append(root.Name).AppendLine("(new __StructurStorageConstructor(record));");
            source.Append("    private ").Append(root.Name).Append("(__StructurStorageConstructor marker) : base(")
                .Append("new global::Synonms.Structur.Core.Entities.EntityId<").Append(rootName).AppendLine(">(marker.Record.Id),")
                .AppendLine("        marker.Record.TenantId,")
                .AppendLine("        new global::Synonms.Structur.Core.Entities.EntityTag(marker.Record.EntityTag),")
                .AppendLine("        __StructurRestoreAction(marker.Record.CreatedAction),")
                .AppendLine("        marker.Record.UpdatedAction is null ? null : __StructurRestoreAction(marker.Record.UpdatedAction),")
                .AppendLine("        marker.Record.DeletedAction is null ? null : __StructurRestoreAction(marker.Record.DeletedAction))");
            source.AppendLine("    {");
            foreach (StateProperty property in model.Properties)
            {
                source.Append("        ").Append(property.Symbol.Name).Append(" = ").Append(property.Shape.Restore("marker.Record." + property.Symbol.Name)).AppendLine(";");
            }
            source.AppendLine("    }");
            source.AppendLine("    private static global::Synonms.Structur.Infrastructure.Persistence.UserActionRecord __StructurCaptureAction(global::Synonms.Structur.Domain.ValueObjects.UserAction value) => new()");
            source.AppendLine("    {");
            source.AppendLine("        ActionAt = value.ActionAt, ActionById = value.ActionById, ActionByName = value.ActionByName");
            source.AppendLine("    };");
            source.AppendLine("    private static global::Synonms.Structur.Domain.ValueObjects.UserAction __StructurRestoreAction(global::Synonms.Structur.Infrastructure.Persistence.UserActionRecord value) =>");
            source.AppendLine("        global::Synonms.Structur.Domain.ValueObjects.UserAction.Rehydrate(new global::Synonms.Structur.Domain.ValueObjects.UserActionDto");
            source.AppendLine("        { ActionAt = value.ActionAt, ActionById = value.ActionById, ActionByName = value.ActionByName });");
            source.AppendLine("    private static TStorage __StructurCaptureData<TData, TStorage>(TData data, global::System.Func<TData, TStorage> capture) => capture(data);");
            source.Append("    private sealed class __StructurStorageConstructor(").Append(model.RecordName).AppendLine(" record)");
            source.AppendLine("    {");
            source.AppendLine("        public " + model.RecordName + " Record { get; } = record;");
            source.AppendLine("    }");
            source.AppendLine("}");
            source.AppendLine();
        }

        private static void EmitMapperAndDescriptor(StringBuilder source, GenerationModel model)
        {
            string root = model.Aggregate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            source.Append("public sealed class ").Append(model.MapperName)
                .Append(" : global::Synonms.Structur.Infrastructure.Persistence.IAggregateRecordMapper<")
                .Append(root).Append(", ").Append(model.RecordName).AppendLine(">");
            source.AppendLine("{");
            source.Append("    public ").Append(model.RecordName).Append(" Capture(").Append(root)
                .AppendLine(" aggregate) => " + model.Aggregate.Name + ".__StructurCapture(aggregate);");
            source.Append("    public ").Append(root).Append(" Restore(").Append(model.RecordName)
                .AppendLine(" record) => " + model.Aggregate.Name + ".__StructurRestore(record);");
            source.AppendLine("}");
            source.AppendLine();
            source.Append("public static class ").Append(model.DescriptorName).AppendLine();
            source.AppendLine("{");
            source.Append("    public const string SchemaName = ").Append(Literal(model.SchemaName)).AppendLine(";");
            source.Append("    public const int SchemaVersion = ").Append(model.SchemaVersion).AppendLine(";");
            source.Append("    public static global::Synonms.Structur.Infrastructure.Persistence.AggregateRecordMapping<")
                .Append(root).Append(", ").Append(model.RecordName).Append("> CreateMapping() =>");
            source.Append(" new(new ").Append(model.MapperName).AppendLine("(), new global::Synonms.Structur.Infrastructure.Persistence.RecordPropertyPolicy[]");
            source.AppendLine("    {");
            foreach (RecordModel record in model.Records)
            {
                foreach (StateProperty property in record.Properties)
                {
                    source.Append("        new(typeof(").Append(record.Name).Append("), nameof(").Append(record.Name).Append('.')
                        .Append(property.Symbol.Name).Append("), ").Append(Literal(property.StorageName))
                        .Append(", AllowMissing: ").Append(property.AllowMissing ? "true" : "false")
                        .Append(", AllowNull: ").Append(property.AllowNull ? "true" : "false").AppendLine("),");
                }
            }
            source.AppendLine("    });");
            source.AppendLine("    public static global::System.Collections.Generic.IReadOnlyDictionary<string, string> DomainToRecordPaths { get; } =");
            source.AppendLine("        new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal)");
            source.AppendLine("        {");
            foreach (StoragePath path in model.Paths.OrderBy(path => path.DomainPath, StringComparer.Ordinal))
            {
                source.Append("            [").Append(Literal(path.DomainPath)).Append("] = ").Append(Literal(path.RecordPath)).AppendLine(",");
            }
            source.AppendLine("        };");
            source.AppendLine("    public static global::System.Collections.Generic.IReadOnlyDictionary<string, string> DomainToRecordCodecs { get; } =");
            source.AppendLine("        new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal)");
            source.AppendLine("        {");
            foreach (StoragePath path in model.Paths.OrderBy(path => path.DomainPath, StringComparer.Ordinal))
            {
                source.Append("            [").Append(Literal(path.DomainPath)).Append("] = ").Append(Literal(path.CodecName)).AppendLine(",");
            }
            source.AppendLine("        };");
            source.AppendLine("    public static global::Synonms.Structur.Infrastructure.MongoDb.Hosting.MongoDatabaseConfiguration Register(string databaseName, global::System.Collections.Generic.IDictionary<global::System.Type, string> collections) =>");
            source.Append("        new global::Synonms.Structur.Infrastructure.MongoDb.Hosting.MongoDatabaseConfiguration(databaseName, collections, new global::Synonms.Structur.Infrastructure.Persistence.AggregateRecordMapping[] { CreateMapping() })");
            source.AppendLine(";");
            source.AppendLine("}");
        }

        private static string Literal(string value) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true);
    }
}

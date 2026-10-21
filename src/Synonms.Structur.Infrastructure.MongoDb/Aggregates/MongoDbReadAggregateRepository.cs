using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;

namespace Synonms.Structur.Infrastructure.MongoDb.Aggregates;

public class MongoDbReadAggregateRepository<TAggregateRoot> : IReadAggregateRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private static readonly Collation OrdinalStringCollation = new("simple");
    private readonly IMongoCollection<BsonDocument> _mongoCollection;
    private readonly MongoDbAggregateQueryTranslator<TAggregateRoot> _queryTranslator;
    protected MongoDbAggregateRecordMapper<TAggregateRoot> RecordMapper { get; }

    public MongoDbReadAggregateRepository(IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration)
    {
        RecordMapper = new MongoDbAggregateRecordMapper<TAggregateRoot>(mongoDatabaseConfiguration);
        _queryTranslator = new MongoDbAggregateQueryTranslator<TAggregateRoot>(RecordMapper);
        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<BsonDocument>(mongoDatabaseConfiguration.GetCollectionName<TAggregateRoot>());
    }

    public virtual FilterDefinition<BsonDocument> GlobalFilter =>
        Builders<BsonDocument>.Filter.Eq(RecordMapper.StorageName(nameof(AggregateRoot<TAggregateRoot>.DeletedAction)), BsonNull.Value);

    public async Task<bool> AnyAsync(AggregateQuery<TAggregateRoot> query, CancellationToken cancellationToken = default)
    {
        IFindFluent<BsonDocument, BsonDocument> find = _mongoCollection.Find(ScopedFilter(query));
        find.Options.Collation = OrdinalStringCollation;
        BsonDocument? document = await find
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        return document is not null;
    }

    public async Task<Maybe<TAggregateRoot>> FindAsync(EntityId<TAggregateRoot> id, CancellationToken cancellationToken = default)
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.And(
            GlobalFilter,
            Builders<BsonDocument>.Filter.Eq("_id", new BsonBinaryData(id.Value, GuidRepresentation.Standard)));

        BsonDocument? document = await _mongoCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document is null ? Maybe<TAggregateRoot>.None : MapToAggregateRoot(document);
    }

    public async Task<TAggregateRoot?> FindFirstAsync(AggregateQuery<TAggregateRoot> query, CancellationToken cancellationToken = default)
    {
        IFindFluent<BsonDocument, BsonDocument> find = _mongoCollection.Find(ScopedFilter(query));
        find.Options.Collation = OrdinalStringCollation;
        BsonDocument? document = await find.Sort(_queryTranslator.Sort(query))
            .Skip(query.SkipCount)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : MapToAggregateRoot(document);
    }

    public async Task<IReadOnlyList<TAggregateRoot>> ListAsync(AggregateQuery<TAggregateRoot> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        int take = query.TakeCount ?? AggregateQuery<TAggregateRoot>.MaximumTake;
        IFindFluent<BsonDocument, BsonDocument> find = _mongoCollection.Find(ScopedFilter(query));
        find.Options.Collation = OrdinalStringCollation;
        find = find.Sort(_queryTranslator.Sort(query))
            .Skip(query.SkipCount)
            .Limit(take);
        using IAsyncCursor<BsonDocument> cursor = await find.ToCursorAsync(cancellationToken);
        List<TAggregateRoot> aggregates = new(take);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (BsonDocument document in cursor.Current)
            {
                aggregates.Add(MapToAggregateRoot(document));
            }
        }

        return aggregates.AsReadOnly();
    }

    public Task<long> CountAsync(AggregateQuery<TAggregateRoot> query, CancellationToken cancellationToken = default) =>
        _mongoCollection.CountDocumentsAsync(ScopedFilter(query),
            new CountOptions { Collation = OrdinalStringCollation }, cancellationToken);

    public async Task<IReadOnlyList<TOut>> SelectAsync<TOut>(AggregateQuery<TAggregateRoot> query,
        Expression<Func<TAggregateRoot, TOut>> selector, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(selector);
        SelectionPlan<TOut> selection = CreateSelection(selector);

        int take = query.TakeCount ?? AggregateQuery<TAggregateRoot>.MaximumTake;
        IFindFluent<BsonDocument, BsonDocument> find = _mongoCollection.Find(ScopedFilter(query));
        find.Options.Collation = OrdinalStringCollation;
        find = find.Sort(_queryTranslator.Sort(query))
            .Skip(query.SkipCount)
            .Limit(take)
            .Project<BsonDocument>(selection.Projection);

        using IAsyncCursor<BsonDocument> cursor = await find.ToCursorAsync(cancellationToken);
        List<TOut> values = new(take);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (BsonDocument document in cursor.Current)
            {
                values.Add(selection.Materialize(document));
            }
        }

        return values.AsReadOnly();
    }

    protected TAggregateRoot MapToAggregateRoot(BsonDocument document) => RecordMapper.Restore(document);

    private FilterDefinition<BsonDocument> ScopedFilter(AggregateQuery<TAggregateRoot> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Builders<BsonDocument>.Filter.And(GlobalFilter, _queryTranslator.Predicate(query));
    }

    private static bool IsProjectionScalar(Type type) =>
        type == typeof(string) || type == typeof(bool) || type == typeof(int) || type == typeof(long) ||
        type == typeof(double) || type == typeof(decimal) || type == typeof(Guid) ||
        type == typeof(DateOnly) || type == typeof(DateTime);

    private SelectionPlan<TOut> CreateSelection<TOut>(Expression<Func<TAggregateRoot, TOut>> selector)
    {
        if (selector.Body is not MemberInitExpression memberInit ||
            memberInit.NewExpression.Arguments.Count != 0 ||
            memberInit.Bindings.Count == 0)
        {
            MongoDbAggregateQueryTranslator<TAggregateRoot>.ResolvedPath direct =
                _queryTranslator.Projection(selector.Body, selector.Parameters[0]);
            Type rawType = Nullable.GetUnderlyingType(direct.Value.Type) ?? direct.Value.Type;
            Type outputType = Nullable.GetUnderlyingType(typeof(TOut)) ?? typeof(TOut);
            if (rawType != outputType || !IsProjectionScalar(rawType))
            {
                throw new InvalidOperationException(
                    $"Unsupported aggregate selection ({selector.Body.NodeType}): select a declared scalar field or a simple parameterless DTO member initializer.");
            }

            BsonDocument directProjection = new("_selected", "$" + direct.Path) { ["_id"] = 0 };
            return new SelectionPlan<TOut>(directProjection, document =>
            {
                BsonValue value = document.GetValue("_selected", BsonNull.Value);
                return value.IsBsonNull ? default! : (TOut)DecodeScalar(value, rawType);
            });
        }

        if (memberInit.NewExpression.Type.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new InvalidOperationException(
                $"Unsupported aggregate selection ({selector.Body.NodeType}): DTO projections require an accessible parameterless constructor.");
        }

        List<(PropertyInfo Property, string Field, Type RawType)> assignments = [];
        BsonDocument projection = new() { ["_id"] = 0 };
        foreach (MemberBinding binding in memberInit.Bindings)
        {
            if (binding is not MemberAssignment { Member: PropertyInfo { SetMethod.IsPublic: true } property, Expression: var expression } ||
                projection.Contains(property.Name))
            {
                throw new InvalidOperationException(
                    $"Unsupported aggregate selection ({selector.Body.NodeType}): DTO projections require unique writable properties assigned from scalar fields.");
            }

            MongoDbAggregateQueryTranslator<TAggregateRoot>.ResolvedPath path =
                _queryTranslator.Projection(expression, selector.Parameters[0]);
            Type rawType = Nullable.GetUnderlyingType(path.Value.Type) ?? path.Value.Type;
            Type outputType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (rawType != outputType || !IsProjectionScalar(rawType))
            {
                throw new InvalidOperationException(
                    $"Unsupported aggregate selection ({selector.Body.NodeType}): DTO members must match declared raw scalar field types.");
            }

            projection[property.Name] = "$" + path.Path;
            assignments.Add((property, property.Name, rawType));
        }

        return new SelectionPlan<TOut>(projection, document =>
        {
            TOut instance = Activator.CreateInstance<TOut>() ??
                throw new InvalidOperationException($"Cannot create selected DTO type {typeof(TOut).Name}.");
            foreach ((PropertyInfo property, string field, Type type) in assignments)
            {
                BsonValue value = document.GetValue(field, BsonNull.Value);
                property.SetValue(instance, value.IsBsonNull ? null : DecodeScalar(value, type));
            }

            return instance;
        });
    }

    private static object DecodeScalar(BsonValue value, Type type)
    {
        object decoded = type == typeof(string) && value.IsString ? value.AsString :
            type == typeof(bool) && value.IsBoolean ? value.AsBoolean :
            type == typeof(int) && value.IsInt32 ? value.AsInt32 :
            type == typeof(long) && value.IsInt64 ? value.AsInt64 :
            type == typeof(double) && value.IsDouble ? value.AsDouble :
            type == typeof(decimal) && value.IsDecimal128 ? Decimal128.ToDecimal(value.AsDecimal128) :
            type == typeof(Guid) && value is BsonBinaryData { SubType: BsonBinarySubType.UuidStandard } binary
                ? binary.ToGuid(GuidRepresentation.Standard) :
            type == typeof(DateOnly) && value.IsString &&
                DateOnly.TryParseExact(value.AsString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
                    ? date :
            type == typeof(DateTime) && value.IsBsonDateTime ? value.ToUniversalTime() :
            throw new InvalidOperationException($"Selected raw BSON value is incompatible with {type.Name}.");
        return decoded;
    }

    private sealed record SelectionPlan<TOut>(BsonDocument Projection, Func<BsonDocument, TOut> Materialize);
}

using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Infrastructure.Persistence;

namespace Synonms.Structur.Infrastructure.MongoDb.Aggregates;

/// <summary>Translates the deliberately bounded aggregate expression language to raw BSON paths.</summary>
internal sealed class MongoDbAggregateQueryTranslator<TAggregate>
    where TAggregate : AggregateRoot<TAggregate>
{
    private readonly MongoDbAggregateRecordMapper<TAggregate> _mapper;
    private readonly RawRecordSchema? _schema;

    public MongoDbAggregateQueryTranslator(MongoDbAggregateRecordMapper<TAggregate> mapper)
    {
        _mapper = mapper;
        _schema = mapper.Schema;
    }

    public BsonDocument Predicate(AggregateQuery<TAggregate> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Predicate is null)
        {
            return new BsonDocument();
        }

        if (_schema is null)
        {
            throw new InvalidOperationException(
                $"Predicate queries for {typeof(TAggregate).Name} require a typed raw-record mapping; legacy direct persistence does not support server-side translation.");
        }

        return Boolean(query.Predicate.Body, new Dictionary<ParameterExpression, QueryScope>
        {
            [query.Predicate.Parameters[0]] = new QueryScope(typeof(TAggregate), _schema.Root, "")
        });
    }

    public BsonDocument Sort(AggregateQuery<TAggregate> query)
    {
        BsonDocument sort = new();
        if (_schema is null && query.SortKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Sort queries for {typeof(TAggregate).Name} require a typed raw-record mapping; legacy direct persistence does not support server-side translation.");
        }

        foreach (AggregateQuerySort<TAggregate> item in query.SortKeys)
        {
            ResolvedPath path = Resolve(item.Key.Body, new Dictionary<ParameterExpression, QueryScope>
            {
                [item.Key.Parameters[0]] = new QueryScope(typeof(TAggregate), _schema!.Root, "")
            });
            sort[path.Path] = item.Descending ? -1 : 1;
        }

        if (!sort.Contains("_id"))
        {
            sort["_id"] = 1;
        }

        return sort;
    }

    public ResolvedPath Projection(Expression expression, ParameterExpression parameter)
    {
        if (_schema is null)
        {
            throw new InvalidOperationException(
                $"Selection queries for {typeof(TAggregate).Name} require a typed raw-record mapping; legacy direct persistence does not support server-side translation.");
        }

        return Resolve(expression, new Dictionary<ParameterExpression, QueryScope>
        {
            [parameter] = new QueryScope(typeof(TAggregate), _schema.Root, "")
        });
    }

    private BsonDocument Boolean(Expression expression, IReadOnlyDictionary<ParameterExpression, QueryScope> scopes)
    {
        expression = StripConvert(expression);
        if (expression is BinaryExpression { NodeType: ExpressionType.AndAlso or ExpressionType.OrElse } logical)
        {
            return new BsonDocument(logical.NodeType == ExpressionType.AndAlso ? "$and" : "$or",
                new BsonArray(new[] { Boolean(logical.Left, scopes), Boolean(logical.Right, scopes) }));
        }

        if (expression is UnaryExpression { NodeType: ExpressionType.Not } not)
        {
            return new BsonDocument("$nor", new BsonArray(Boolean(not.Operand, scopes)));
        }

        if (expression is BinaryExpression comparison &&
            comparison.NodeType is ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.GreaterThan or
                ExpressionType.GreaterThanOrEqual or ExpressionType.LessThan or ExpressionType.LessThanOrEqual)
        {
            Expression member = IsPath(comparison.Left) ? comparison.Left : comparison.Right;
            Expression constant = ReferenceEquals(member, comparison.Left) ? comparison.Right : comparison.Left;
            if (!IsPath(member))
            {
                throw Unsupported(expression, "comparison must reference a declared aggregate field");
            }

            ResolvedPath path = Resolve(member, scopes);
            object? value = EvaluateSafe(constant);
            if (value is not null && !ReferenceEquals(member, comparison.Left))
            {
                comparison = comparison.NodeType switch
                {
                    ExpressionType.GreaterThan => Expression.MakeBinary(ExpressionType.LessThan, comparison.Left, comparison.Right),
                    ExpressionType.GreaterThanOrEqual => Expression.MakeBinary(ExpressionType.LessThanOrEqual, comparison.Left, comparison.Right),
                    ExpressionType.LessThan => Expression.MakeBinary(ExpressionType.GreaterThan, comparison.Left, comparison.Right),
                    ExpressionType.LessThanOrEqual => Expression.MakeBinary(ExpressionType.GreaterThanOrEqual, comparison.Left, comparison.Right),
                    _ => comparison
                };
            }

            string operation = comparison.NodeType switch
            {
                ExpressionType.Equal => "$eq",
                ExpressionType.NotEqual => "$ne",
                ExpressionType.GreaterThan => "$gt",
                ExpressionType.GreaterThanOrEqual => "$gte",
                ExpressionType.LessThan => "$lt",
                ExpressionType.LessThanOrEqual => "$lte",
                _ => throw Unsupported(expression, "unsupported comparison")
            };
            Type rawType = Unwrap(path.Value.Type);
            if (value is not null && rawType == typeof(string) &&
                comparison.NodeType is ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or ExpressionType.LessThan or ExpressionType.LessThanOrEqual)
            {
                throw Unsupported(expression, "ordered string comparison is not supported");
            }

            Type expressionType = StripConvert(member).Type;
            if (value is not null && expressionType != rawType && value.GetType() != expressionType)
            {
                throw Unsupported(expression, "value-object comparison requires an exact value-object type match");
            }

            return new BsonDocument(path.Path, new BsonDocument(operation, ToBson(value, rawType)));
        }

        if (expression is MemberExpression { Member.Name: "HasValue", Expression: not null } hasValue)
        {
            ResolvedPath path = Resolve(hasValue.Expression!, scopes);
            return new BsonDocument(path.Path, new BsonDocument("$ne", BsonNull.Value));
        }

        if (expression is MethodCallExpression call && call.Method.Name == "Any" && call.Arguments.Count == 2 &&
            (call.Method.DeclaringType == typeof(Enumerable) || call.Method.DeclaringType == typeof(Queryable)))
        {
            Expression source = call.Object ?? call.Arguments[0];
            LambdaExpression predicate = call.Arguments
                .Select(argument => StripQuote(argument))
                .OfType<LambdaExpression>()
                .SingleOrDefault() ?? throw Unsupported(expression, "Any requires an element predicate");
            ResolvedPath listPath = Resolve(source, scopes);
            RawRecordNode? element = UnwrapNode(listPath.Value).Element;
            if (element is null)
            {
                throw Unsupported(expression, "Any is only supported for declared raw lists");
            }

            Dictionary<ParameterExpression, QueryScope> nestedScopes = new(scopes)
            {
                [predicate.Parameters[0]] = new QueryScope(predicate.Parameters[0].Type, element,
                    listPath.Path + ".$")
            };
            BsonDocument inner = Boolean(predicate.Body, nestedScopes);
            // $elemMatch fields are relative to the element, not rooted with "$.".
            return new BsonDocument(listPath.Path,
                new BsonDocument("$elemMatch", StripElementPrefix(inner, listPath.Path + ".$.")));
        }

        if (expression is MethodCallExpression contains && TryContains(contains, scopes, out BsonDocument? membership))
        {
            return membership!;
        }

        if (expression is MethodCallExpression equals && TryEquals(equals, scopes, out BsonDocument? equality))
        {
            return equality!;
        }

        if (IsPath(expression))
        {
            ResolvedPath path = Resolve(expression, scopes);
            return new BsonDocument(path.Path, true);
        }

        throw Unsupported(expression, "unsupported predicate expression");
    }

    private bool TryContains(MethodCallExpression call, IReadOnlyDictionary<ParameterExpression, QueryScope> scopes, out BsonDocument? result)
    {
        result = null;
        if (call.Method.Name != "Contains" ||
            (call.Method.DeclaringType != typeof(Enumerable) &&
             (call.Object is null || call.Object.Type == typeof(string) ||
              !call.Object.Type.GetInterfaces().Append(call.Object.Type).Any(type =>
                  type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))))
        {
            return false;
        }

        if (call.Object is not null && call.Arguments.Count == 1 && IsPath(call.Object))
        {
            ResolvedPath listPath = Resolve(call.Object, scopes);
            RawRecordNode? element = UnwrapNode(listPath.Value).Element;
            if (element is null)
            {
                throw Unsupported(call, "aggregate-list Contains requires a declared raw list");
            }

            object? item = EvaluateSafe(call.Arguments[0]);
            result = new BsonDocument(listPath.Path, new BsonDocument("$eq", ToBson(item, UnwrapNode(element).Type)));
            return true;
        }

        Expression? member;
        object? collection;
        if (call.Object is not null && call.Arguments.Count == 1 && IsPath(call.Arguments[0]))
        {
            member = call.Arguments[0];
            collection = EvaluateSafe(call.Object);
        }
        else if (call.Object is null && call.Arguments.Count == 2 && call.Method.DeclaringType == typeof(Enumerable))
        {
            collection = EvaluateSafe(call.Arguments[0]);
            member = call.Arguments[1];
        }
        else
        {
            return false;
        }

        if (!IsPath(member))
        {
            return false;
        }

        ResolvedPath path = Resolve(member, scopes);
        if (collection is not IList enumerable || collection is string)
        {
            throw Unsupported(call, "Contains requires a captured constant set");
        }

        Type rawType = Unwrap(path.Value.Type);
        BsonArray bsonValues = new();
        foreach (object? value in enumerable)
        {
            bsonValues.Add(ToBson(value, rawType));
            if (bsonValues.Count > AggregateQuery<TAggregate>.MaximumTake)
            {
                throw Unsupported(call, "Contains constant set exceeds its supported bound");
            }
        }

        result = new BsonDocument(path.Path, new BsonDocument("$in", bsonValues));
        return true;
    }

    private bool TryEquals(MethodCallExpression call, IReadOnlyDictionary<ParameterExpression, QueryScope> scopes,
        out BsonDocument? result)
    {
        result = null;
        Expression? member = call.Object;
        Expression? value = null;
        if (member is not null && call.Arguments.Count == 1)
        {
            value = call.Arguments[0];
        }
        else if (member is null && call.Method.DeclaringType == typeof(object) && call.Arguments.Count == 2)
        {
            member = call.Arguments[0];
            value = call.Arguments[1];
        }

        if (call.Method.Name != nameof(object.Equals) || member is null || value is null || !IsPath(member))
        {
            return false;
        }

        ResolvedPath path = Resolve(member, scopes);
        object? captured = EvaluateSafe(value);
        Type rawType = Unwrap(path.Value.Type);
        Type expressionType = StripConvert(member).Type;
        if (captured is not null && expressionType != rawType && captured.GetType() != expressionType)
        {
            throw Unsupported(call, "value-object equality requires an exact value-object type match");
        }

        result = new BsonDocument(path.Path, new BsonDocument("$eq", ToBson(captured, rawType)));
        return true;
    }

    private ResolvedPath Resolve(Expression expression, IReadOnlyDictionary<ParameterExpression, QueryScope> scopes)
    {
        expression = StripConvert(expression);
        List<MemberExpression> members = [];
        Expression? current = expression;
        while (current is MemberExpression member)
        {
            members.Add(member);
            current = StripConvert(member.Expression!);
        }

        if (current is not ParameterExpression parameter || !scopes.TryGetValue(parameter, out QueryScope? scope))
        {
            throw Unsupported(expression, "field path must start at an aggregate or list element parameter");
        }

        members.Reverse();
        string path = scope.Path;
        RawRecordNode currentNode = scope.Node;
        MemberInfo? previousMember = null;
        foreach (MemberExpression member in members)
        {
            if (member.Member.Name is "Value" && previousMember is PropertyInfo previousProperty &&
                previousProperty.PropertyType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance) is not null)
            {
                previousMember = member.Member;
                continue;
            }

            if (member.Member.Name is "HasValue" or "Value" && Nullable.GetUnderlyingType(member.Expression!.Type) is not null)
            {
                if (member.Member.Name == "HasValue")
                {
                    return new ResolvedPath(path, currentNode);
                }

                previousMember = member.Member;
                continue;
            }

            RawRecordNode node = UnwrapNode(currentNode);
            RawRecordProperty? property = node.Properties.SingleOrDefault(candidate => candidate.Name == member.Member.Name);
            if (property is null)
            {
                throw Unsupported(expression, $"unmapped field '{member.Member.Name}'");
            }

            path = string.IsNullOrEmpty(path) ? property.StorageName : path + "." + property.StorageName;
            currentNode = property.Value;
            previousMember = member.Member;
        }

        if (string.IsNullOrEmpty(path))
        {
            throw Unsupported(expression, "aggregate root is not a scalar field");
        }

        return new ResolvedPath(path, currentNode);
    }

    private bool IsPath(Expression expression)
    {
        expression = StripConvert(expression);
        while (expression is MemberExpression member)
        {
            expression = StripConvert(member.Expression!);
        }

        return expression is ParameterExpression;
    }

    private static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            Type source = unary.Operand.Type;
            Type destination = unary.Type;
            if (destination != typeof(object) && source != destination &&
                Nullable.GetUnderlyingType(destination) != source)
            {
                throw Unsupported(unary, "numeric, custom and checked conversions are not supported");
            }

            expression = unary.Operand;
        }

        return expression;
    }

    private static Expression StripQuote(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Quote } quote ? quote.Operand : expression;

    private object? EvaluateSafe(Expression expression)
    {
        expression = StripConvert(expression);
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        if (expression is MemberExpression member && member.Member is FieldInfo field)
        {
            object? target = member.Expression is null ? null : EvaluateSafe(member.Expression);
            return field.GetValue(target);
        }

        throw Unsupported(expression, "only constants and captured immutable scalar fields are supported");
    }

    private BsonValue ToBson(object? value, Type rawType)
    {
        if (value is null)
        {
            return BsonNull.Value;
        }

        if (rawType.IsInstanceOfType(value))
        {
            return value switch
            {
                string text => new BsonString(text),
                bool boolean => new BsonBoolean(boolean),
                int integer => new BsonInt32(integer),
                long integer => new BsonInt64(integer),
                double number => new BsonDouble(number),
                decimal number => new BsonDecimal128(number),
                Guid id => new BsonBinaryData(id, GuidRepresentation.Standard),
                DateOnly date => new BsonString(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                DateTime date when date.Kind == DateTimeKind.Utc => new BsonDateTime(date),
                _ =>                 throw _schema!.Failure(rawType.Name, "query constant has no compatible raw codec")
            };
        }

        PropertyInfo? valueProperty = value.GetType() == rawType ? null :
            value.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        if (valueProperty is not null && valueProperty.PropertyType == rawType)
        {
            return ToBson(valueProperty.GetValue(value), rawType);
        }

        throw _schema!.Failure(rawType.Name, "query constant type is incompatible with its raw field codec");
    }

    private static RawRecordNode UnwrapNode(RawRecordNode node) => node.IsNullable ? node.Element! : node;

    private static Type Unwrap(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    private static BsonDocument StripElementPrefix(BsonDocument document, string prefix)
    {
        if (document.ElementCount == 0)
        {
            return document;
        }

        BsonDocument result = new();
        foreach (BsonElement element in document)
        {
            if (element.Name is "$and" or "$or")
            {
                result[element.Name] = new BsonArray(element.Value.AsBsonArray.Select(value =>
                    StripElementPrefix(value.AsBsonDocument, prefix)));
            }
            else
            {
                string name = element.Name.StartsWith(prefix, StringComparison.Ordinal)
                    ? element.Name[prefix.Length..]
                    : element.Name;
                result[name] = element.Value;
            }
        }

        return result;
    }

    private static InvalidOperationException Unsupported(Expression expression, string reason) =>
        new($"Unsupported aggregate query expression '{expression.NodeType}' ({expression.Type.Name}): {reason}.");

    private sealed record QueryScope(Type AggregateType, RawRecordNode Node, string Path);

    internal sealed record ResolvedPath(string Path, RawRecordNode Value);
}

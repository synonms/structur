using System.Collections;
using System.Globalization;
using MongoDB.Bson;
using Synonms.Structur.Infrastructure.Persistence;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisation;

/// <summary>
/// BSON boundary for validated plain records. Uses descriptor bindings, never aggregate/VO
/// serializers or global class-map conventions. Presence is checked externally on BSON before
/// restoration; no BSON/presence state is placed in records.
/// </summary>
/// <remarks>
/// V1 codec contract: PascalCase CLR names (Id maps to _id) unless explicitly overridden;
/// all fields written including null; Standard UUID; Int32/Int64 widths; Double; Decimal128;
/// DateOnly as invariant yyyy-MM-dd; DateTime as UTC BSON milliseconds (Local/Unspecified rejected).
/// Historical schemas with other codecs need a reviewed adapter before migration.
/// </remarks>
public sealed class RawRecordBsonCodec(RawRecordSchema schema)
{
    public BsonDocument Encode(object record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return EncodeValue(schema.Root, record, schema.RecordType.Name).AsBsonDocument;
    }

    public object Decode(BsonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return DecodeValue(schema.Root, document, schema.RecordType.Name);
    }

    private BsonValue EncodeValue(RawRecordNode node, object value, string path)
    {
        if (node.IsNullable)
        {
            return EncodeValue(node.Element!, value, path);
        }

        if (value.GetType() != node.Type)
        {
            throw schema.Failure(path, "runtime type does not match declared raw type; polymorphism is unsupported");
        }

        if (node.Properties.Count != 0)
        {
            BsonDocument document = new();
            foreach (RawRecordProperty property in node.Properties)
            {
                string propertyPath = path + "." + property.Name;
                object? raw = ReadProperty(property, value, propertyPath);
                if (raw is null && !property.AllowNull)
                {
                    throw schema.Failure(propertyPath, "null is not allowed");
                }

                document.Add(property.StorageName, raw is null ? BsonNull.Value : EncodeValue(property.Value, raw, propertyPath));
            }

            return document;
        }

        if (node.Element is not null)
        {
            BsonArray array = new();
            foreach (object? element in (IList)value)
            {
                if (element is null)
                {
                    if (!node.Element.IsNullable)
                    {
                        throw schema.Failure(path + "[]", "null collection element is unsupported");
                    }

                    array.Add(BsonNull.Value);
                }
                else
                {
                    array.Add(EncodeValue(node.Element, element, path + "[]"));
                }
            }

            return array;
        }

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
            DateTime => throw schema.Failure(path, "DateTime codec requires UTC"),
            _ => throw schema.Failure(path, "unsupported raw codec")
        };
    }

    private object DecodeValue(RawRecordNode node, BsonValue value, string path)
    {
        if (node.IsNullable)
        {
            return DecodeValue(node.Element!, value, path);
        }

        if (node.Properties.Count != 0)
        {
            if (value is not BsonDocument document || document.Contains("_t"))
            {
                throw schema.Failure(path, "expected raw document without polymorphic discriminator");
            }

            if (document.Names.Distinct(StringComparer.Ordinal).Count() != document.ElementCount)
            {
                throw schema.Failure(path, "duplicate stored field");
            }

            object result = CreateRecord(node, path);
            foreach (RawRecordProperty property in node.Properties)
            {
                string propertyPath = path + "." + property.Name;
                if (!document.TryGetValue(property.StorageName, out BsonValue raw))
                {
                    if (!property.AllowMissing)
                    {
                        throw schema.Failure(propertyPath, "required field is missing");
                    }

                    WriteProperty(property, result, null, propertyPath);
                    continue;
                }

                if (raw.IsBsonNull)
                {
                    if (!property.AllowNull)
                    {
                        throw schema.Failure(propertyPath, "explicit null is not allowed");
                    }

                    WriteProperty(property, result, null, propertyPath);
                    continue;
                }

                WriteProperty(property, result, DecodeValue(property.Value, raw, propertyPath), propertyPath);
            }

            return result;
        }

        if (node.Element is not null)
        {
            if (value is not BsonArray array)
            {
                throw schema.Failure(path, "expected raw list");
            }

            IList list = (IList)node.Create!();
            foreach (BsonValue element in array)
            {
                if (element.IsBsonNull)
                {
                    if (!node.Element.IsNullable)
                    {
                        throw schema.Failure(path + "[]", "null collection element is unsupported");
                    }

                    list.Add(null);
                }
                else
                {
                    list.Add(DecodeValue(node.Element, element, path + "[]"));
                }
            }

            return list;
        }

        // Never expose driver conversion messages: they can contain persisted values.
        try
        {
            Type type = node.Type;
            if (type == typeof(string) && value.IsString) { return value.AsString; }
            if (type == typeof(bool) && value.IsBoolean) { return value.AsBoolean; }
            if (type == typeof(int) && value.IsInt32) { return value.AsInt32; }
            if (type == typeof(long) && value.IsInt64) { return value.AsInt64; }
            if (type == typeof(double) && value.IsDouble) { return value.AsDouble; }
            if (type == typeof(decimal) && value.IsDecimal128) { return Decimal128.ToDecimal(value.AsDecimal128); }
            if (type == typeof(Guid) && value is BsonBinaryData { SubType: BsonBinarySubType.UuidStandard } binary)
            {
                return binary.ToGuid(GuidRepresentation.Standard);
            }
            if (type == typeof(DateTime) && value.IsBsonDateTime) { return value.ToUniversalTime(); }
            if (type == typeof(DateOnly) && value.IsString &&
                DateOnly.TryParseExact(value.AsString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            {
                return date;
            }
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentException or InvalidOperationException or InvalidCastException)
        {
            throw schema.Failure(path, "malformed/unrepresentable raw value");
        }

        throw schema.Failure(path, "incompatible raw BSON codec");
    }

    private object CreateRecord(RawRecordNode node, string path)
    {
        try
        {
            return node.Create!();
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            throw schema.Failure(path, "raw record construction failed");
        }
    }

    private object? ReadProperty(RawRecordProperty property, object record, string path)
    {
        try
        {
            return property.Get(record);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            throw schema.Failure(path, "raw property getter failed");
        }
    }

    private void WriteProperty(RawRecordProperty property, object record, object? value, string path)
    {
        try
        {
            property.Set(record, value);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            throw schema.Failure(path, "raw property setter failed");
        }
    }
}

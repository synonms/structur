using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Synonms.Structur.Api.Core.Content;
using Synonms.Structur.Api.Core.Serialisation;
using Synonms.Structur.Api.Core.Serialisation.Default;

namespace Synonms.Structur.Api.Server.Hypermedia.Default;

public class DefaultOutputFormatter : TextOutputFormatter
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new ()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { 
            new DateOnlyJsonConverter(),
            new OptionalDateOnlyJsonConverter(),
            new TimeOnlyJsonConverter(),
            new OptionalTimeOnlyJsonConverter(),
            new DefaultCustomJsonConverterFactory(),
            new DefaultLinkJsonConverter(),
            new DefaultFormDocumentJsonConverter(),
            new DefaultFormFieldJsonConverter(),
            new DefaultPaginationJsonConverter(),
            new DefaultErrorCollectionDocumentJsonConverter(),
            new DefaultErrorJsonConverter()
        }
    };

    public DefaultOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(MediaTypes.Any));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(MediaTypes.Json));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(MediaTypes.AspNetCoreError));

        SupportedEncodings.Add(Encoding.UTF8);
    }

    protected override bool CanWriteType(Type? type) => 
        true;

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        string json = JsonSerializer.Serialize(context.Object, JsonSerializerOptions);

        await context.HttpContext.Response.WriteAsync(json, selectedEncoding);
    }
}
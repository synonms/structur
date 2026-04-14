using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Synonms.Structur.Api.Core.Content;
using Synonms.Structur.Api.Core.Serialisation;
using Synonms.Structur.Api.Core.Serialisation.Default;
using Synonms.Structur.Api.Server.Versioning.Context;

namespace Synonms.Structur.Api.Server.Hypermedia.Default;

public class DefaultOutputFormatter : TextOutputFormatter
{
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
        IVersionContext versionContext = context.HttpContext.RequestServices.GetRequiredService<IVersionContext>();

        JsonSerializerOptions jsonSerializerOptions = new ()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { 
                new DateOnlyJsonConverter(),
                new OptionalDateOnlyJsonConverter(),
                new TimeOnlyJsonConverter(),
                new OptionalTimeOnlyJsonConverter(),
                new DefaultCustomJsonConverterFactory(versionContext.Version ?? new Version()),
                new DefaultLinkJsonConverter(),
                new DefaultFormDocumentJsonConverter(),
                new DefaultFormFieldJsonConverter(),
                new DefaultPaginationJsonConverter(),
                new DefaultErrorCollectionDocumentJsonConverter(),
                new DefaultErrorJsonConverter()
            }
        };
            
        string json = JsonSerializer.Serialize(context.Object, jsonSerializerOptions);

        await context.HttpContext.Response.WriteAsync(json, selectedEncoding);
    }
}
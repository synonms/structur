using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.WebApi.Content;
using Synonms.Structur.WebApi.Serialisation;
using Synonms.Structur.WebApi.Serialisation.Default;

namespace Synonms.Structur.WebApi.Hypermedia.Default;

public class DefaultInputFormatter : TextInputFormatter
{
    private readonly ILogger<DefaultInputFormatter> _logger;

    public DefaultInputFormatter(ILogger<DefaultInputFormatter> logger)
    {
        _logger = logger;
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(MediaTypes.Any));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(MediaTypes.Json));

        SupportedEncodings.Add(Encoding.UTF8);
    }

    protected override bool CanReadType(Type type) => 
        type.IsResource();
    
    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        JsonSerializerOptions jsonSerializerOptions = new ()
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

        try
        {
            using TextReader streamReader = context.ReaderFactory(context.HttpContext.Request.Body, encoding);

            string body = await streamReader.ReadToEndAsync();

            object? resource = JsonSerializer.Deserialize(body, context.ModelType, jsonSerializerOptions);

            return await InputFormatterResult.SuccessAsync(resource);
        }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Failed to read request body.");
            return await InputFormatterResult.FailureAsync();
        }
    }
}
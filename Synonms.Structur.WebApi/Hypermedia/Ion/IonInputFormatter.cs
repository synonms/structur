using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.WebApi.Content;
using Synonms.Structur.WebApi.Serialisation;
using Synonms.Structur.WebApi.Serialisation.Ion;

namespace Synonms.Structur.WebApi.Hypermedia.Ion;

public class IonInputFormatter : TextInputFormatter
{
    private readonly ILogger<IonInputFormatter> _logger;

    public IonInputFormatter(ILogger<IonInputFormatter> logger)
    {
        _logger = logger;
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(MediaTypes.Ion));

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
                new IonCustomJsonConverterFactory(),
                new IonLinkJsonConverter(),
                new IonFormDocumentJsonConverter(),
                new IonFormFieldJsonConverter(),
                new IonPaginationJsonConverter()
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
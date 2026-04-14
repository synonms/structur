using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Synonms.Structur.Api.Core.Content;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.Serialisation;
using Synonms.Structur.Api.Core.Serialisation.Ion;
using Synonms.Structur.Api.Server.Versioning.Context;

namespace Synonms.Structur.Api.Server.Hypermedia.Ion;

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
                new IonCustomJsonConverterFactory(versionContext.Version ?? new Version()),
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Synonms.Structur.WebApi.Hypermedia.Ion;

public static class IonMvcOptionsExtensions
{
    public static MvcOptions WithIonFormatters(this MvcOptions mvcOptions, ILoggerFactory loggerFactory)
    {
        mvcOptions.InputFormatters.Add(new IonInputFormatter(loggerFactory.CreateLogger<IonInputFormatter>()));
        mvcOptions.OutputFormatters.Add(new IonOutputFormatter());

        return mvcOptions;
    }
}
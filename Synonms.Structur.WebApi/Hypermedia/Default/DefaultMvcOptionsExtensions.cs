using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Synonms.Structur.WebApi.Hypermedia.Default;

public static class DefaultMvcOptionsExtensions
{
    public static MvcOptions WithDefaultFormatters(this MvcOptions mvcOptions, ILoggerFactory loggerFactory)
    {
        mvcOptions.InputFormatters.Add(new DefaultInputFormatter(loggerFactory.CreateLogger<DefaultInputFormatter>()));
        mvcOptions.OutputFormatters.Add(new DefaultOutputFormatter());

        return mvcOptions;
    }
}
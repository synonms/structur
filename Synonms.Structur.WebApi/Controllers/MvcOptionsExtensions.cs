using Microsoft.AspNetCore.Mvc;

namespace Synonms.Structur.WebApi.Controllers;

public static class MvcOptionsExtensions
{
    public static MvcOptions ConfigureForStructur(this MvcOptions mvcOptions)
    {
        mvcOptions.RespectBrowserAcceptHeader = true;

        return mvcOptions;
    }
    
    public static MvcOptions ClearFormatters(this MvcOptions mvcOptions)
    {
        mvcOptions.InputFormatters.Clear();
        mvcOptions.OutputFormatters.Clear();

        return mvcOptions;
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Api.Core.Schema.Errors;
using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Api.Server.Http;

public static class HttpResponseMapper
{
    public static IActionResult MapFault(Fault fault, ErrorCollectionDocument? errorCollectionDocument = null) => fault switch
    {
        ApplicationRulesFault applicationRulesFault => new BadRequestObjectResult(errorCollectionDocument),
        ApplicationRuleFault applicationRuleFault => new BadRequestObjectResult(errorCollectionDocument),
        DomainRulesFault domainRulesFault => new BadRequestObjectResult(errorCollectionDocument),
        DomainRuleFault domainRuleFault => new BadRequestObjectResult(errorCollectionDocument),
        EntityNotFoundFault entityNotFoundFault => new NotFoundObjectResult(errorCollectionDocument),
        _ => new StatusCodeResult(StatusCodes.Status500InternalServerError) 
    };
}
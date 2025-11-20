using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Application.Faults;
using Synonms.Structur.Application.Schema.Errors;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.WebApi.Http;

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
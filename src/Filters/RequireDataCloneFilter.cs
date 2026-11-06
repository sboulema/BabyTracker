using BabyTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BabyTracker.Filters;

public class RequireDataCloneFilter(IImportService importService) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!importService.HasDataClone(context.HttpContext.User))
        {
            context.Result = new RedirectResult("/import");
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
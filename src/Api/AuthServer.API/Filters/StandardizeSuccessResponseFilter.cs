using AuthServer.Application.ApiResponses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthServer.API.Filters;

public class StandardizeSuccessResponseFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult)
        {
            var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
            if (statusCode == StatusCodes.Status200OK || statusCode == StatusCodes.Status201Created)
            {
                if (!IsWrappedResponse(objectResult.Value))
                {
                    objectResult.Value = new ApiResponse<object?>
                    {
                        StatusCode = statusCode,
                        Success = true,
                        Message = statusCode == StatusCodes.Status201Created ? "Resource created successfully." : "Request processed successfully.",
                        TraceId = context.HttpContext.TraceIdentifier,
                        Data = objectResult.Value
                    };
                }
            }
        }

        return next();
    }

    private static bool IsWrappedResponse(object? value)
    {
        if (value is null)
        {
            return false;
        }

        var type = value.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>);
    }
}

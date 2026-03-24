namespace AuthServer.Application.ApiResponses;

public class ValidationErrorResponse : ApiErrorResponse
{
    public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
}

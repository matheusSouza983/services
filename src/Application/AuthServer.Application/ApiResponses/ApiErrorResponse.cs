namespace AuthServer.Application.ApiResponses;

public class ApiErrorResponse
{
    public int StatusCode { get; set; }
    public bool Success { get; set; } = false;
    public string? ErrorCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }
}

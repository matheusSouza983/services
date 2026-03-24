namespace AuthServer.Application.ApiResponses;

public class ApiResponse<T>
{
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? TraceId { get; set; }
    public T? Data { get; set; }
    public object? Meta { get; set; }
}

namespace eCommerce.Core.DTO
{
    public record ApiErrorResponse(
        string Message,
        IReadOnlyDictionary<string, string[]>? Errors = null,
        string? Type = null);
}

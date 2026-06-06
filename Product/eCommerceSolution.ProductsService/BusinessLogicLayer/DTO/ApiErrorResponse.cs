namespace eCommerce.BusinessLogicLayer.DTO
{
    public record ApiErrorResponse(
        string Message,
        IReadOnlyDictionary<string, string[]>? Errors = null,
        string? Type = null);
}

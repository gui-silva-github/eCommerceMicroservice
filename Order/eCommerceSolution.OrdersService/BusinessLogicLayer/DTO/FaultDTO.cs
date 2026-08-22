namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO
{
    /// <summary>
    /// Payload de fallback quando uma dependency falha.
    /// OrdersService é o dependant; UsersService / ProductsService são as dependencies.
    /// </summary>
    public record FaultDTO(
        string Dependant,
        string Dependency,
        string Message,
        string FaultType,
        DateTime OccurredAtUtc,
        bool UsedFallback = true);
}

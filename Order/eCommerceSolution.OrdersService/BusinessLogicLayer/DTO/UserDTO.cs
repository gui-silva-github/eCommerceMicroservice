namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO
{
    /// <summary>
    /// Contrato espelhado do UsersService para comunicação síncrona via HttpClient.
    /// </summary>
    public record UserDTO(
        Guid UserID,
        string? Email,
        string? PersonName,
        string? Gender
    )
    {
        public UserDTO() : this(default, default, default, default)
        {
        }
    }
}

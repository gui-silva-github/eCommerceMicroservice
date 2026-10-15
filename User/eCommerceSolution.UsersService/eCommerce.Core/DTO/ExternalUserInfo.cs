namespace eCommerce.Core.DTO
{
    /// <summary>
    /// Dados do usuário vindos do access token emitido pelo Microsoft Entra External ID.
    /// O <see cref="ObjectId"/> é a claim <c>oid</c>: identificador estável do usuário no tenant externo,
    /// e passa a ser o <c>UserID</c> no Postgres (é ele que o Orders valida em /api/Users/{userID}).
    /// </summary>
    public record ExternalUserInfo(
        Guid ObjectId,
        string? Email,
        string? PersonName,
        string? Gender
    );
}

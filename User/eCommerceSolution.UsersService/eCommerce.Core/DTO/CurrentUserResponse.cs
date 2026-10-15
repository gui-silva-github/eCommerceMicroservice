namespace eCommerce.Core.DTO
{
    /// <summary>
    /// Resposta de GET /api/Users/me. O Angular usa para montar a sessão
    /// (nome, UserID para o checkout e se mostra o menu de administração).
    /// <see cref="IsAdmin"/> vem do app role "Admin" no token, nunca do frontend.
    /// </summary>
    public record CurrentUserResponse(
        Guid UserID,
        string? Email,
        string? PersonName,
        string? Gender,
        bool IsAdmin
    );
}

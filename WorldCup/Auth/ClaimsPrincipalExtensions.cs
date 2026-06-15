using System.Security.Claims;

namespace WorldCup.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id do usuario autenticado a partir do token JWT.</summary>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var value)
            ? value
            : throw new UnauthorizedAccessException("Usuario nao autenticado.");
    }

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole("Admin");
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WorldCup.Auth;
using WorldCup.Domain.Entities;
using WorldCup.Dtos;
using WorldCup.Infrastructure;

namespace WorldCup.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, ITokenService tokens, IConfiguration config)
    {
        _db = db;
        _tokens = tokens;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        // Bolao fechado: cadastro desabilitado por padrao (Auth:AllowRegistration).
        if (!_config.GetValue("Auth:AllowRegistration", false))
            return StatusCode(403, new { message = "Cadastro encerrado: o bolao e fechado aos participantes convidados." });

        if (string.IsNullOrWhiteSpace(req.Nome) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Senha))
            return BadRequest(new { message = "Nome, email e senha sao obrigatorios." });
        if (req.Senha.Length < 6)
            return BadRequest(new { message = "A senha deve ter ao menos 6 caracteres." });

        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Email ja cadastrado." });

        var user = new User
        {
            Nome = req.Nome.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Senha),
            IsAdmin = false,
            Pago = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(_tokens.GenerateToken(user), ToDto(user)));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email = (req.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Senha, user.PasswordHash))
            return Unauthorized(new { message = "Email ou senha invalidos." });

        return Ok(new AuthResponse(_tokens.GenerateToken(user), ToDto(user)));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _db.Users.FindAsync(User.GetUserId());
        return user is null ? NotFound() : Ok(ToDto(user));
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Nome, u.Email, u.IsAdmin, u.Pago);
}

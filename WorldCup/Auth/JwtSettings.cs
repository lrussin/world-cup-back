namespace WorldCup.Auth;

/// <summary>Configuracao do JWT lida de appsettings ("Jwt"). A chave deve vir de variavel de ambiente em producao.</summary>
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "WorldCupBolao";
    public string Audience { get; set; } = "WorldCupBolaoClient";
    public int ExpiresMinutes { get; set; } = 480;
}

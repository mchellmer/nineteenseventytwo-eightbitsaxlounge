using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;

/// <summary>
/// Defines authentication endpoints for the web application.
/// Issues JWT tokens for valid client credentials.
/// </summary>
public static class AuthenticationEndpoints
{
    /// <summary>
    /// Adds the authentication endpoints to the web application.
    /// </summary>
    /// <param name="app"></param>
    public static void AddAuthenticationEndpoints(this WebApplication app)
    {
        app.MapPost("/api/token",
            ([FromServices] IConfiguration config,
             [FromServices] ILoggerFactory lf,
             [FromBody] ClientCredentialRequest req) =>
            {
                var logger = lf.CreateLogger("Auth.Token");
                var opts = config.GetSection("Authentication").Get<AppAuthenticationOptions>();
                if (opts is null)
                {
                    logger.LogError("Authentication configuration missing");
                    return Results.Problem("Auth configuration missing.");
                }
                logger.LogInformation("Token request received for clientId={ClientId}. Loaded {Count} clients from configuration.", req.ClientId, opts.Clients.Count);

                var client = opts.Clients.FirstOrDefault(c =>
                    ConstantTimeEquals(c.ClientId, req.ClientId) &&
                    ConstantTimeEquals(c.ClientSecret, req.ClientSecret));

                if (client is null)
                {
                    logger.LogWarning("Invalid credentials for clientId={ClientId}", req.ClientId);
                    return Results.Unauthorized();
                }

                var token = GenerateToken(opts, client.ClientId);
                logger.LogInformation("Issued token for clientId={ClientId}", client.ClientId);
                return Results.Ok(new { access_token = token, token_type = "Bearer" });
            })
            .AllowAnonymous();
    }

    /// <summary>
    /// Generates a JWT token for the given client ID.
    /// </summary>
    /// <param name="opts">
    /// The authentication options containing issuer, audience, secret key, and token duration.
    /// </param>
    /// <param name="clientId">
    /// The client ID for which the token is being generated.
    /// </param>
    /// <returns>
    /// The generated JWT token as a string.
    /// </returns>
    private static string GenerateToken(AppAuthenticationOptions opts, string clientId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            new Claim("client_id", clientId)
        };
        var jwt = new JwtSecurityToken(
            issuer: opts.Issuer,
            audience: opts.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(opts.TokenExpiryInMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    /// <summary>
    /// Compares two strings in constant time to prevent timing attacks.
    /// </summary>
    /// <param name="a">
    /// The first string to compare.
    /// </param>
    /// <param name="b">
    /// The second string to compare.
    /// </param>
    /// <returns>
    /// True if the strings are equal; otherwise, false.
    /// </returns>
    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var result = 0;
        for (int i = 0; i < a.Length; i++)
            result |= a[i] ^ b[i];
        return result == 0;
    }
}
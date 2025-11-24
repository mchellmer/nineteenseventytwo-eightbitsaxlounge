using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;

//TODO: dev only - replace with real auth system
public static class AuthenticationEndpoints
{
    public static void AddAuthenticationEndpoints(this WebApplication app)
    {
        app.MapPost("/api/token", (IConfiguration config, [FromBody] AuthenticationData data) =>
        {
            var user = ValidateCredentials(data);
            
            if (user == null)
            {
                return Results.Unauthorized();
            }
            
            var token = GenerateToken(config, user);

            return Results.Ok(token);
        })
        .AllowAnonymous();
    }
    
    private static string GenerateToken(IConfiguration config, UserData user)
    {
        var secretKey = new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes(
                config.GetValue<string>("Authentication:SecretKey")));
        
        var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
        
        List<Claim> claims = new()
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
        };
        
        var token = new JwtSecurityToken(
            issuer: config.GetValue<string>("Authentication:Issuer"),
            audience: config.GetValue<string>("Authentication:Audience"),
            claims: claims,
            notBefore: DateTime.Now,
            expires: DateTime.Now.AddMinutes(1),
            signingCredentials: signingCredentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserData? ValidateCredentials(AuthenticationData data)
    {
        // Not production code - replace with auth system
        if (CompareValues(data.UserName, "admin") && CompareValues(data.Password, "password123"))
        {
            return new UserData(1, "Admin", "User", "admin");
        }
        
        if (CompareValues(data.UserName, "user") && CompareValues(data.Password, "password123"))
        {
            return new UserData(2, "Normal", "User", "user");
        }
        
        return null;
    }
    
    private static bool CompareValues(string? actual, string? expected)
    {
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }
}
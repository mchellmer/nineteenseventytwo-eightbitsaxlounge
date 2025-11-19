using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure logging providers and default minimum level so all typed ILogger<T> work consistently
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Explicitly register logging services (CreateBuilder does this by default, but this makes it explicit)
builder.Services.AddLogging();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// use localhost key and cert for development

// Api model and SSL
builder.Services.AddEndpointsApiExplorer();

// Docs
builder.Services.AddSwaggerGen(opts =>
{
    // Setup swagger to use authentication token generated
    var securityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Scheme = "bearer",
        Type = SecuritySchemeType.Http
    };
    var securityRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new string[] { }
        }
    };
    opts.AddSecurityDefinition("Bearer", securityScheme);
    opts.AddSecurityRequirement(securityRequirement);
});

// Inject models
builder.Services.AddSingleton<IDataAccess, EightbitSaxLoungeDataAccess>();
builder.Services.AddSingleton<IEffectActivatorFactory, EffectActivatorFactory>();
builder.Services.AddSingleton<IEffectActivator, VentrisDualReverbActivator>();
builder.Services.AddSingleton<IMidiDeviceService, WinmmMidiDeviceService>();
builder.Services.AddSingleton<IMidiDataService, EightBitSaxLoungeMidiDataService>();
builder.Services.AddTransient<MidiEndpointsHandler>();

// Auth
builder.Services.AddAuthorization(opts =>
{
    opts.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Read JWT secret
var jwtKey = builder.Configuration.GetValue<string>("Authentication:SecretKey");
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Missing configuration 'Authentication:SecretKey'. Set via user-secrets for Development or environment variables for production.");
}

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration.GetValue<string>("Authentication:Issuer"),
            ValidAudience = builder.Configuration.GetValue<string>("Authentication:Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.AddAuthenticationEndpoints();
app.AddMidiEndpoints();

app.Run();
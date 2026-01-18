using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Services;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Services.AddLogging();

// Authentication
var authOptions = builder.Configuration.GetSection("Authentication").Get<AppAuthenticationOptions>()
                  ?? throw new InvalidOperationException("Missing 'Authentication' configuration section.");
if (string.IsNullOrWhiteSpace(authOptions.SecretKey))
    throw new InvalidOperationException("Missing 'Authentication:SecretKey'. Set via user-secrets or environment variable.");
builder.Services.Configure<AppAuthenticationOptions>(builder.Configuration.GetSection("Authentication"));

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
            []
        }
    };
    opts.AddSecurityDefinition("Bearer", securityScheme);
    opts.AddSecurityRequirement(securityRequirement);
});

// Inject models
builder.Services.AddSingleton<IDataAccess, EightbitSaxLoungeDataAccess>();
builder.Services.AddSingleton<IEffectActivatorFactory, EffectActivatorFactory>();
builder.Services.AddSingleton<IEffectActivator, VentrisDualReverbActivator>();
builder.Services.AddSingleton<IMidiDataService, EightBitSaxLoungeMidiDataService>();

// HTTP Client for device proxy (conditionally registered)
var deviceServiceUrl = builder.Configuration["MidiDeviceService:Url"];
if (!string.IsNullOrWhiteSpace(deviceServiceUrl))
{
    builder.Services.AddHttpClient<MidiDeviceProxyService>();
    builder.Services.AddSingleton<MidiDeviceProxyService>();
}
else
{
    // Local Windows service with direct device access
    builder.Services.AddSingleton<IMidiOutDeviceFactory, MidiOutDeviceFactory>();
    builder.Services.AddSingleton<IMidiDeviceService, WinmmMidiDeviceService>();
}

// Register handlers that depend on IMidiDeviceService
builder.Services.AddTransient<MidiEndpointsHandler>();

// Auth
builder.Services.AddAuthorization(opts =>
{
    opts.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SecretKey));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = authOptions.Issuer,
            ValidAudience = authOptions.Audience,
            IssuerSigningKey = signingKey
        };
    });

// Build app
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

app.AddHealthEndpoints();
app.AddAuthenticationEndpoints();
app.AddMidiEndpoints();

app.Run();
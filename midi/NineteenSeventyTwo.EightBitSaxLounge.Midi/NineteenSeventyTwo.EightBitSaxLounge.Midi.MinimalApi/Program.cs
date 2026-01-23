using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

using System.Text;
using System.Net;
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

// API and HTTP services
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();

// MIDI Device Service - handles both local and remote (proxy) requests
// If MidiDeviceService:Url is configured, it will proxy remote requests, otherwise use local devices
builder.Services.AddSingleton<IMidiOutDeviceFactory, MidiOutDeviceFactory>();

var deviceServiceUrl = builder.Configuration["MidiDeviceService:Url"];

if (!string.IsNullOrWhiteSpace(deviceServiceUrl))
{
    // Configure HttpClient for proxy with certificate validation handling
    builder.Services.AddHttpClient<WinmmMidiDeviceService>()
        .ConfigureHttpClient(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "NineteenSeventyTwo.EightBitSaxLounge.Midi");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                // Skip certificate validation for development/localhost
                ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                {
                    // If development and localhost, accept any certificate
                    if (builder.Environment.IsDevelopment())
                    {
                        var host = request?.RequestUri?.Host;
                        if (host == "localhost" || host == "127.0.0.1")
                        {
                            return true;
                        }
                    }
                    // Otherwise validate normally
                    return errors == System.Net.Security.SslPolicyErrors.None;
                }
            };
            return handler;
        });
    
    builder.Services.AddSingleton<IMidiDeviceService>(sp => 
        ActivatorUtilities.CreateInstance<WinmmMidiDeviceService>(sp, sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<IHttpContextAccessor>()));
}
else
{
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

// Bypass authentication middleware for requests with valid bypass key
var bypassKey = builder.Configuration["MidiDeviceService:BypassKey"];
if (!string.IsNullOrWhiteSpace(bypassKey))
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Headers.TryGetValue("X-Bypass-Key", out var headerValue) &&
            headerValue == bypassKey)
        {
            // Mark request as authenticated to bypass JWT validation
            var claims = new[] { new System.Security.Claims.Claim("bypass", "true") };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "BypassKey");
            context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        }
        await next();
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.AddHealthEndpoints();
app.AddAuthenticationEndpoints();
app.AddMidiEndpoints();

app.Run();
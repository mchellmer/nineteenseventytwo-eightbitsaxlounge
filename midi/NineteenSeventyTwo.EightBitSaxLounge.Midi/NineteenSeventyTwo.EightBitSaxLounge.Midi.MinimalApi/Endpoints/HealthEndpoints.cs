namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;

public static class HealthEndpoints
{
    public static void AddHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        }))
        .WithName("Health")
        .WithTags("Health")
        .AllowAnonymous();
    }
}

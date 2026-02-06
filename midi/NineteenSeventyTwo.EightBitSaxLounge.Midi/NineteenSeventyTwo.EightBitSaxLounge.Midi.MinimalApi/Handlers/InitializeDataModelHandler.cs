using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

public class InitializeDataModelHandler : IMidiEndpointHandler<IResult>
{
    private readonly ILogger<InitializeDataModelHandler> _logger;
    private readonly IMidiDataService _midiDataService;
    private readonly IOptions<DatabaseOptions> _databaseOptions;

    public InitializeDataModelHandler(
        ILogger<InitializeDataModelHandler> logger,
        IMidiDataService midiDataService,
        IOptions<DatabaseOptions> databaseOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _midiDataService = midiDataService ?? throw new ArgumentNullException(nameof(midiDataService));
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));
    }

    public async Task<IResult> HandleAsync()
    {
        _logger.LogInformation("Initializing MIDI data model");

        var databases = _databaseOptions.Value.Names;

        foreach (var db in databases)
        {
            try
            {
                _logger.LogInformation("Initializing database: {Database}", db);
                await _midiDataService.CreateDatabaseAsync(db);
                _logger.LogInformation("Database initialized: {Database}", db);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error initializing database: {Database}", db);
                return Results.Problem(
                    detail: $"Failed to initialize database '{db}': {e.Message}",
                    title: "Data model initialization failed",
                    statusCode: 500);
            }
        }

        return Results.Ok(new { Message = $"MIDI data model initialized successfully. Databases created: {string.Join(", ", databases)}" });
    }
}

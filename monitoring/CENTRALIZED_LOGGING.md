# Centralized Logging Configuration

This document describes the centralized logging setup across all layers of the application, ensuring consistent log formatting with automatic prefixes and correlation ID tracking.

## Architecture

### Request Flow
```
Discord User → [ui] → [midi] → [data] → CouchDB
                ↓        ↓         ↓
         correlationID passes through each layer
```

### Log Format
All layers follow this format:
```
[component] operation=xxx key=value status=xxx correlationID=xxx
```

Examples:
```
[ui] operation=handle_command command=engine user=streamer status=success correlationID=123e4567-e89b-12d3-a456-426614174000
[midi] operation=set_effect device=VentrisDualReverb effect=ReverbEngineA status=success correlationID=123e4567-e89b-12d3-a456-426614174000
[data] operation=get_document db=devices id=ventris status=success correlationID=123e4567-e89b-12d3-a456-426614174000
```

## UI Layer (Python)

### Configuration

The UI layer generates correlation IDs and passes them downstream.

**File: `ui/src/config/logging_config.py`**

Provides:
- Automatic `[ui]` prefix for all log messages
- Correlation ID generation and context management
- Centralized format configuration

### Usage

**1. Configure logging on startup** (`main.py`):
```python
from config.logging_config import configure_logging

# Configure with automatic [ui] prefix
configure_logging(settings.log_level)
logger = logging.getLogger(__name__)
```

**2. Use correlation IDs in command handlers**:
```python
from config.logging_config import get_correlation_id, set_correlation_id
import logging

logger = logging.getLogger(__name__)

async def handle_command(command, context):
    # Generate correlation ID for this request
    correlation_id = get_correlation_id()
    
    # Log normally - [ui] prefix and correlationID added automatically
    logger.info("operation=handle_command command=%s user=%s status=started", 
                command, context.author.name)
    
    try:
        # Make request to MIDI layer - correlation ID passed automatically
        result = await midi_client.set_effect(...)
        
        logger.info("operation=handle_command command=%s status=success", command)
        return result
    except Exception as e:
        logger.error("operation=handle_command command=%s status=error error=%s", 
                    command, str(e))
        raise
```

**3. MidiClient automatically includes correlation ID**:
```python
# No changes needed - midi_client.py already includes correlation ID in headers:
headers = {
    'X-Correlation-ID': get_correlation_id()
}
```

### Example Output
```
2026-02-12 10:30:15.123 - INFO - [ui] operation=handle_command command=engine user=streamer status=started correlationID=123e4567-e89b-12d3-a456-426614174000
2026-02-12 10:30:15.456 - INFO - [ui] operation=handle_command command=engine status=success correlationID=123e4567-e89b-12d3-a456-426614174000
```

## MIDI Layer (C#)

### Configuration

The MIDI layer extracts correlation IDs from requests and passes them to the data layer.

**Files:**
- `Middleware/CorrelationIdMiddleware.cs` - Extracts/generates correlation IDs
- `Logging/MidiLogFormatter.cs` - Automatic `[midi]` prefix

### Setup

**1. Update `Program.cs`**:
```csharp
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Middleware;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging with [midi] prefix
builder.Logging.ConfigureMidiLogging();

var app = builder.Build();

// Add correlation ID middleware
app.UseMiddleware<CorrelationIdMiddleware>();

// ... rest of configuration
```

**2. Update handlers to include correlation ID**:
```csharp
public class SetEffectHandler : IEndpointHandler<SetEffectRequest, IResult>
{
    private readonly ILogger<SetEffectHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<IResult> HandleAsync(SetEffectRequest request)
    {
        var correlationId = _httpContextAccessor.HttpContext?.GetCorrelationId();
        
        _logger.LogInformation(
            "operation=set_effect device={DeviceName} effect={EffectName} status=started correlationID={CorrelationId}",
            request.DeviceName, request.DeviceEffectName, correlationId);

        try
        {
            // Call data layer with correlation ID
            var result = await ProcessRequest(request, correlationId);
            
            _logger.LogInformation(
                "operation=set_effect device={DeviceName} status=success correlationID={CorrelationId}",
                request.DeviceName, correlationId);
            
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "operation=set_effect device={DeviceName} status=error error={Error} correlationID={CorrelationId}",
                request.DeviceName, ex.Message, correlationId);
            throw;
        }
    }
}
```

**3. Pass correlation ID to data layer requests**:
```csharp
private async Task<HttpResponseMessage> CallDataLayer(string endpoint, string correlationId)
{
    var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
    request.Headers.Add("X-Correlation-ID", correlationId);
    
    return await _httpClient.SendAsync(request);
}
```

### Example Output
```
2026-02-12 10:30:15.234 [Information] [midi] Request started: POST /api/Midi/SetEffect correlationID=123e4567-e89b-12d3-a456-426614174000
2026-02-12 10:30:15.345 [Information] [midi] operation=set_effect device=VentrisDualReverb status=success correlationID=123e4567-e89b-12d3-a456-426614174000
```

## Data Layer (Go)

### Configuration

The data layer extracts correlation IDs from requests (already implemented via middleware).

**Files:**
- `go/logger.go` - Centralized logger with automatic `[data]` prefix
- `go/correlation.go` - Correlation ID middleware (already exists)

### Usage

**Option 1: Use package-level functions** (recommended for simple cases):
```go
import (
    "go-couchdb-api/go"
)

func GetDocumentHandler(w http.ResponseWriter, r *http.Request) {
    correlationID := gofiles.GetCorrelationID(r.Context())
    
    gofiles.Info("operation=get_document db=%s id=%s status=started correlationID=%s", 
                 dbname, id, correlationID)
    
    doc, err := svc.GetDocument(dbname, id)
    if err != nil {
        gofiles.Error("operation=get_document status=error error=%v correlationID=%s", 
                      err, correlationID)
        return
    }
    
    gofiles.Info("operation=get_document db=%s id=%s status=success correlationID=%s",
                 dbname, id, correlationID)
}
```

**Option 2: Use Logger instance** (for more control):
```go
import (
    "go-couchdb-api/go"
)

var logger = gofiles.NewLogger()

func ProcessRequest() {
    logger.Info("operation=process status=started correlationID=%s", correlationID)
    logger.Error("operation=process status=error error=%v correlationID=%s", err, correlationID)
}
```

**Option 3: Replace standard log calls** (for existing code):
```go
import (
    gofiles "go-couchdb-api/go"
)

func ExistingHandler() {
    // OLD: log.Printf("processing request")
    // NEW: Just import the logger package and use Printf
    gofiles.Printf("operation=process status=started correlationID=%s", correlationID)
}
```

### Example Output
```
2026/02/12 10:30:15 [data] operation=get_document db=devices id=ventris status=success correlationID=123e4567-e89b-12d3-a456-426614174000
```

## Grafana LogQL Queries

### Track a specific operation flow
```logql
{namespace=~"eightbitsaxlounge-(dev|prod)"} 
|~ "correlationID=123e4567-e89b-12d3-a456-426614174000"
```

### All UI layer logs
```logql
{namespace=~"eightbitsaxlounge-(dev|prod)"} 
|~ "\\[ui\\]"
```

### Error logs across all layers
```logql
{namespace=~"eightbitsaxlounge-(dev|prod)"} 
|~ "status=error"
| label_format level="ERROR"
```

### Specific operation across all layers
```logql
{namespace=~"eightbitsaxlounge-(dev|prod)"} 
|~ "operation=set_effect"
```

## Benefits

1. **Centralized Configuration**: Prefix is set once, applied everywhere
2. **Consistent Format**: All layers use the same structured logging pattern
3. **Automatic Correlation**: IDs flow through the entire request chain
4. **Easy Filtering**: Component prefixes make Grafana queries simple
5. **No Manual Prefixing**: Developers don't need to remember to add [component]
6. **Correlation Tracking**: Follow a single request through all layers

## Migration Checklist

### UI Layer
- [x] Create `config/logging_config.py`
- [x] Update `main.py` to use `configure_logging()`
- [x] Update `midi_client.py` to pass correlation IDs
- [ ] Update all command handlers to remove manual `[ui]` prefixes (automatic now)
- [ ] Test correlation ID generation and forwarding

### MIDI Layer
- [x] Create `Middleware/CorrelationIdMiddleware.cs`
- [x] Create `Logging/MidiLogFormatter.cs`
- [ ] Update `Program.cs` to use middleware and formatter
- [ ] Update all handlers to:
  - Remove manual `[midi]` prefixes
  - Include correlation IDs in logs
  - Pass correlation IDs to data layer
- [ ] Test correlation ID extraction and forwarding

### Data Layer
- [x] Create `go/logger.go`
- [x] Correlation ID middleware exists (`go/correlation.go`)
- [x] Update `go/handlers.go` to use new logger (currently using manual prefixes)
- [ ] Test correlation ID extraction from headers
- [ ] Verify all logs have [data] prefix automatically

### Testing
- [ ] Test full request flow: UI → MIDI → DATA
- [ ] Verify correlation ID appears in all layers
- [ ] Confirm Grafana can track requests by correlation ID
- [ ] Validate log format consistency across all services

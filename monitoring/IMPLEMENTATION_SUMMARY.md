# Centralized Logging Implementation Summary

## What's Been Implemented

### ✅ UI Layer (Python)
**Files Created:**
- `ui/src/config/logging_config.py` - Centralized logging with automatic `[ui]` prefix and correlation ID support

**Files Updated:**
- `ui/src/main.py` - Uses new `configure_logging()` function
- `ui/src/services/midi_client.py` - Automatically includes `X-Correlation-ID` header in all requests to MIDI layer

**Features:**
- Automatic `[ui]` prefix on all log messages
- Correlation ID generation using UUID
- Context-based correlation ID storage (per async request)
- Correlation IDs passed to MIDI layer via HTTP headers

**Log Format:**
```
2026-02-12 10:30:15.123 - INFO - [ui] operation=handle_command command=engine status=success correlationID=123e4567-e89b-12d3-a456-426614174000
```

---

### ✅ Data Layer (Go)
**Files Created:**
- `data/go/logger.go` - Centralized logger with automatic `[data]` prefix
- `data/go/correlation.go` - Middleware for correlation ID extraction (already existed)
- `data/go/correlation_test.go` - Tests for correlation ID middleware

**Files Updated:**
- `data/go/handlers.go` - All handlers now use centralized logger functions (`Info()`, `Error()`) instead of `log.Printf()`

**Features:**
- Automatic `[data]` prefix on all log messages
- Package-level functions: `Info()`, `Error()`, `Warn()`, `Debug()`, `Printf()`
- Logger instance methods for more control: `logger.Info()`, etc.
- Correlation ID middleware extracts from `X-Correlation-ID` header
- All tests pass ✅

**Log Format:**
```
2026/02/12 10:30:15 [data] operation=get_document db=devices id=ventris status=success correlationID=123e4567-e89b-12d3-a456-426614174000
```

---

### ✅ MIDI Layer (C#) - Files Created
**Files Created:**
- `midi/.../Middleware/CorrelationIdMiddleware.cs` - Extracts/generates correlation IDs, adds to response headers
- `midi/.../Logging/MidiLogFormatter.cs` - Custom console formatter with `[midi]` prefix

**Not Yet Integrated:**
- These files need to be registered in `Program.cs`
- Handlers need to be updated to use correlation IDs
- HttpClient needs to pass correlation IDs to data layer

---

### ✅ Documentation
**Files Created:**
- `monitoring/CENTRALIZED_LOGGING.md` - Complete guide for all three layers with examples, usage patterns, and migration checklist

---

## Request Flow Architecture

```
Discord User → !engine command
        ↓
[UI Layer - Python]
  - Generates correlationID: 123e4567-e89b-12d3-a456-426614174000
  - Logs: [ui] operation=handle_command correlationID=123e4567...
  - Sends HTTP request to MIDI with X-Correlation-ID header
        ↓
[MIDI Layer - C#]  
  - Extracts correlationID from request header
  - Logs: [midi] operation=set_effect correlationID=123e4567...
  - Sends HTTP request to DATA with X-Correlation-ID header
        ↓
[DATA Layer - Go]
  - Extracts correlationID from request header
  - Logs: [data] operation=get_document correlationID=123e4567...
        ↓
CouchDB
```

## Log Format Standard

All layers follow this pattern:
```
[component] operation=xxx key=value status=xxx correlationID=xxx
```

**Examples:**
```
[ui] operation=handle_command command=engine user=streamer status=success correlationID=123e4567-e89b-12d3-a456-426614174000
[midi] operation=set_effect device=VentrisDualReverb effect=ReverbEngineA status=success correlationID=123e4567-e89b-12d3-a456-426614174000
[data] operation=get_document db=devices id=ventris status=success correlationID=123e4567-e89b-12d3-a456-426614174000
```

## Benefits

### 1. **Centralized Configuration**
- Set prefix once, applies everywhere
- No manual `[component]` prefixing needed
- Consistent format across all services

### 2. **Automatic Correlation Tracking**
- UI generates ID → passes to MIDI → passes to DATA
- Track entire request flow through logs
- Easy debugging of multi-service operations

### 3. **Easy Grafana Queries**
```logql
# Find all logs for a specific request
{namespace=~"eightbitsaxlounge-(dev|prod)"} 
|~ "correlationID=123e4567-e89b-12d3-a456-426614174000"

# All UI layer logs
{namespace=~"eightbitsaxlounge-(dev|prod)"} |~ "\\[ui\\]"

# All errors across all layers
{namespace=~"eightbitsaxlounge-(dev|prod)"} |~ "status=error"
```

### 4. **Clean Code**
```go
// Before:
log.Printf("[data] operation=get_document db=%s id=%s status=success correlationID=%s", ...)

// After:
Info("operation=get_document db=%s id=%s status=success correlationID=%s", ...)
```

## What Still Needs to Be Done

### MIDI Layer (C#)
1. Update `Program.cs`:
   ```csharp
   builder.Logging.ConfigureMidiLogging();
   app.UseMiddleware<CorrelationIdMiddleware>();
   ```

2. Update handlers to:
   - Extract correlation ID: `var correlationId = _httpContextAccessor.HttpContext?.GetCorrelationId();`
   - Include in logs: `_logger.LogInformation("operation=set_effect status=success correlationID={CorrelationId}", correlationId);`
   - Pass to data layer in HTTP requests

3. Configure HttpClient to include `X-Correlation-ID` header

### UI Layer (Python)
1. Update command handlers to use correlation IDs in logs
2. Remove any manual `[ui]` prefixes (now automatic)
3. Test correlation ID generation and propagation

### All Layers
1. Deploy and test end-to-end flow
2. Verify correlation IDs appear in Grafana
3. Confirm log filtering works (health checks dropped by Alloy)

## Testing

All Go tests pass ✅ (35 tests including 6 correlation ID tests)

See test output showing automatic `[data]` prefix working correctly:
```
2026/02/12 11:03:19 [data] operation=get_document db=testdb id=123 status=success correlationID=
```

(Note: correlationID is empty in unit tests since they don't use middleware, but will be populated in actual requests)

# Request Flow Tracking and Targeted Logging

This document explains how to track operations across services using correlation IDs and configured log filtering.

## Health Check Log Filtering - IMPLEMENTED ✅

Health check logs are filtered out at the Alloy collector level (before sending to Loki), saving bandwidth and storage.

**Configuration:** [monitoring/k8s-monitoring.yaml](k8s-monitoring.yaml)

```yaml
podLogs:
  enabled: true
  extraStages: |-
    // Drop health check logs before sending to Loki
    - match:
        selector: '{namespace=~"eightbitsaxlounge-(dev|prod)"}'
        stages:
          - drop:
              expression: '.*(/health|/ready|/_up).*'
```

**To apply changes:**
```bash
cd server
ansible-playbook -i ansible-hosts ../monitoring/k8s-monitoring.yaml --ask-vault-pass
```

This filters out:
- `/health` - Application health checks
- `/ready` - Kubernetes readiness probes  
- `/_up` - CouchDB health endpoint

**Note:** Applications log all requests normally. The filtering happens at the Alloy collector, not in the app code. This keeps the architecture clean and centralized.

## Architecture

Each request flow gets a unique correlation ID that's passed through all services:
```
[UI] !engineroom → correlationID=abc123
  ↓ X-Correlation-ID: abc123
[MIDI] receives request → logs with correlationID=abc123
  ↓ X-Correlation-ID: abc123  
[DATA] processes → logs with correlationID=abc123
  ↓ (DB logs can't include correlatoinID, but timestamp correlation works)
[DB] CouchDB logs with timestamp
```

## Implementation by Layer

### ✅ Data Layer (Go) - IMPLEMENTED

Files changed:
- `data/go/correlation.go` - New middleware
- `data/go/routes.go` - Added correlationID middleware and health check filter
- `data/go/handlers.go` - Updated logging format
- `data/go.mod` - Added google/uuid dependency

Run after pulling changes:
```bash
cd data
go mod tidy
```

Log format example:
```
[data] correlationID=abc123 operation=get_document db=devices id=VentrisDualReverb status=success
```

### MIDI Layer (C#/.NET) - TO IMPLEMENT

**1. Add NuGet package:**
```bash
dotnet add package System.Diagnostics.DiagnosticSource
```

**2. Create middleware** `CorrelationIdMiddleware.cs`:
```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        context.Response.Headers.Add(CorrelationIdHeader, correlationId);
        context.Items["CorrelationId"] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
```

**3. Register in `Program.cs`:**
```csharp
// Add before MapControllers
app.UseMiddleware<CorrelationIdMiddleware>();

// Filter health check logs
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/health"), 
    appBuilder => {
        appBuilder.UseHttpLogging();
    });
```

**4. Add to HTTP client requests** (when calling data layer):
```csharp
var correlationId = httpContext.Items["CorrelationId"]?.ToString();
if (!string.IsNullOrEmpty(correlationId))
{
    httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
}
```

**5. Update logging** in controllers:
```csharp
var correlationId = HttpContext.Items["CorrelationId"];
_logger.LogInformation("[midi] correlationId={CorrelationId} operation=set_effect device={Device} status=start", 
    correlationId, deviceName);

// ... do work ...

_logger.LogInformation("[midi] correlationId={CorrelationId} operation=send_cc device={Device} cc={CC} value={Value}", 
    correlationId, deviceName, ccNumber, value);
```

### UI Layer (Python) - TO IMPLEMENT

**1. Create correlation ID middleware** `correlation.py`:
```python
import uuid
from contextvars import ContextVar

correlation_id_var: ContextVar[str] = ContextVar('correlation_id', default='')

class CorrelationIdMiddleware:
    def __init__(self, app):
        self.app = app
    
    async def __call__(self, scope, receive, send):
        if scope['type'] == 'http':
            headers = dict(scope['headers'])
            correlation_id = headers.get(b'x-correlation-id', b'').decode()
            
            if not correlation_id:
                correlation_id = str(uuid.uuid4())
            
            correlation_id_var.set(correlation_id)
            
            # Add to response headers
            async def send_wrapper(message):
                if message['type'] == 'http.response.start':
                    message['headers'].append(
                        (b'x-correlation-id', correlation_id.encode())
                    )
                await send(message)
            
            await self.app(scope, receive, send_wrapper)
        else:
            await self.app(scope, receive, send)

def get_correlation_id() -> str:
    return correlation_id_var.get()
```

**2. Update logging configuration** to include correlation ID:
```python
import logging
from correlation import get_correlation_id

class CorrelationLogFilter(logging.Filter):
    def filter(self, record):
        record.correlation_id = get_correlation_id()
        return True

# In logging setup
logging.basicConfig(
    format='[ui] correlationId=%(correlation_id)s %(message)s',
    level=logging.INFO
)
logging.getLogger().addFilter(CorrelationLogFilter())
```

**3. When sending requests to MIDI layer:**
```python
import httpx
from correlation import get_correlation_id

async with httpx.AsyncClient() as client:
    response = await client.post(
        midi_url,
        json=payload,
        headers={'X-Correlation-ID': get_correlation_id()}
    )
```

**4. Filter health check logs:**
```python
class HealthCheckFilter(logging.Filter):
    def filter(self, record):
        message = record.getMessage()
        return '/health' not in message and '/ready' not in message

logging.getLogger().addFilter(HealthCheckFilter())
```

**5. Update chat command handler:**
```python
if "!engineroom" in message.lower():
    logging.info("operation=chat_command command=engineroom status=start")
    
    # Make request to MIDI layer
    logging.info("operation=call_midi_service endpoint=/setEffect device=VentrisDualReverb")
    
    # ... rest of logic
```

## Grafana Dashboard Queries

### Panel 1: Operation Flow Logs

**LogQL Query:**
```logql
{namespace=~"eightbitsaxlounge-(dev|prod)"}
  |= "correlationID"
  | logfmt
  | line_format "[{{.container}}] {{.operation}} {{if .db}}db={{.db}}{{end}} {{if .device}}device={{.device}}{{end}} status={{.status}}"
```

### Panel 2: Filter by Specific Operation

Track all "engineroom" command flows:
```logql
{namespace=~"eightbitsaxlounge-(dev|prod)"}
  |= "!engineroom" or "VentrisDualReverb"
  | logfmt
  | line_format "{{.ts}} [{{.layer}}] {{.message}}"
```

### Panel 3: Error-Only Logs

```logql
{namespace=~"eightbitsaxlounge-(dev|prod)"}
  |= "status=error" or "error=" or "ERROR" or "WARN"
  | logfmt
```

### Panel 4: Trace Specific CorrelationID

Once you see a correlation ID in one log, trace the entire flow:
```logql
{namespace=~"eightbitsaxlounge-(dev|prod)"}
  |= "correlationID=abc-123-def-456"
  | logfmt
  | line_format "{{.ts}} [{{.container}}] {{.operation}} {{.db}} {{.status}}"
```

## Grafana Dashboard Panel Configuration

1. **Create Panel** → Visualization: **Logs**
2. **Query**: Use one of the LogQL queries above
3. **Options**:
   - Show time: Yes
   - Show labels: Yes  
   - Wrap lines: Yes
   - Deduplication: None
4. **Display**:
   - Line highlighting: Error (red), Warn (yellow)

## Example Flow Output

When a user types `!engineroom` in chat:

```
2026-02-12 10:29:50 [ui] correlationId=f3a2b1c0 operation=chat_command command=engineroom status=start
2026-02-12 10:29:50 [ui] correlationId=f3a2b1c0 operation=call_midi_service endpoint=/setEffect device=VentrisDualReverb
2026-02-12 10:29:50 [midi] correlationId=f3a2b1c0 operation=set_effect device=VentrisDualReverb status=start
2026-02-12 10:29:50 [midi] correlationId=f3a2b1c0 operation=get_effect_data calling=data-service
2026-02-12 10:29:50 [data] correlationId=f3a2b1c0 operation=get_document db=devices id=VentrisDualReverb status=success
2026-02-12 10:29:50 [data] correlationId=f3a2b1c0 operation=get_document db=effects id=dual-reverb status=success
2026-02-12 10:29:50 [db] 2026-02-12T10:29:50.123Z GET /devices/VentrisDualReverb 200 ok 2ms
2026-02-12 10:29:50 [db] 2026-02-12T10:29:50.125Z GET /effects/dual-reverb 200 ok 1ms
2026-02-12 10:29:51 [midi] correlationId=f3a2b1c0 operation=send_cc device=VentrisDualReverb cc=20 value=64
2026-02-12 10:29:51 [midi] correlationId=f3a2b1c0 operation=update_device_state calling=data-service
2026-02-12 10:29:51 [data] correlationId=f3a2b1c0 operation=update_document db=devices id=VentrisDualReverb status=success
2026-02-12 10:29:51 [db] 2026-02-12T10:29:51.457Z PUT /devices/VentrisDualReverb 201 ok 3ms
2026-02-12 10:29:51 [midi] correlationId=f3a2b1c0 operation=set_effect status=success
```

## Next Steps

1. ✅ Health check filtering configured in Alloy
2. ✅ Data layer correlation ID implemented
3. **Deploy Alloy config update:** Run `ansible-playbook -i server/ansible-hosts monitoring/k8s-monitoring.yaml --ask-vault-pass`
4. Implement MIDI layer correlation ID middleware
5. Implement UI layer correlation ID middleware  
6. Create Grafana dashboard with log panels
7. Test full flow with `!engineroom` command
8. Add more operation-specific logging as needed

## Benefits

- **Trace requests** across all services with a single ID
- **Filter out noise** (health checks) from logs
- **Debug issues** by following the correlation ID through the entire stack
- **Monitor operations** like chat commands, MIDI messages, database updates
- **Identify bottlenecks** by seeing timing between service calls

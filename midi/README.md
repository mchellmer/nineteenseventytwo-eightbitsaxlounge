# Overview
The midi layer interacts with midi devices sending midi messages to set a desired state of effects. The layer uses dual deployment: a containerized API proxy in Kubernetes and a Windows service on the PC with connected MIDI devices.

**Architecture:**
- ASP.NET Core Minimal API (.NET 8.0)
- Kubernetes: Publicly accessible proxy service with JWT authentication
- Windows PC: Direct device access via WinMM.dll
- Request flow: `UI → K8s (JWT) → PC (bypass key) → WinMM → Device`

**Datamodel:**
- See [midi/datamodel/couchdb](datamodel/couchdb) - Models MIDI implementation documented in [docs/VentrisDualReverb](docs/VentrisDualReverb)
- Flexible structure supporting multiple devices with device-specific effect mappings
- Configuration files:
  - `appsettings.Effects.json` - Effect definitions with device-specific settings
  - `appsettings.Devices.VentrisDualReverb.json` - Device MIDI implementation and mappings

**Authentication:**
- External requests (UI, clients): JWT token authentication
- Internal requests (K8s → PC): Bypass key via X-Bypass-Key header

# Deployment
## Kubernetes Implementation
Containerized proxy service deployed to Kubernetes cluster. Handles authentication and forwards device operations to Windows PC service.

**Deployment:**
```bash
make deploy-k8s-dev     # Deploy to eightbitsaxlounge-dev namespace
make deploy-k8s-prod    # Deploy to eightbitsaxlounge-prod namespace
```

**Configuration:**
- Image: `ghcr.io/mchellmer/eightbitsaxlounge-midi:<version>`
- Environment: MidiDeviceService__Url points to PC service
- Secrets: JWT keys + MidiDeviceService__BypassKey for outgoing proxy requests

## PC Implementation
Windows service deployed to PC with connected MIDI devices. Accepts bypass key from K8s or JWT tokens for direct access.

**Deployment:**
```bash
make deploy-pc-dev      # Deploy to C:\Services\Midi\dev (port 5000)
make deploy-pc-prod     # Deploy to C:\Services\Midi\prod (port 5001)
```

**Configuration:**
- Uses Winmm.dll for direct device access
- Managed by NSSM as Windows Service
- Accepts X-Bypass-Key header (from K8s) or JWT token

# CI/CD

**Workflow Trigger:**
- Changes to `midi/version.txt` on `main` branches
- Manual workflow dispatch

**Pipeline:**
1. Run unit tests
2. **Parallel builds:**
   - Package Windows application (`dotnet publish`)
   - Build and push Linux ARM64 container to GHCR
3. **Conditional deployments:**
   - Deploy to Windows PC via Ansible (environment based on branch)
   - Deploy to Kubernetes via Ansible (environment based on branch)

**Data Management Workflows:**
- **Data Initialization** (`midi-data-init.yml`): Creates CouchDB databases and views
- **Data Upload** (`midi-data-upload.yml`): Uploads effects or device configurations
  - Manual trigger with choice: `effects` or `devices`
  - Authenticates with JWT token
  - Uploads to environment based on branch (dev/prod)

**Manual Deployment:**
```bash
make deploy-k8s-dev      # Deploy K8s proxy to dev
make deploy-k8s-prod     # Deploy K8s proxy to prod
make deploy-pc-dev       # Deploy PC service to dev
make deploy-pc-prod      # Deploy PC service to prod
```

**Initial Setup:**

Ansible Access to PC
- Install OpenSSH (elevated powershell session)
  Check enabled: `Get-WindowsCapability -Online | Where-Object Name -like 'OpenSSH.Server*'`
  Enable: `Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0`
  Start: `Start-Service sshd`
  AutoStart: `Set-Service -Name sshd -StartupType 'Automatic'`
  Confirm Running: `Get-Service sshd`
  Confirm Listening: `Get-NetTCPConnection -LocalPort 22 -State Listen`
  Test localhost: `ssh localhost`
  Add rule allowing connection from ci/cd host: 
  ```
    New-NetFirewallRule -DisplayName "OpenSSH Server (Pi only)" 
        -Name "OpenSSH-Server-CICD" `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort 22 `
        -Action Allow `
        -RemoteAddress <CICD IP> `
        -Profile Any `
        -Enabled True`
  ```
  Restart: `Restart-Service sshd`
  Test from pi: `ssh <username>@<PC IP>`

- Ansible access
  Ensure entry in /etc/ansible/hosts for midi group (handled by server layer)
    ```
      [midi]
      midi-host ansible_host=<PC IP> ansible_user=<PC User> ansible_connection=ssh ansible_shell_type=powershell
    ```
  PC uses powershell by default for ssh (elevated powershell session)
    temProperty -Path "HKLM:\SOFTWARE\OpenSSH" -Name DefaultShell -Value "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe" -PropertyType String -Force`
  Test: `ansible midi-host -m ansible.windows.win_shell -a "Get-Service sshd" --ask-pass`

- Configure PC via Ansible (ssh keys, service directory and dependencies): `make init-midi`
- Release to PC (take artifact and install): `make deploy-midi`

# Scope

**Current Implementation:**
- Dual deployment: Kubernetes proxy + Windows PC device service
- Device MIDI implementation handled by Windows PC service
- K8s service proxies device requests to PC using bypass key authentication
- Send control change messages to named devices
- Data model initialization and management via CouchDB
- Configuration-based effect and device metadata upload

**Request Flow:**
```
Client → K8s MIDI API (JWT auth)
       → Windows PC Service (bypass key)
       → WinMM.dll
       → USB MIDI Device
```

**Data Management:**
```
GitHub Actions → K8s MIDI API (JWT auth)
              → Data Layer API
              → CouchDB (devices, effects, selectors)
```

**Future Enhancements:**
- Device-specific activation logic (turn on/off devices or effects)
- Value validation (min/max ranges for continuous/selector effects)
- Real-time device state synchronization with database
- PUT {deviceName}/reset endpoint to restore device defaults
- Automated effect preset management

## Minimal API

**Endpoints:**
- `POST /api/token`: Retrieve JWT token for authenticated client
  - Request: `{ "clientId": "...", "clientSecret": "..." }`
  - Response: `{ "access_token": "...", "token_type": "Bearer" }`
  
- `POST /api/Midi/SendControlChangeMessage`: Send MIDI control change message
  - Authentication: Required (Bearer token)
  - Request: `{ "deviceMidiConnectName": "...", "address": 0-127, "value": 0-127, "channel": 0-15 }`
  
- `POST /api/Midi/InitializeDataModel`: Initialize CouchDB databases and views
  - Authentication: Required (Bearer token)
  - Creates databases: `devices`, `effects`, `selectors`
  - Sets up design documents and views
  
- `POST /api/Midi/UploadEffects`: Upload effects configuration to data layer
  - Authentication: Required (Bearer token)
  - Reads from `appsettings.Effects.json`
  - Creates/updates effect documents in CouchDB
  
- `POST /api/Midi/UploadDevice/{deviceName}`: Upload device configuration to data layer
  - Authentication: Required (Bearer token)
  - Parameter: `deviceName` (e.g., "VentrisDualReverb")
  - Reads from `appsettings.Devices.{deviceName}.json`
  - Creates/updates device, selector, and effect documents in CouchDB

**Example - Send MIDI Message:**
```bash
# Get token
TOKEN=$(curl -X POST http://localhost:5000/api/token \
  -H "Content-Type: application/json" \
  -d '{"clientId":"...","clientSecret":"..."}' | jq -r '.access_token')

# Send MIDI message
curl -X POST http://localhost:5000/api/Midi/SendControlChangeMessage \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceMidiConnectName": "One Series Ventris Reverb",
    "address": 1,
    "value": 8,
    "channel": 0
  }'
```

**Example - Initialize Data Model:**
```bash
curl -X POST http://localhost:5000/api/Midi/InitializeDataModel \
  -H "Authorization: Bearer $TOKEN"
```

**Example - Upload Device Configuration:**
```bash
curl -X POST http://localhost:5000/api/Midi/UploadDevice/VentrisDualReverb \
  -H "Authorization: Bearer $TOKEN"
```

## Library

**Architecture:**
- Handler pattern with `IEndpointHandler<TRequest, TResponse>` interface
- Endpoint handlers: `SendControlChangeMessageHandler`, `InitializeDataModelHandler`, `UploadEffectsHandler`, `UploadDeviceHandler`, `ResetDeviceHandler`
- Service layer: `WinmmMidiDeviceService`, `EightBitSaxLoungeMidiDataService`

**WinmmMidiDeviceService:**
- P/Invoke wrapper for WinMM.dll MIDI functions
- Supports proxy mode (K8s) and direct mode (Windows PC)
- Proxy mode: Forwards requests to Windows PC via HTTP with bypass key
- Direct mode: Uses WinMM API for local device access

**EightBitSaxLoungeMidiDataService:**
- Manages device, effect, and selector metadata in CouchDB
- Handles data model initialization and document creation
- Supports configuration-based data upload from JSON files
- Interfaces with data layer API for CRUD operations

**Configuration Model:**
- Device metadata (name, MIDI implementation, effect mappings)
- Selector mappings (discrete MIDI values for named effects)
- Effect settings (control change addresses, value ranges, device-specific mappings)
- DeviceSettings with DeviceName for multi-device effect support

# Tests
- Endpoint handler unit tests (SendControlChangeMessage, InitializeDataModel, UploadEffects, UploadDevice, ResetDevice)
- Data service unit tests (EightBitSaxLoungeMidiDataService)
- WinMM device service unit tests (WinmmMidiDeviceService)
- Configuration model validation tests


# Testing the UI Bot with Podman on Windows

## Prerequisites

1. Podman Desktop or Podman CLI installed on Windows
2. MIDI service running and accessible at `http://192.168.68.50:5000`
3. Valid MIDI API credentials

## Setup

### 1. Create a test `.env` file

Copy `.env.example` to `.env` and update with your actual values:

```bash
cp .env.example .env
```

Edit `.env` and set:
- `TWITCH_TOKEN`: Your Twitch OAuth token
- `TWITCH_CLIENT_ID`: Your Twitch application client ID
- `TWITCH_CHANNEL`: The Twitch channel to monitor
- `MIDI_CLIENT_SECRET`: Your MIDI API client secret

### 2. Build the container image

```powershell
podman build -t eightbitsaxlounge-ui:test .
```

### 3. Run the container

Run with the `.env` file:

```powershell
podman run -it --rm `
  --env-file .env `
  --name eightbitsax-ui-test `
  eightbitsaxlounge-ui:test
```

Or run with individual environment variables:

```powershell
podman run -it --rm `
  -e TWITCH_TOKEN="oauth:your_token" `
  -e TWITCH_CLIENT_ID="your_client_id" `
  -e TWITCH_CHANNEL="your_channel" `
  -e MIDI_DEVICE_URL="http://192.168.68.50:5000" `
  -e MIDI_CLIENT_ID="localhost" `
  -e MIDI_CLIENT_SECRET="your_secret" `
  -e MIDI_DEVICE_NAME="One Series Ventris Reverb" `
  -e LOG_LEVEL="DEBUG" `
  --name eightbitsax-ui-test `
  eightbitsaxlounge-ui:test
```

### 4. Test the engine command

Once the bot is running and connected to Twitch chat, test the `!engine room` command:

1. Go to your Twitch channel chat
2. Type: `!engine room`
3. The bot should:
   - Authenticate with the MIDI API (if not already authenticated)
   - Send a control change message (address=1, value=8) to "One Series Ventris Reverb"
   - Respond with: "🎵 Engine set to 'room' mode! 🎵"

## How Authentication Works

The bot implements automatic authentication with retry:

1. **First Request**: When `!engine room` is first called, the MIDI client has no token
   - `send_control_change_message()` checks if token exists
   - If not, calls `_ensure_authenticated()` which requests a new token
   - Proceeds with the MIDI message

2. **Token Expiration**: If the token expires after some time
   - The POST request returns HTTP 401 or 403
   - Client automatically calls `_ensure_authenticated()` to refresh the token
   - Retries the request with the new token

3. **Concurrent Requests**: If multiple commands are issued simultaneously
   - The `_authenticating` flag prevents multiple concurrent auth requests
   - Subsequent requests wait for the ongoing authentication to complete

## Monitoring

Watch the logs for authentication and MIDI activity:

```
INFO - Successfully authenticated as localhost
INFO - Engine changed to room by username (CC 1=8)
```

If there are issues, set `LOG_LEVEL=DEBUG` for more detailed output.

## Stopping the Container

Press `Ctrl+C` or in another terminal:

```powershell
podman stop eightbitsax-ui-test
```

## Troubleshooting

### Cannot connect to MIDI service

If you see connection errors, verify:
- MIDI service is running: `curl http://192.168.68.50:5000/api/health`
- Container can reach the host network (you may need `--network=host` on Linux)

### Authentication fails

- Verify `MIDI_CLIENT_ID` and `MIDI_CLIENT_SECRET` are correct
- Test authentication manually:
  ```bash
  curl -X POST http://192.168.68.50:5000/api/token \
    -H "Content-Type: application/json" \
    -d '{"clientId": "localhost", "clientSecret": "your_secret"}'
  ```

### Bot doesn't respond in chat

- Verify Twitch credentials are correct
- Check the bot has joined the channel (you'll see a join message in logs)
- Ensure the channel name matches exactly

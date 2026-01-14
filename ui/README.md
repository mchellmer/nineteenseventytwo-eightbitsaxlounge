# UI Layer for EightBitSaxLounge

The UI is the viewer facing interface for the 8bsl. Viewers have the ability to alter elements on the UI in ways that trigger requests handled here.

## Chatbot

A Python-based chatbot that monitors channels and responds to commands, integrating with the MIDI layer to control audio equipment.

Configured for Twitch, but flexible to build bots for other streaming services.

### Development

#### Streaming Service

The app runs an implementation of StreamingBot, currently set to a TwitchIO based Twitch chat bot.

To change to another service create a new impelementation in ./src/bots and update ./src/main.py.

#### UI Element Response

UI elements viewers can interact with are mapped to elements defined in ./src/commands. E.g. a viewer in Twitch types in chat !engine room -> the engine command updates the UI and the 8bsl to the 'room' reverb engine.

Register a new command in ./src/commands/command_registry.py and create a handler in ./src/commands/handlers that implements CommandHandler.

Configured commands:
- General
    - `help` - Shows available commands
- Ventris Dual Reverb
    - `engine <engine name>` - Sets reverb engine A

#### App Services

The 8bsl has several services that handle updating music hardware, state data, etc. Integration with these services is defined in ./src/services.

Configured services:
- midi_data_client - this handles requests to update midi data inline with UI element state
- midi_device_client - this handles requests to update midi devices inline with UI element state

#### App config

The streaming bot requires config e.g. channel details, bot account details and these are set in ./src/config

#### Local Development

```bash
# Install dependencies
pip install -r requirements.txt

# Run the bot
python src/main.py
```

### Building and Deployment

```bash
# Build Docker image
make build

# Deploy to Kubernetes
make deploy
```

## Configuration

Set the following environment variables:

- `TWITCH_TOKEN` - Your Twitch bot token
- `TWITCH_CLIENT_ID` - Your Twitch application client ID
- `TWITCH_CHANNEL` - The channel to monitor
- `MIDI_API_URL` - URL of the MIDI service API
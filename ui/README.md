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
- twitch_client - handles monitoring token validity

#### App config

The streaming bot requires config e.g. channel details, bot account details and these are set in ./src/config

#### Local Development

```bash
# Install dependencies
pip install -r requirements.txt
pip install -r requirements-dev.txt

# Run tests
make test

# Run the bot locally (requires .env file with credentials)
python src/main.py
```

#### Docker (Local)

```bash
# Build and run locally
# Create .env file with required variables first
make docker-build
make docker-run
```

### Deployment

Deployed to Kubernetes with separate dev and prod namespaces. GitHub Actions handles CI/CD on version.txt changes which triggers an ansible deployment from the cicd server to the cluster.

**Switch Active Environment:**
- Navigate to Actions → "UI Set Active Environment"
- Select dev or prod from dropdown
- Only one environment runs at a time (prevents duplicate bot messages)

**Manual Deploy:**
```bash
# From cicd server
make deploy-ui
```
# Twitch Chatbot for EightBitSaxLounge

A Python-based Twitch chatbot that monitors channels and responds to commands, integrating with the MIDI layer to control audio equipment.

## Features

- Monitors Twitch chat for commands
- Responds to !engine commands to control MIDI settings
- Extensible command system for easy addition of new commands
- Kubernetes-ready deployment

## Commands

- `!engine room` - Sets MIDI engine to 'room' setting
- `!engine jazz` - Sets MIDI engine to 'jazz' setting
- `!help` - Shows available commands

## Development

### Local Development

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
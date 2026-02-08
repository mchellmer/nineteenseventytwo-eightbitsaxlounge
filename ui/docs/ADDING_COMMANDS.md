# Adding New Value-Based Commands

This guide explains how to add new value-based commands (like `!time`, `!decay`, `!predelay`, etc.) that scale values from 0-10 to MIDI range (0-127).

## Overview

The `ValueHandler` class provides a reusable way to create commands that:
- Accept a numeric value from the user (e.g., `!time 6`)
- Validate the input is within the allowed range (default: 0-10)
- Scale the value to MIDI range (0-127)
- Send it to the MIDI API with the appropriate device and effect settings

### Scaling Logic

The helper function `scale_value_to_midi()` converts values:
- Input: 0-10 range (configurable)
- Output: 0-127 MIDI range
- Examples:
  - 0 → 0
  - 1 → 13
  - 5 → 64
  - 10 → 127

## How to Add a New Command

### Step 1: Add Handler in `command_registry.py`

```python
# In CommandRegistry.__init__()

self._decay_handler = ValueHandler(
    midi_client=self._midi_client,
    command_name="decay",  # Command name (!decay)
    device_name="VentrisDualReverb",  # MIDI device
    device_effect_name="ReverbEngineA",  # Effect name
    device_effect_setting_name="Decay",  # Setting to control
    min_value=0,  # Minimum allowed value
    max_value=10  # Maximum allowed value
)
```

Add to the `_commands` dictionary:

```python
self._commands: Dict[str, Tuple[Callable, str]] = {
    # ... existing commands ...
    'decay': (
        self._decay_handler.handle,
        self._decay_handler.description
    ),
}
```

### Step 2: Register Command in `twitch_bot.py`

Add the command registration in `__init__()`:

```python
# Register 8bsl channel commands with TwitchIO
self.twitchio.add_command(commands.Command(name='decay', func=self.decay_command))
```

Add the handler method:

```python
async def decay_command(self, ctx, *args):
    """Handle !decay commands."""
    await self._execute_command('decay', list(args), ctx)
```

## Example Commands to Add

Based on typical reverb parameters, you might want to add:

- `!decay <0-10>` - Reverb decay time
- `!predelay <0-10>` - Pre-delay amount
- `!mix <0-10>` - Wet/dry mix
- `!size <0-10>` - Room size
- `!damping <0-10>` - High frequency damping

## Testing

Tests are located in `tests/handlers/test_value_handler.py` and cover:
- Value scaling (0-10 to 0-127)
- Input validation
- Error handling
- API integration

Run tests with:
```bash
pytest tests/handlers/test_value_handler.py -v
```

## Custom Value Ranges

If you need a different input range (e.g., 0-100 instead of 0-10), adjust `min_value` and `max_value`:

```python
self._custom_handler = ValueHandler(
    midi_client=self._midi_client,
    command_name="custom",
    device_name="MyDevice",
    device_effect_name="MyEffect",
    device_effect_setting_name="MySetting",
    min_value=0,
    max_value=100  # Accepts 0-100 instead of 0-10
)
```

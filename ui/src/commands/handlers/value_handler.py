"""Generic value command handler for MIDI parameters that accept numeric values."""

import logging
from typing import Any

from commands.handlers.midi_base import MidiBaseHandler
from services.midi_client import MidiClient

logger = logging.getLogger(__name__)


def scale_value_to_midi(value: float, min_input: float = 0, max_input: float = 10) -> int:
    """
    Scale a value from an input range to MIDI range (0-127).
    
    Args:
        value: The input value to scale
        min_input: Minimum value of the input range (default: 0)
        max_input: Maximum value of the input range (default: 10)
        
    Returns:
        Scaled value in the range 0-127
        
    Raises:
        ValueError: If value is outside the input range
    """
    if value < min_input or value > max_input:
        raise ValueError(f"Value {value} is outside the range [{min_input}, {max_input}]")
    
    # Scale from input range to 0-127
    scaled = ((value - min_input) / (max_input - min_input)) * 127
    return round(scaled)


class ValueHandler(MidiBaseHandler):
    """Handler for value-based MIDI commands.
    
    This handler can be configured for different effects that accept numeric values.
    It scales input values from 0-10 to MIDI range (0-127).
    """
    
    def __init__(
        self,
        midi_client: MidiClient,
        command_name: str,
        device_name: str,
        device_effect_name: str,
        device_effect_setting_name: str,
        min_value: int = 0,
        max_value: int = 10
    ):
        """Initialize value handler with MIDI client and effect configuration.
        
        Args:
            midi_client: Shared MidiClient instance for API requests
            command_name: Name of the command (e.g., "time", "decay")
            device_name: MIDI device name
            device_effect_name: Name of the device effect
            device_effect_setting_name: Name of the specific effect setting
            min_value: Minimum allowed input value (default: 0)
            max_value: Maximum allowed input value (default: 10)
        """
        super().__init__(midi_client)
        self._command_name = command_name
        self._device_name = device_name
        self._device_effect_name = device_effect_name
        self._device_effect_setting_name = device_effect_setting_name
        self._min_value = min_value
        self._max_value = max_value
    
    @property
    def command_name(self) -> str:
        """Get the command name."""
        return self._command_name
    
    @property
    def description(self) -> str:
        """Get the command description."""
        return f"Set {self._command_name}. Usage: !{self._command_name} <value> (range: {self._min_value}-{self._max_value})"
    
    async def handle(self, args: list[str], context: Any) -> str:
        """
        Handle value-based commands.
        
        Args:
            args: Command arguments (expects numeric value as first arg)
            context: Command context (Twitch context)
            
        Returns:
            Response message for chat
        """
        if not args:
            return f"Usage: !{self._command_name} <value>. Range: {self._min_value}-{self._max_value}"
        
        try:
            # Parse the input value
            input_value = float(args[0])
            
            # Validate range
            if input_value < self._min_value or input_value > self._max_value:
                return f"❌ Value must be between {self._min_value} and {self._max_value}"
            
            # Scale to MIDI range (0-127)
            midi_value = scale_value_to_midi(input_value, self._min_value, self._max_value)
            
            requester = context.author.name if hasattr(context, 'author') else "chatbot"
            
            # Call SetEffect endpoint with the scaled value
            await self.midi_client.set_effect(
                device_name=self._device_name,
                device_effect_name=self._device_effect_name,
                device_effect_setting_name=self._device_effect_setting_name,
                value=midi_value
            )
            
            logger.info(f"{self._command_name} set to {input_value} (MIDI: {midi_value}) by {requester}")
            
            # Format display value to show integers without decimal point
            display_value = int(input_value) if input_value == int(input_value) else input_value
            return f"🎵 {self._command_name.capitalize()} set to {display_value}! 🎵"
            
        except ValueError as e:
            if "outside the range" in str(e):
                return f"❌ Value must be between {self._min_value} and {self._max_value}"
            return f"❌ Invalid value. Please provide a number between {self._min_value} and {self._max_value}"
        except Exception as e:
            logger.error(f"Failed to set {self._command_name} to {args[0]}: {e}")
            return f"❌ Failed to set {self._command_name}. Please try again later."

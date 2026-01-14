"""Engine command handler for changing MIDI engine types."""

import logging
from typing import Any

from .midi_base import MidiBaseHandler
from ...config.settings import settings
from ...services.midi_client import MidiClient

logger = logging.getLogger(__name__)


class EngineHandler(MidiBaseHandler):
    """Handler for engine-related commands."""
    
    VALID_ENGINES = ["room"]
    
    def __init__(self, midi_client: MidiClient):
        """Initialize engine handler with MIDI client.
        
        Args:
            midi_client: Shared MidiClient instance for API requests
        """
        super().__init__(midi_client)
    
    @property
    def command_name(self) -> str:
        """Get the command name."""
        return "engine"
    
    @property
    def description(self) -> str:
        """Get the command description."""
        return f"Change MIDI engine. Usage: !engine <type>. Available: {', '.join(self.VALID_ENGINES)}"
    
    async def handle(self, args: list[str], context: Any) -> str:
        """
        Handle !engine commands.
        
        Args:
            args: Command arguments (expects engine type as first arg)
            context: Command context (Twitch context)
            
        Returns:
            Response message for chat
        """
        if not args:
            return f"Usage: !engine <type>. Available engines: {', '.join(self.VALID_ENGINES)}"
        
        engine_type = args[0].lower()
        
        if engine_type not in self.VALID_ENGINES:
            return f"Invalid engine type: {engine_type}. Available engines: {', '.join(self.VALID_ENGINES)}"
        
        try:
            requester = context.author.name if hasattr(context, 'author') else "chatbot"
            
            # Map engine types to MIDI control values
            # For now, "room" engine sends control change: address=1, value=8
            engine_midi_values = {
                "room": (1, 8)
            }
            
            address, value = engine_midi_values[engine_type]
            
            # Send MIDI control change message (will auto-authenticate if needed)
            await self.midi_client.send_control_change_message(
                device_midi_connect_name=settings.midi_device_name,
                address=address,
                value=value
            )
            
            logger.info(f"Engine changed to {engine_type} by {requester} (CC {address}={value})")
            return f"🎵 Engine set to '{engine_type}' mode! 🎵"
            
        except Exception as e:
            logger.error(f"Failed to set engine to {engine_type}: {e}")
            return f"❌ Failed to set engine to '{engine_type}'. Please try again later."

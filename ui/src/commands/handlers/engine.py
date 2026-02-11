"""Engine command handler for changing MIDI engine types."""

import logging
from typing import Any

from commands.handlers.midi_base import MidiBaseHandler
from config.settings import settings
from services.midi_client import MidiClient

logger = logging.getLogger(__name__)


class EngineHandler(MidiBaseHandler):
    """Handler for engine-related commands."""
    
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
        # Convert engine names to lowercase for display
        available = ', '.join([e.lower() for e in settings.valid_engines])
        return f"Change MIDI engine. Usage: !engine <type>. Available: {available}"
    
    async def handle(self, args: list[str], context: Any) -> str:
        """
        Handle !engine commands.
        
        Args:
            args: Command arguments (expects engine type as first arg)
            context: Command context (Twitch context)
            
        Returns:
            Response message for chat
        """
        # Convert valid engines to lowercase for comparison
        valid_engines_lower = [e.lower() for e in settings.valid_engines]
        
        if not args:
            return f"Usage: !engine <type>. Available engines: {', '.join(valid_engines_lower)}"
        
        engine_type = args[0].lower()
        
        # Find the matching engine name (case-insensitive)
        matching_engine = None
        for valid_engine in settings.valid_engines:
            if valid_engine.lower() == engine_type:
                matching_engine = valid_engine
                break
        
        if not matching_engine:
            return f"Invalid engine type: {engine_type}. Available engines: {', '.join(valid_engines_lower)}"
        
        try:
            requester = context.author.name if hasattr(context, 'author') else "chatbot"
            
            # Call SetEffect endpoint with static values except for selection
            await self.midi_client.set_effect(
                device_name="VentrisDualReverb",
                device_effect_name="ReverbEngineA",
                device_effect_setting_name="ReverbEngine",
                selection=matching_engine
            )
            
            logger.info(f"Engine changed to {matching_engine} by {requester}")
            return f"🎵 Engine set to '{engine_type}' mode! 🎵"
            
        except Exception as e:
            logger.error(f"Failed to set engine to {engine_type}: {e}")
            return f"❌ Failed to set engine to '{engine_type}'. Please try again later."

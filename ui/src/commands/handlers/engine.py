"""Engine command handler for changing MIDI engine types."""

import logging
from typing import Any

from .base import BaseHandler
from ...config.settings import settings

logger = logging.getLogger(__name__)


class EngineHandler(BaseHandler):
    """Handler for engine-related commands."""
    
    VALID_ENGINES = ["room", "jazz", "ambient", "rock", "electronic"]
    
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
            # Make request to MIDI service to change engine
            requester = context.author.name if hasattr(context, 'author') else "chatbot"
            response = await self.midi_client.post("api/engine", {
                "engine_type": engine_type,
                "requester": requester
            })
            
            logger.info(f"Engine changed to {engine_type} by {requester}")
            return f"🎵 Engine set to '{engine_type}' mode! 🎵"
            
        except Exception as e:
            logger.error(f"Failed to set engine to {engine_type}: {e}")
            return f"❌ Failed to set engine to '{engine_type}'. Please try again later."

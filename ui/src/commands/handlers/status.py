"""Status command handler for displaying bot and service status."""

import logging
from typing import Any

from .base import BaseHandler
from ...config.settings import settings

logger = logging.getLogger(__name__)


class StatusHandler(BaseHandler):
    """Handler for status commands."""
    
    @property
    def command_name(self) -> str:
        """Get the command name."""
        return "status"
    
    @property
    def description(self) -> str:
        """Get the command description."""
        return "Show bot and MIDI service status"
    
    async def handle(self, args: list[str], context: Any) -> str:
        """
        Handle !status commands.
        
        Args:
            args: Command arguments (not used)
            context: Command context (Twitch context)
            
        Returns:
            Response message for chat with status information
        """
        try:
            # Check MIDI service health
            midi_status = await self.midi_client.get("health")
            midi_healthy = midi_status.get("status") == "healthy"
            
            status_parts = [
                "🤖 Bot: Online",
                f"🎵 MIDI Service: {'✅ Healthy' if midi_healthy else '❌ Unhealthy'}",
                f"📺 Channel: #{settings.twitch_channel}"
            ]
            
            return " | ".join(status_parts)
            
        except Exception as e:
            logger.error(f"Failed to get status: {e}")
            return "🤖 Bot: Online | 🎵 MIDI Service: ❌ Unavailable"

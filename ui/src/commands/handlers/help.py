"""Help command handler for displaying available commands."""

import logging
from typing import Any

from .base import BaseHandler

logger = logging.getLogger(__name__)


class HelpHandler(BaseHandler):
    """Handler for help commands."""
    
    @property
    def command_name(self) -> str:
        """Get the command name."""
        return "help"
    
    @property
    def description(self) -> str:
        """Get the command description."""
        return "Show available bot commands and usage information"
    
    async def handle(self, args: list[str], context: Any) -> str:
        """
        Handle !help commands.
        
        Args:
            args: Command arguments (not used)
            context: Command context (Twitch context)
            
        Returns:
            Response message for chat with command list
        """
        help_text = [
            "🎵 EightBitSaxLounge Bot Commands 🎵",
            "!engine <type> - Change MIDI engine (room, jazz, ambient, rock, electronic)",
            "!status - Show bot and system status", 
            "!help - Show this help message"
        ]
        return " | ".join(help_text)

"""Help command handler for displaying available commands."""

import logging
from typing import Any

from commands.handlers.command_handler import CommandHandler
from config.settings import settings

logger = logging.getLogger(__name__)


class HelpHandler(CommandHandler):
    """Handler for help commands."""
    
    @property
    def command_name(self) -> str:
        """Get the command name."""
        return "help"
    
    @property
    def description(self) -> str:
        """Get the command description."""
        return "Show available bot commands and usage information"
    
    async def handle(self, args: list[str], context: Any) -> list[str]:
        """
        Handle !help commands.
        
        Args:
            args: Command arguments (not used)
            context: Command context (Twitch context)
            
        Returns:
            List of response messages for chat (sent as separate messages)
        """
        # Get available engines dynamically from settings
        available_engines = ', '.join([e.lower() for e in settings.valid_engines])
        
        # Return list of messages to be sent separately for multi-line effect
        return [
            "🎵 EightBitSaxLounge Bot Commands 🎵",
            f"!engine <type> - Change reverb engine ({available_engines})",
            "!time <0-10> - Set reverb time",
            "!predelay <0-10> - Set pre-delay",
            "!control1 <0-10> - Set control 1 parameter",
            "!control2 <0-10> - Set control 2 parameter",
            "!help - Show this help message"
        ]

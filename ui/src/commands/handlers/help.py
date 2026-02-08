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
    
    async def handle(self, args: list[str], context: Any) -> str:
        """
        Handle !help commands.
        
        Args:
            args: Command arguments (not used)
            context: Command context (Twitch context)
            
        Returns:
            Response message for chat with command list
        """
        # Get available engines dynamically from settings
        available_engines = ', '.join([e.lower() for e in settings.valid_engines])
        
        # Compact format with grouped commands for better readability in chat
        help_text = (
            f"🎵 Commands: !engine ({available_engines}) • "
            f"!time/!predelay/!control1/!control2 (0-10) • !help"
        )
        return help_text

"""
Command mappings for the Twitch chatbot.
This module defines the available commands and their corresponding handlers.
"""

from typing import Dict, Callable, Awaitable, Any
from .handlers import EngineHandler, HelpHandler, StatusHandler

class CommandRegistry:
    """Registry for bot commands."""
    
    def __init__(self):
        self.engine_handler = EngineHandler()
        self.help_handler = HelpHandler()
        self.status_handler = StatusHandler()
        
        # Command mapping: command -> (handler_method, description)
        self._commands: Dict[str, tuple[Callable, str]] = {
            "engine": (self.engine_handler.handle_engine_command, "Control MIDI engine settings"),
            "help": (self.help_handler.handle_help_command, "Show available commands"),
            "status": (self.status_handler.handle_status_command, "Show bot status"),
        }
    
    def get_command_handler(self, command: str) -> tuple[Callable, str] | None:
        """Get the handler for a specific command."""
        return self._commands.get(command.lower())
    
    def get_all_commands(self) -> Dict[str, str]:
        """Get all available commands and their descriptions."""
        return {cmd: desc for cmd, (_, desc) in self._commands.items()}
    
    def is_valid_command(self, command: str) -> bool:
        """Check if a command is valid."""
        return command.lower() in self._commands
    
    async def execute_command(self, command: str, args: list[str], context: Any) -> str:
        """Execute a command with the given arguments."""
        handler_info = self.get_command_handler(command)
        if not handler_info:
            return f"Unknown command: {command}. Type !help for available commands."
        
        handler, _ = handler_info
        try:
            return await handler(args, context)
        except Exception as e:
            return f"Error executing command: {str(e)}"
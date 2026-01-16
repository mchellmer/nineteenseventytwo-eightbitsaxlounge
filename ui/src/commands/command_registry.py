"""
Command registry for managing and executing bot commands.
Maps command names to their handler methods.
"""

import logging
from typing import Dict, Tuple, Callable, List, Any

from config.settings import settings
from services.midi_client import MidiClient

logger = logging.getLogger(__name__)


class CommandRegistry:
    """Registry for managing and executing bot commands."""
    
    def __init__(self):
        """Initialize the command registry with all available commands."""
        from .handlers.engine import EngineHandler
        from .handlers.help import HelpHandler
        from .handlers.status import StatusHandler
        
        self._midi_client = MidiClient(
            device_base_url=settings.midi_device_url,
            data_base_url=settings.midi_data_url,
            client_id=settings.midi_client_id,
            client_secret=settings.midi_client_secret,
            timeout=settings.midi_api_timeout
        )
        
        self._engine_handler = EngineHandler(self._midi_client)
        self._help_handler = HelpHandler()
        self._status_handler = StatusHandler(self._midi_client)
        
        self._commands: Dict[str, Tuple[Callable, str]] = {
            'engine': (
                self._engine_handler.handle,
                self._engine_handler.description
            ),
            'help': (
                self._help_handler.handle,
                self._help_handler.description
            ),
            'status': (
                self._status_handler.handle,
                self._status_handler.description
            ),
        }
    
    async def execute_command(self, command_name: str, args: List[str], context: Any) -> str:
        """
        Execute a command with the given arguments and context.
        
        Args:
            command_name: The name of the command to execute
            args: List of arguments passed to the command
            context: The command context (typically a Twitch context object)
            
        Returns:
            The response message to send to chat
            
        Raises:
            ValueError: If the command is not found
        """
        if command_name not in self._commands:
            raise ValueError(f"Unknown command: {command_name}")
        
        handler_method, _ = self._commands[command_name]
        return await handler_method(args, context)
    
    def get_all_commands(self) -> Dict[str, str]:
        """
        Get all available commands and their descriptions.
        
        Returns:
            Dictionary mapping command names to descriptions
        """
        return {name: desc for name, (_, desc) in self._commands.items()}
    
    def register_command(self, name: str, handler: Callable, description: str):
        """
        Register a new command dynamically.
        
        Args:
            name: The command name
            handler: The async handler function
            description: Description of what the command does
        """
        self._commands[name] = (handler, description)
        logger.info(f"Registered new command: {name}")

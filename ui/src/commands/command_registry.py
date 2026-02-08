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
        from .handlers.value_handler import ValueHandler
        
        self._midi_client = MidiClient(
            base_url=settings.midi_device_url,
            client_id=settings.midi_client_id,
            client_secret=settings.midi_client_secret,
            timeout=settings.midi_api_timeout
        )
        
        self._engine_handler = EngineHandler(self._midi_client)
        self._help_handler = HelpHandler()
        
        # Value-based commands using ValueHandler
        # To add more commands like !decay, !predelay, etc.:
        # 1. Create a handler instance with ValueHandler
        # 2. Add it to self._commands dict below
        # 3. Register it in twitch_bot.py (add_command and add handler method)
        self._time_handler = ValueHandler(
            midi_client=self._midi_client,
            command_name="time",
            device_name="VentrisDualReverb",
            device_effect_name="ReverbEngineA",
            device_effect_setting_name="Time",
            min_value=0,
            max_value=10
        )
        
        self._predelay_handler = ValueHandler(
            midi_client=self._midi_client,
            command_name="predelay",
            device_name="VentrisDualReverb",
            device_effect_name="ReverbEngineA",
            device_effect_setting_name="PreDelay",
            min_value=0,
            max_value=10
        )
        
        self._control1_handler = ValueHandler(
            midi_client=self._midi_client,
            command_name="control1",
            device_name="VentrisDualReverb",
            device_effect_name="ReverbEngineA",
            device_effect_setting_name="Control1",
            min_value=0,
            max_value=10
        )
        
        self._control2_handler = ValueHandler(
            midi_client=self._midi_client,
            command_name="control2",
            device_name="VentrisDualReverb",
            device_effect_name="ReverbEngineA",
            device_effect_setting_name="Control2",
            min_value=0,
            max_value=10
        )
        
        self._commands: Dict[str, Tuple[Callable, str]] = {
            'engine': (
                self._engine_handler.handle,
                self._engine_handler.description
            ),
            'time': (
                self._time_handler.handle,
                self._time_handler.description
            ),
            'predelay': (
                self._predelay_handler.handle,
                self._predelay_handler.description
            ),
            'control1': (
                self._control1_handler.handle,
                self._control1_handler.description
            ),
            'control2': (
                self._control2_handler.handle,
                self._control2_handler.description
            ),
            'help': (
                self._help_handler.handle,
                self._help_handler.description
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

"""Base handler class for all command handlers."""

import logging
from typing import Any
from abc import ABC

from ...config.settings import settings
from ...services.midi_client import MidiClient
from ...interfaces.command_handler import CommandHandler

logger = logging.getLogger(__name__)


class BaseHandler(CommandHandler, ABC):
    """Base class for all command handlers with MIDI API integration."""
    
    def __init__(self):
        """Initialize the base handler with a MIDI client."""
        self._midi_client = MidiClient(
            base_url=settings.midi_api_url,
            timeout=settings.midi_api_timeout
        )
    
    @property
    def midi_client(self) -> MidiClient:
        """Get the MIDI client instance."""
        return self._midi_client
    
    @property
    def command_name(self) -> str:
        """Get the command name. Must be implemented by subclasses."""
        raise NotImplementedError
    
    @property
    def description(self) -> str:
        """Get the command description. Must be implemented by subclasses."""
        raise NotImplementedError
    
    async def handle(self, args: list[str], context: Any) -> str:
        """Handle the command. Must be implemented by subclasses."""
        raise NotImplementedError

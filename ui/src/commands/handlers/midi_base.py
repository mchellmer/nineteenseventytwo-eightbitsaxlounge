"""Base handler for command handlers that require MIDI API integration."""

import logging

from services.midi_client import MidiClient
from commands.handlers.command_handler import CommandHandler

logger = logging.getLogger(__name__)


class MidiBaseHandler(CommandHandler):
    """Base class for command handlers that need MIDI API integration.
    
    Provides shared access to a MidiClient instance via dependency injection.
    Handlers that don't need MIDI should extend CommandHandler directly.
    """
    
    def __init__(self, midi_client: MidiClient):
        """Initialize the handler with a MIDI client.
        
        Args:
            midi_client: Shared MidiClient instance for making requests to MIDI services
        """
        self._midi_client = midi_client
    
    @property
    def midi_client(self) -> MidiClient:
        """Get the MIDI client instance."""
        return self._midi_client

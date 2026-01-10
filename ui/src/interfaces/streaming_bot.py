"""
Abstract bot interface for streaming services.
"""

from abc import ABC, abstractmethod


class StreamingBot(ABC):
    """Abstract base class for streaming service bots."""
    
    @abstractmethod
    async def start(self) -> None:
        """Start the bot and connect to the streaming service."""
        pass
    
    @abstractmethod
    async def shutdown(self) -> None:
        """Gracefully shutdown the bot and disconnect from the service."""
        pass
    
    @abstractmethod
    async def send_message(self, channel: str, message: str) -> None:
        """Send a message to the specified channel."""
        pass
    
    @property
    @abstractmethod
    def is_connected(self) -> bool:
        """Check if the bot is currently connected to the streaming service."""
        pass
    
    @property
    @abstractmethod
    def bot_name(self) -> str:
        """Get the bot's username/nickname."""
        pass
    
    @property
    @abstractmethod
    def primary_channel(self) -> str:
        """Get the primary channel the bot is monitoring."""
        pass
    
    @property
    @abstractmethod
    def service_name(self) -> str:
        """Get the name of the streaming service."""
        pass

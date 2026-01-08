"""
Abstract bot interface for streaming services.
Defines the contract that any streaming bot implementation must follow.
"""

from abc import ABC, abstractmethod
from typing import Any, Optional


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


class BotFactory:
    """Factory for creating streaming bot instances."""
    
    @staticmethod
    def create_bot(service_type: str = "twitch") -> StreamingBot:
        """Create a bot instance for the specified streaming service."""
        if service_type.lower() == "twitch":
            from .bot import TwitchBot
            return TwitchBot()
        elif service_type.lower() == "discord":
            # Future implementation
            raise NotImplementedError("Discord bot not yet implemented")
        elif service_type.lower() == "youtube":
            # Future implementation  
            raise NotImplementedError("YouTube bot not yet implemented")
        else:
            raise ValueError(f"Unsupported streaming service: {service_type}")
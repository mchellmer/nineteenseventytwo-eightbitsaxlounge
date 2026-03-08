"""Base interface for command handlers."""

from abc import ABC, abstractmethod
from typing import Any, List, Union


class CommandHandler(ABC):
    """Abstract base class for command handlers."""
    
    @abstractmethod
    async def handle(self, args: List[str], context: Any) -> Union[str, List[str]]:
        """
        Handle a command with the given arguments.
        
        Args:
            args: Command arguments
            context: Command execution context (e.g., message, user info)
            
        Returns:
            Response message(s) to send to the user.
            Can be a single string or a list of strings (sent as separate messages).
        """
        pass
    
    @property
    @abstractmethod
    def command_name(self) -> str:
        """Get the name of this command."""
        pass
    
    @property
    @abstractmethod
    def description(self) -> str:
        """Get a description of what this command does."""
        pass

"""Factory for creating streaming bot instances."""

from ..interfaces.streaming_bot import StreamingBot


class BotFactory:
    """Factory for creating streaming bot instances."""
    
    @staticmethod
    def create_bot(service_type: str = "twitch") -> StreamingBot:
        """
        Create a bot instance for the specified streaming service.
        
        Args:
            service_type: The type of streaming service ("twitch", "discord", "youtube")
            
        Returns:
            A StreamingBot instance for the specified service
            
        Raises:
            ValueError: If the service type is not supported
            NotImplementedError: If the service type is planned but not yet implemented
        """
        if service_type.lower() == "twitch":
            from .twitch_bot import TwitchBot
            return TwitchBot()
        elif service_type.lower() == "discord":
            # Future implementation
            raise NotImplementedError("Discord bot not yet implemented")
        elif service_type.lower() == "youtube":
            # Future implementation  
            raise NotImplementedError("YouTube bot not yet implemented")
        else:
            raise ValueError(f"Unsupported streaming service: {service_type}")

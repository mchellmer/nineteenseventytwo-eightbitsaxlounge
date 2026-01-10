"""Application configuration settings loaded from environment variables."""

import os
from typing import Optional
from pydantic import BaseSettings


class Settings(BaseSettings):
    """Application settings loaded from environment variables."""
    
    # Twitch Configuration
    twitch_token: str
    twitch_client_id: str
    twitch_channel: str
    twitch_prefix: str = "!"
    
    # MIDI API Configuration
    midi_api_url: str = "http://midi-service:8080"
    midi_api_timeout: int = 30
    
    # Bot Configuration
    bot_name: str = "EightBitSaxBot"
    log_level: str = "INFO"
    
    class Config:
        env_file = ".env"
        case_sensitive = False


# Global settings instance
settings = Settings()

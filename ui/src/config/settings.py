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
    midi_device_url: str = "http://midi-device-service:5000"
    midi_data_url: Optional[str] = None  # Defaults to midi_device_url if not set
    midi_api_timeout: int = 30
    
    # MIDI Authentication
    midi_client_id: str
    midi_client_secret: str
    
    # MIDI Device Configuration
    midi_device_name: str = "One Series Ventris Reverb"
    
    # Bot Configuration
    bot_name: str = "EightBitSaxBot"
    log_level: str = "INFO"
    
    class Config:
        env_file = ".env"
        case_sensitive = False


# Global settings instance
settings = Settings()

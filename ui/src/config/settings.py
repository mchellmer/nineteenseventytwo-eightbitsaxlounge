"""Application configuration settings loaded from environment variables."""

from pydantic import ConfigDict
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """Application settings loaded from environment variables."""
    
    model_config = ConfigDict(
        env_file=".env",
        case_sensitive=False
    )
    
    # Twitch Configuration
    twitch_token: str  # Access token with 'oauth:' prefix
    twitch_client_id: str  # Your app's Client ID from dev.twitch.tv
    twitch_channel: str  # Channel name to connect to
    twitch_prefix: str = "!"
    
    # MIDI API Configuration
    midi_device_url: str = "http://eightbitsaxlounge-midi-service:8080"
    midi_api_timeout: int = 30
    
    # MIDI Authentication
    midi_client_id: str
    midi_client_secret: str
    
    # MIDI Device Configuration
    midi_device_name: str = "One Series Ventris Reverb"
    
    # Bot Configuration
    bot_name: str = "EightBitSaxBot"
    log_level: str = "INFO"


# Global settings instance
settings = Settings()

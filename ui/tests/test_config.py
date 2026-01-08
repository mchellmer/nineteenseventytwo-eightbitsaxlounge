import pytest
from unittest.mock import patch, Mock
from src.config.settings import Settings


class TestSettings:
    """Test cases for Settings configuration."""
    
    def test_settings_default_values(self):
        """Test that settings have expected default values."""
        with patch.dict('os.environ', {
            'TWITCH_TOKEN': 'test_token',
            'TWITCH_CLIENT_ID': 'test_client_id',
            'TWITCH_CHANNEL': 'test_channel'
        }):
            settings = Settings()
            
            assert settings.twitch_prefix == "!"
            assert settings.midi_api_url == "http://midi-service:8080"
            assert settings.midi_api_timeout == 30
            assert settings.bot_name == "EightBitSaxBot"
            assert settings.log_level == "INFO"
            assert settings.health_check_port == 8080
            assert settings.metrics_enabled is True
            assert settings.metrics_port == 9090
    
    def test_settings_from_environment(self):
        """Test that settings load from environment variables."""
        test_env = {
            'TWITCH_TOKEN': 'env_token',
            'TWITCH_CLIENT_ID': 'env_client_id',
            'TWITCH_CHANNEL': 'env_channel',
            'TWITCH_PREFIX': '?',
            'MIDI_API_URL': 'http://custom-midi:9000',
            'BOT_NAME': 'CustomBot',
            'LOG_LEVEL': 'DEBUG'
        }
        
        with patch.dict('os.environ', test_env):
            settings = Settings()
            
            assert settings.twitch_token == 'env_token'
            assert settings.twitch_client_id == 'env_client_id'
            assert settings.twitch_channel == 'env_channel'
            assert settings.twitch_prefix == '?'
            assert settings.midi_api_url == 'http://custom-midi:9000'
            assert settings.bot_name == 'CustomBot'
            assert settings.log_level == 'DEBUG'
    
    def test_settings_case_insensitive(self):
        """Test that environment variable names are case insensitive."""
        test_env = {
            'twitch_token': 'lower_token',
            'TWITCH_CLIENT_ID': 'upper_client_id',
            'Twitch_Channel': 'mixed_channel'
        }
        
        with patch.dict('os.environ', test_env):
            settings = Settings()
            
            assert settings.twitch_token == 'lower_token'
            assert settings.twitch_client_id == 'upper_client_id'
            assert settings.twitch_channel == 'mixed_channel'
"""Pytest configuration and shared fixtures."""

import pytest
from unittest.mock import Mock, AsyncMock
import os
import sys

# Set environment variables before importing any app modules
os.environ.setdefault('TWITCH_TOKEN', 'oauth:test_token')
os.environ.setdefault('TWITCH_CLIENT_ID', 'test_client_id')
os.environ.setdefault('TWITCH_CHANNEL', 'test_channel')
os.environ.setdefault('MIDI_CLIENT_ID', 'test_midi_client')
os.environ.setdefault('MIDI_CLIENT_SECRET', 'test_midi_secret')

# Add src to path for imports
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'src'))


@pytest.fixture
def mock_settings():
    """Mock settings for testing."""
    settings = Mock()
    settings.twitch_token = "oauth:test_token"
    settings.twitch_client_id = "test_client_id"
    settings.twitch_channel = "test_channel"
    settings.twitch_prefix = "!"
    settings.bot_name = "TestBot"
    settings.log_level = "INFO"
    settings.midi_device_url = "http://test-midi:5000"
    settings.midi_data_url = "http://test-midi-data:5001"
    settings.midi_api_timeout = 5
    settings.midi_client_id = "test_midi_client"
    settings.midi_client_secret = "test_midi_secret"
    settings.midi_device_name = "Test MIDI Device"
    return settings


@pytest.fixture
def mock_midi_client():
    """Mock MIDI client for testing."""
    client = Mock()
    client.authenticate = AsyncMock(return_value="test_token")
    client.send_control_change_message = AsyncMock(return_value={"success": True})
    client.set_effect = AsyncMock(return_value={"success": True})
    client.get = AsyncMock(return_value={"status": "ok"})
    client.post = AsyncMock(return_value={"success": True})
    return client


@pytest.fixture
def mock_twitch_context():
    """Mock Twitch command context."""
    context = Mock()
    context.author = Mock()
    context.author.name = "test_user"
    context.channel = Mock()
    context.channel.name = "test_channel"
    context.send = AsyncMock()
    return context


@pytest.fixture
def mock_aiohttp_response():
    """Mock aiohttp response."""
    def create_response(status=200, json_data=None, text_data=None):
        response = AsyncMock()
        response.status = status
        response.json = AsyncMock(return_value=json_data or {})
        response.text = AsyncMock(return_value=text_data or "")
        response.raise_for_status = Mock()
        response.__aenter__ = AsyncMock(return_value=response)
        response.__aexit__ = AsyncMock(return_value=None)
        return response
    return create_response

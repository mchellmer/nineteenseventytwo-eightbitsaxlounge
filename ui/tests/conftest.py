import pytest
import asyncio
from unittest.mock import Mock, AsyncMock, patch
from src.config import Settings

@pytest.fixture
def event_loop():
    """Create an instance of the default event loop for the test session."""
    loop = asyncio.get_event_loop_policy().new_event_loop()
    yield loop
    loop.close()

@pytest.fixture
def mock_settings():
    """Mock settings for testing."""
    return Settings(
        twitch_token="test_token",
        twitch_client_id="test_client_id", 
        twitch_channel="test_channel",
        midi_api_url="http://test-midi:8080",
        bot_name="TestBot",
        log_level="DEBUG"
    )

@pytest.fixture
def mock_twitch_context():
    """Mock Twitch message context."""
    context = Mock()
    context.author = Mock()
    context.author.name = "test_user"
    context.send = AsyncMock()
    context.command = "test_command"
    return context

@pytest.fixture
def mock_aiohttp_session():
    """Mock aiohttp session for API calls."""
    with patch('aiohttp.ClientSession') as mock_session:
        mock_response = AsyncMock()
        mock_response.status = 200
        mock_response.json = AsyncMock(return_value={"status": "success"})
        mock_response.raise_for_status = Mock()
        
        mock_session.return_value.__aenter__.return_value.post.return_value.__aenter__.return_value = mock_response
        mock_session.return_value.__aenter__.return_value.get.return_value.__aenter__.return_value = mock_response
        
        yield mock_session

@pytest.fixture(autouse=True)
def mock_config_settings(mock_settings):
    """Auto-use mock settings in all tests."""
    with patch('src.handlers.settings', mock_settings), \
         patch('src.commands.settings', mock_settings), \
         patch('src.main.settings', mock_settings):
        yield mock_settings
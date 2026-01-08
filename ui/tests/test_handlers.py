import pytest
from unittest.mock import AsyncMock, patch
from src.handlers import EngineHandler, HelpHandler, StatusHandler


class TestEngineHandler:
    """Test cases for EngineHandler."""
    
    @pytest.fixture
    def engine_handler(self):
        return EngineHandler()
    
    @pytest.mark.asyncio
    async def test_handle_engine_command_no_args(self, engine_handler, mock_twitch_context):
        """Test engine command with no arguments."""
        result = await engine_handler.handle_engine_command([], mock_twitch_context)
        
        assert "Usage: !engine <type>" in result
        assert "Available engines:" in result
        assert "room" in result
    
    @pytest.mark.asyncio
    async def test_handle_engine_command_invalid_engine(self, engine_handler, mock_twitch_context):
        """Test engine command with invalid engine type."""
        result = await engine_handler.handle_engine_command(["invalid"], mock_twitch_context)
        
        assert "Invalid engine type: invalid" in result
        assert "Available engines:" in result
    
    @pytest.mark.asyncio
    async def test_handle_engine_command_valid_engine_success(self, engine_handler, mock_twitch_context, mock_aiohttp_session):
        """Test successful engine command."""
        result = await engine_handler.handle_engine_command(["room"], mock_twitch_context)
        
        assert "Engine set to 'room' mode!" in result
        assert "🎵" in result
    
    @pytest.mark.asyncio
    async def test_handle_engine_command_api_error(self, engine_handler, mock_twitch_context):
        """Test engine command when MIDI API fails."""
        with patch('aiohttp.ClientSession') as mock_session:
            # Mock session that raises an error
            mock_session.return_value.__aenter__.return_value.post.side_effect = Exception("API Error")
            
            result = await engine_handler.handle_engine_command(["room"], mock_twitch_context)
            
            assert "Failed to set engine to 'room'" in result
            assert "❌" in result
    
    @pytest.mark.asyncio
    async def test_valid_engines_list(self, engine_handler):
        """Test that valid engines list contains expected values."""
        expected_engines = ["room", "jazz", "ambient", "rock", "electronic"]
        
        assert engine_handler.VALID_ENGINES == expected_engines
    
    @pytest.mark.asyncio 
    async def test_make_midi_request_success(self, engine_handler, mock_aiohttp_session):
        """Test successful MIDI API request."""
        result = await engine_handler.make_midi_request("api/engine", {"engine_type": "room"})
        
        assert result == {"status": "success"}
    
    @pytest.mark.asyncio
    async def test_make_midi_request_get(self, engine_handler, mock_aiohttp_session):
        """Test successful MIDI API GET request."""
        result = await engine_handler.make_midi_request("health")
        
        assert result == {"status": "success"}


class TestHelpHandler:
    """Test cases for HelpHandler."""
    
    @pytest.fixture
    def help_handler(self):
        return HelpHandler()
    
    @pytest.mark.asyncio
    async def test_handle_help_command(self, help_handler, mock_twitch_context):
        """Test help command returns expected content."""
        result = await help_handler.handle_help_command([], mock_twitch_context)
        
        assert "EightBitSaxLounge Bot Commands" in result
        assert "!engine <type>" in result
        assert "!status" in result
        assert "!help" in result
        assert "github.com" in result


class TestStatusHandler:
    """Test cases for StatusHandler."""
    
    @pytest.fixture
    def status_handler(self):
        return StatusHandler()
    
    @pytest.mark.asyncio
    async def test_handle_status_command_success(self, status_handler, mock_twitch_context, mock_aiohttp_session):
        """Test successful status command."""
        # Mock successful MIDI health check
        with patch.object(status_handler, 'make_midi_request', return_value={"status": "healthy"}):
            result = await status_handler.handle_status_command([], mock_twitch_context)
            
            assert "Bot: Online" in result
            assert "✅ Healthy" in result
            assert "Channel: #test_channel" in result
    
    @pytest.mark.asyncio
    async def test_handle_status_command_midi_unhealthy(self, status_handler, mock_twitch_context):
        """Test status command when MIDI service is unhealthy."""
        with patch.object(status_handler, 'make_midi_request', return_value={"status": "unhealthy"}):
            result = await status_handler.handle_status_command([], mock_twitch_context)
            
            assert "Bot: Online" in result
            assert "❌ Unhealthy" in result
    
    @pytest.mark.asyncio
    async def test_handle_status_command_midi_unavailable(self, status_handler, mock_twitch_context):
        """Test status command when MIDI service is unavailable."""
        with patch.object(status_handler, 'make_midi_request', side_effect=Exception("Connection failed")):
            result = await status_handler.handle_status_command([], mock_twitch_context)
            
            assert "Bot: Online" in result
            assert "❌ Unavailable" in result
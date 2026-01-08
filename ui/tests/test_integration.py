"""
Integration test example showing how to test the full bot workflow.
This demonstrates testing the complete command flow from message to response.
"""

import pytest
from unittest.mock import AsyncMock, patch, Mock
from src.main import EightBitSaxBot


class TestBotIntegration:
    """Integration tests for the complete bot workflow."""
    
    @pytest.fixture
    def mock_twitchio(self):
        """Mock TwitchIO components for integration testing."""
        with patch('src.main.commands.Bot.__init__', return_value=None), \
             patch('src.main.start_http_server'):
            yield
    
    @pytest.fixture
    async def bot_instance(self, mock_twitchio, mock_settings):
        """Create a fully initialized bot for integration testing."""
        bot = EightBitSaxBot()
        bot.nick = "testbot"
        bot._shutdown = False
        return bot
    
    @pytest.mark.asyncio
    async def test_engine_command_full_flow(self, bot_instance, mock_aiohttp_session):
        """Test complete engine command flow from parsing to MIDI API call."""
        # Create mock context
        mock_ctx = Mock()
        mock_ctx.author = Mock()
        mock_ctx.author.name = "test_user"
        mock_ctx.send = AsyncMock()
        
        # Execute the engine command
        await bot_instance.engine_command(mock_ctx, "room")
        
        # Verify the response was sent
        mock_ctx.send.assert_called_once()
        call_args = mock_ctx.send.call_args[0][0]
        assert "Engine set to 'room' mode!" in call_args
        assert "🎵" in call_args
    
    @pytest.mark.asyncio
    async def test_help_command_full_flow(self, bot_instance):
        """Test complete help command flow."""
        mock_ctx = Mock()
        mock_ctx.send = AsyncMock()
        
        await bot_instance.help_command(mock_ctx)
        
        mock_ctx.send.assert_called_once()
        call_args = mock_ctx.send.call_args[0][0]
        assert "EightBitSaxLounge Bot Commands" in call_args
        assert "!engine <type>" in call_args
    
    @pytest.mark.asyncio
    async def test_status_command_full_flow(self, bot_instance, mock_aiohttp_session):
        """Test complete status command flow."""
        mock_ctx = Mock()
        mock_ctx.send = AsyncMock()
        
        # Mock MIDI service health check
        mock_aiohttp_session.return_value.__aenter__.return_value.get.return_value.__aenter__.return_value.json.return_value = {
            "status": "healthy"
        }
        
        await bot_instance.status_command(mock_ctx)
        
        mock_ctx.send.assert_called_once()
        call_args = mock_ctx.send.call_args[0][0]
        assert "Bot: Online" in call_args
        assert "✅ Healthy" in call_args
    
    @pytest.mark.asyncio
    async def test_unknown_command_handling(self, bot_instance):
        """Test how bot handles unknown commands through the full flow."""
        mock_ctx = Mock()
        mock_ctx.send = AsyncMock()
        
        # Simulate an unknown command by calling _execute_command directly
        await bot_instance._execute_command("unknown", [], mock_ctx)
        
        mock_ctx.send.assert_called_once()
        call_args = mock_ctx.send.call_args[0][0]
        assert "Unknown command: unknown" in call_args
        assert "Type !help for available commands" in call_args
    
    @pytest.mark.asyncio
    async def test_command_with_midi_api_failure(self, bot_instance):
        """Test command handling when MIDI API is down."""
        mock_ctx = Mock()
        mock_ctx.author = Mock()
        mock_ctx.author.name = "test_user"
        mock_ctx.send = AsyncMock()
        
        # Mock aiohttp to raise an exception
        with patch('aiohttp.ClientSession') as mock_session:
            mock_session.return_value.__aenter__.return_value.post.side_effect = Exception("Connection failed")
            
            await bot_instance.engine_command(mock_ctx, "room")
            
        mock_ctx.send.assert_called_once()
        call_args = mock_ctx.send.call_args[0][0]
        assert "Failed to set engine to 'room'" in call_args
        assert "❌" in call_args
    
    @pytest.mark.asyncio 
    async def test_message_processing_flow(self, bot_instance):
        """Test the complete message processing flow."""
        # Create mock message
        mock_message = Mock()
        mock_message.echo = False
        mock_message.author = Mock()
        mock_message.author.name = "test_user"
        mock_message.content = "!help"
        
        with patch.object(bot_instance, 'handle_commands') as mock_handle:
            await bot_instance.event_message(mock_message)
            
        mock_handle.assert_called_once_with(mock_message)
    
    @pytest.mark.asyncio
    async def test_bot_startup_and_shutdown(self, mock_twitchio, mock_settings):
        """Test bot startup and shutdown process."""
        bot = EightBitSaxBot()
        
        # Test startup
        await bot.event_ready()
        
        # Test shutdown
        with patch.object(bot, 'close') as mock_close:
            await bot.shutdown()
            
        assert bot._shutdown is True
        mock_close.assert_called_once()
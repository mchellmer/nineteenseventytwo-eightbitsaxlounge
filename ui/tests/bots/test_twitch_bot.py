"""Tests for TwitchBot."""

import pytest
from unittest.mock import Mock, AsyncMock, patch, MagicMock
from bots.twitch_bot import TwitchBot


class TestTwitchBot:
    """Test cases for TwitchBot."""
    
    @pytest.fixture
    def mock_settings(self):
        """Mock settings for TwitchBot."""
        settings = Mock()
        settings.twitch_token = "oauth:test_token"
        settings.twitch_client_id = "test_client_id"
        settings.twitch_channel = "test_channel"
        settings.twitch_prefix = "!"
        settings.bot_name = "TestBot"
        settings.log_level = "INFO"
        return settings
    
    @pytest.fixture
    def twitch_bot(self, mock_settings):
        """Create a TwitchBot instance for testing."""
        with patch('bots.twitch_bot.settings', mock_settings):
            with patch('bots.twitch_bot.commands.Bot'):
                with patch('bots.twitch_bot.CommandRegistry'):
                    with patch('bots.twitch_bot.TwitchClient'):
                        bot = TwitchBot()
                        return bot
    
    def test_bot_initialization(self, twitch_bot):
        """Test bot initializes correctly with TwitchIO components."""
        assert twitch_bot is not None
        assert hasattr(twitch_bot, 'twitchio')
        assert hasattr(twitch_bot, 'command_registry')
    
    @pytest.mark.asyncio
    async def test_start_validates_token(self, twitch_bot):
        """Test start method validates token."""
        twitch_bot.token_validator = Mock()
        twitch_bot.token_validator.validate_and_warn = AsyncMock()
        twitch_bot.twitchio.start = AsyncMock()
        
        with patch('bots.twitch_bot.settings') as mock_settings:
            mock_settings.twitch_token = "oauth:test_token"
            await twitch_bot.start()
            
            twitch_bot.token_validator.validate_and_warn.assert_called_once()
    
    @pytest.mark.asyncio
    async def test_on_ready_sets_connected(self, twitch_bot):
        """Test _on_ready sets connected flag."""
        twitch_bot.twitchio.nick = "TestBot"
        
        await twitch_bot._on_ready()
        
        assert twitch_bot._connected is True
    
    @pytest.mark.asyncio
    async def test_on_message_ignores_echo(self, twitch_bot):
        """Test _on_message ignores echo messages."""
        message = Mock()
        message.echo = True
        twitch_bot.twitchio.handle_commands = AsyncMock()
        
        await twitch_bot._on_message(message)
        
        twitch_bot.twitchio.handle_commands.assert_not_called()
    
    @pytest.mark.asyncio
    async def test_on_message_handles_commands(self, twitch_bot):
        """Test _on_message handles non-echo messages."""
        message = Mock()
        message.echo = False
        message.author = Mock()
        message.author.name = "test_user"
        message.content = "!engine room"
        twitch_bot.twitchio.handle_commands = AsyncMock()
        
        await twitch_bot._on_message(message)
        
        twitch_bot.twitchio.handle_commands.assert_called_once_with(message)
    
    @pytest.mark.asyncio
    async def test_execute_command_success(self, twitch_bot):
        """Test successful command execution."""
        ctx = Mock()
        ctx.send = AsyncMock()
        twitch_bot.command_registry.execute_command = AsyncMock(
            return_value="Command executed successfully"
        )
        
        await twitch_bot._execute_command("engine", ["room"], ctx)
        
        ctx.send.assert_called_once_with("Command executed successfully")
    
    @pytest.mark.asyncio
    async def test_execute_command_error(self, twitch_bot):
        """Test command execution with error."""
        ctx = Mock()
        ctx.send = AsyncMock()
        twitch_bot.command_registry.execute_command = AsyncMock(
            side_effect=Exception("Command failed")
        )
        
        await twitch_bot._execute_command("engine", ["room"], ctx)
        
        # Should send error message
        assert ctx.send.called
        call_args = ctx.send.call_args[0][0]
        assert "error" in call_args.lower()

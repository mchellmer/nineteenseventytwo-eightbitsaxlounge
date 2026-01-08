import pytest
from unittest.mock import Mock, AsyncMock, patch
from src.bots.twitch_bot import EightBitSaxBot
from src.config.settings import Settings


class TestEightBitSaxBot:
    """Test cases for EightBitSaxBot."""
    
    @pytest.fixture
    def mock_bot_init(self):
        """Mock the bot initialization to avoid actual Twitch connection."""
        with patch('src.main.commands.Bot.__init__', return_value=None):
            yield
    
    @pytest.fixture
    def bot(self, mock_bot_init, mock_settings):
        """Create a bot instance for testing."""
        bot = EightBitSaxBot()
        bot.nick = "testbot"
        bot._shutdown = False
        return bot
    
    @pytest.mark.asyncio
    async def test_bot_initialization(self, mock_bot_init, mock_settings):
        """Test that bot initializes correctly."""
        bot = EightBitSaxBot()
        
        assert bot.command_registry is not None
        assert bot._shutdown is False
    
    @pytest.mark.asyncio
    async def test_event_ready(self, bot, caplog):
        """Test the event_ready handler."""
        with patch('src.main.start_http_server'):
            await bot.event_ready()
            
        assert "is online and connected" in caplog.text
    
    @pytest.mark.asyncio
    async def test_event_message_ignores_echo(self, bot):
        """Test that bot ignores its own messages."""
        mock_message = Mock()
        mock_message.echo = True
        
        with patch.object(bot, 'handle_commands') as mock_handle:
            await bot.event_message(mock_message)
            
        mock_handle.assert_not_called()
    
    @pytest.mark.asyncio
    async def test_event_message_handles_commands(self, bot):
        """Test that bot processes non-echo messages."""
        mock_message = Mock()
        mock_message.echo = False
        mock_message.author.name = "test_user"
        mock_message.content = "!engine room"
        
        with patch.object(bot, 'handle_commands') as mock_handle:
            await bot.event_message(mock_message)
            
        mock_handle.assert_called_once_with(mock_message)
    
    @pytest.mark.asyncio
    async def test_event_command_error(self, bot, caplog):
        """Test command error handling."""
        mock_context = Mock()
        mock_context.command = "test_command"
        mock_context.send = AsyncMock()
        
        error = Exception("Test error")
        
        await bot.event_command_error(mock_context, error)
        
        assert "Command error in test_command" in caplog.text
        mock_context.send.assert_called_once_with('❌ An error occurred while processing your command.')
    
    @pytest.mark.asyncio
    async def test_execute_command_success(self, bot):
        """Test successful command execution."""
        mock_ctx = Mock()
        mock_ctx.send = AsyncMock()
        
        with patch.object(bot.command_registry, 'execute_command', return_value="Success response") as mock_execute:
            await bot._execute_command("test", ["arg1"], mock_ctx)
            
        mock_execute.assert_called_once_with("test", ["arg1"], mock_ctx)
        mock_ctx.send.assert_called_once_with("Success response")
    
    @pytest.mark.asyncio
    async def test_execute_command_error(self, bot):
        """Test command execution with error."""
        mock_ctx = Mock()
        mock_ctx.send = AsyncMock()
        
        with patch.object(bot.command_registry, 'execute_command', side_effect=Exception("Command failed")):
            await bot._execute_command("test", ["arg1"], mock_ctx)
            
        mock_ctx.send.assert_called_once_with('❌ An error occurred while processing your command.')
    
    @pytest.mark.asyncio
    async def test_engine_command(self, bot):
        """Test engine command wrapper."""
        mock_ctx = Mock()
        
        with patch.object(bot, '_execute_command') as mock_execute:
            await bot.engine_command(mock_ctx, "room", "reverb")
            
        mock_execute.assert_called_once_with("engine", ["room", "reverb"], mock_ctx)
    
    @pytest.mark.asyncio
    async def test_help_command(self, bot):
        """Test help command wrapper."""
        mock_ctx = Mock()
        
        with patch.object(bot, '_execute_command') as mock_execute:
            await bot.help_command(mock_ctx)
            
        mock_execute.assert_called_once_with("help", [], mock_ctx)
    
    @pytest.mark.asyncio
    async def test_status_command(self, bot):
        """Test status command wrapper."""
        mock_ctx = Mock()
        
        with patch.object(bot, '_execute_command') as mock_execute:
            await bot.status_command(mock_ctx, "detailed")
            
        mock_execute.assert_called_once_with("status", ["detailed"], mock_ctx)
    
    @pytest.mark.asyncio
    async def test_shutdown(self, bot):
        """Test graceful shutdown."""
        with patch.object(bot, 'close') as mock_close:
            await bot.shutdown()
            
        assert bot._shutdown is True
        mock_close.assert_called_once()


class TestMainFunction:
    """Test cases for the main function and signal handling."""
    
    @pytest.mark.asyncio
    async def test_main_function_startup(self):
        """Test main function startup process."""
        mock_bot = Mock()
        mock_bot.start = AsyncMock()
        mock_bot.shutdown = AsyncMock()
        
        with patch('src.main.EightBitSaxBot', return_value=mock_bot), \
             patch('signal.signal'), \
             patch('src.main.logger'):
            
            # Mock KeyboardInterrupt to simulate graceful shutdown
            mock_bot.start.side_effect = KeyboardInterrupt()
            
            from src.main import main
            await main()
            
        mock_bot.start.assert_called_once()
        mock_bot.shutdown.assert_called_once()
    
    @pytest.mark.asyncio
    async def test_main_function_exception(self):
        """Test main function handles exceptions."""
        mock_bot = Mock()
        mock_bot.start = AsyncMock(side_effect=Exception("Fatal error"))
        mock_bot.shutdown = AsyncMock()
        
        with patch('src.main.EightBitSaxBot', return_value=mock_bot), \
             patch('signal.signal'), \
             patch('src.main.logger'), \
             patch('sys.exit') as mock_exit:
            
            from src.main import main
            await main()
            
        mock_exit.assert_called_once_with(1)
        mock_bot.shutdown.assert_called_once()
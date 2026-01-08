import pytest
from unittest.mock import AsyncMock, Mock
from src.commands.registry import CommandRegistry


class TestCommandRegistry:
    """Test cases for CommandRegistry."""
    
    @pytest.fixture
    def registry(self):
        return CommandRegistry()
    
    def test_initialization(self, registry):
        """Test that CommandRegistry initializes with expected handlers."""
        assert registry._engine_handler is not None
        assert registry._help_handler is not None
        assert registry._status_handler is not None
        assert len(registry._commands) >= 3
    
    @pytest.mark.asyncio
    async def test_execute_command_valid(self, registry, mock_twitch_context):
        """Test executing a valid command."""
        result = await registry.execute_command("help", [], mock_twitch_context)
        
        assert "EightBitSaxLounge" in result
    
    @pytest.mark.asyncio
    async def test_execute_command_invalid(self, registry, mock_twitch_context):
        """Test executing an invalid command raises ValueError."""
        with pytest.raises(ValueError):
            await registry.execute_command("nonexistent", [], mock_twitch_context)
    
    def test_get_all_commands(self, registry):
        """Test getting all available commands."""
        commands = registry.get_all_commands()
        
        assert isinstance(commands, dict)
        assert "engine" in commands
        assert "help" in commands
        assert "status" in commands
        
        # Check that descriptions are provided
        for command, description in commands.items():
            assert isinstance(description, str)
            assert len(description) > 0
    
    def test_is_valid_command(self, registry):
        """Test command validation."""
        assert registry.is_valid_command("engine") is True
        assert registry.is_valid_command("help") is True
        assert registry.is_valid_command("status") is True
        assert registry.is_valid_command("ENGINE") is True  # Case insensitive
        assert registry.is_valid_command("nonexistent") is False
        assert registry.is_valid_command("") is False
    
    @pytest.mark.asyncio
    async def test_execute_command_valid(self, registry, mock_twitch_context):
        """Test executing a valid command."""
        # Mock the handler method
        mock_handler = AsyncMock(return_value="Test response")
        registry._commands["test"] = (mock_handler, "Test command")
        
        result = await registry.execute_command("test", ["arg1"], mock_twitch_context)
        
        assert result == "Test response"
        mock_handler.assert_called_once_with(["arg1"], mock_twitch_context)
    
    @pytest.mark.asyncio
    async def test_execute_command_invalid(self, registry, mock_twitch_context):
        """Test executing an invalid command."""
        result = await registry.execute_command("nonexistent", [], mock_twitch_context)
        
        assert "Unknown command: nonexistent" in result
        assert "Type !help for available commands" in result
    
    @pytest.mark.asyncio
    async def test_execute_command_handler_exception(self, registry, mock_twitch_context):
        """Test executing a command when handler raises an exception."""
        # Mock the handler method to raise an exception
        mock_handler = AsyncMock(side_effect=Exception("Handler error"))
        registry._commands["test"] = (mock_handler, "Test command")
        
        result = await registry.execute_command("test", [], mock_twitch_context)
        
        assert "Error executing command: Handler error" in result
    
    @pytest.mark.asyncio
    async def test_execute_command_case_insensitive(self, registry, mock_twitch_context):
        """Test that command execution is case insensitive."""
        mock_handler = AsyncMock(return_value="Test response")
        registry._commands["test"] = (mock_handler, "Test command")
        
        result1 = await registry.execute_command("test", [], mock_twitch_context)
        result2 = await registry.execute_command("TEST", [], mock_twitch_context)
        result3 = await registry.execute_command("Test", [], mock_twitch_context)
        
        assert result1 == result2 == result3 == "Test response"
        assert mock_handler.call_count == 3
    
    def test_commands_have_required_structure(self, registry):
        """Test that all registered commands have the correct structure."""
        for command_name, (handler, description) in registry._commands.items():
            assert isinstance(command_name, str)
            assert len(command_name) > 0
            assert callable(handler)
            assert isinstance(description, str)
            assert len(description) > 0
    
    def test_default_commands_present(self, registry):
        """Test that default commands are present."""
        expected_commands = ["engine", "help", "status"]
        
        for command in expected_commands:
            assert registry.is_valid_command(command), f"Command '{command}' should be present"
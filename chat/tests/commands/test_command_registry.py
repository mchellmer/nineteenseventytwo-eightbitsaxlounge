"""Tests for command registry."""

import pytest
from unittest.mock import Mock, AsyncMock, patch
from commands.command_registry import CommandRegistry


class TestCommandRegistry:
    """Test cases for CommandRegistry."""
    
    @pytest.fixture
    def command_registry(self, mock_settings):
        """Create a command registry instance."""
        with patch('commands.command_registry.settings', mock_settings):
            with patch('commands.command_registry.MidiClient') as mock_client_class:
                mock_client = Mock()
                mock_client.send_control_change_message = AsyncMock(return_value={"success": True})
                mock_client.get = AsyncMock(return_value={"status": "ok"})
                mock_client_class.return_value = mock_client
                
                registry = CommandRegistry()
                return registry
    
    @pytest.mark.asyncio
    async def test_execute_engine_command(self, command_registry, mock_twitch_context):
        """Test executing engine command."""
        response = await command_registry.execute_command("engine", ["room"], mock_twitch_context)
        
        assert isinstance(response, str)
        assert len(response) > 0
    
    @pytest.mark.asyncio
    async def test_execute_help_command(self, command_registry, mock_twitch_context):
        """Test executing help command."""
        response = await command_registry.execute_command("help", [], mock_twitch_context)
        
        # Help returns a list of messages
        assert isinstance(response, list)
        full_response = ' '.join(response).lower()
        assert "command" in full_response
    
    @pytest.mark.asyncio
    async def test_execute_unknown_command(self, command_registry, mock_twitch_context):
        """Test executing unknown command raises ValueError."""
        with pytest.raises(ValueError, match="Unknown command: unknown"):
            await command_registry.execute_command("unknown", [], mock_twitch_context)
    
    @pytest.mark.asyncio
    async def test_execute_command_with_error(self, command_registry, mock_twitch_context):
        """Test command execution with error propagates exception."""
        # Mock the handler to raise an exception
        registry_commands = command_registry._commands
        
        async def failing_handler(*args, **kwargs):
            raise Exception("Test error")
        
        registry_commands["engine"] = (failing_handler, "Test command")
        
        with pytest.raises(Exception, match="Test error"):
            await command_registry.execute_command("engine", ["room"], mock_twitch_context)
    
    def test_get_all_commands(self, command_registry):
        """Test getting all registered commands."""
        commands = command_registry.get_all_commands()
        
        assert "engine" in commands
        assert "help" in commands
    
    def test_get_command_descriptions(self, command_registry):
        """Test getting all command descriptions."""
        commands = command_registry.get_all_commands()
        
        assert "engine" in commands
        assert commands["engine"] is not None
        assert "engine" in commands["engine"].lower()

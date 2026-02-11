"""Tests for HelpHandler."""

import pytest
from commands.handlers.help import HelpHandler


class TestHelpHandler:
    """Test cases for HelpHandler."""
    
    @pytest.fixture
    def help_handler(self):
        """Create a help handler instance."""
        return HelpHandler()
    
    @pytest.mark.asyncio
    async def test_handle_general_help(self, help_handler, mock_twitch_context):
        """Test general help command."""
        response = await help_handler.handle([], mock_twitch_context)
        
        assert isinstance(response, list)
        assert len(response) > 0
        # Join all messages to check content
        full_response = ' '.join(response)
        assert "🎵 EightBitSaxLounge Bot Commands 🎵" in full_response
        assert "!engine" in full_response
    
    @pytest.mark.asyncio
    async def test_handle_specific_command_help(self, help_handler, mock_twitch_context):
        """Test help for specific command (help doesn't use args, returns general help)."""
        response = await help_handler.handle(["engine"], mock_twitch_context)
        
        assert isinstance(response, list)
        full_response = ' '.join(response).lower()
        assert "engine" in full_response
    
    @pytest.mark.asyncio
    async def test_handle_multiple_args(self, help_handler, mock_twitch_context):
        """Test help with multiple arguments (should handle gracefully)."""
        response = await help_handler.handle(["engine", "room"], mock_twitch_context)
        
        assert isinstance(response, list)
        assert len(response) > 0

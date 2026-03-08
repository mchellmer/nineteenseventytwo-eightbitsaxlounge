"""Tests for CommandHandler interface."""

import pytest
from commands.handlers.command_handler import CommandHandler


class TestCommandHandler:
    """Test cases for CommandHandler interface contract."""
    
    class MockHandler(CommandHandler):
        """Concrete test implementation of CommandHandler."""
        
        def __init__(self):
            self._name = "testcommand"
            self._desc = "Test command description"
        
        async def handle(self, args, context):
            """Test implementation of handle."""
            return "Test response"
        
        @property
        def command_name(self):
            """Test command name."""
            return self._name
        
        @property
        def description(self):
            """Test description."""
            return self._desc
    
    @pytest.fixture
    def test_handler(self):
        """Create a test handler instance."""
        return self.MockHandler()
    
    @pytest.mark.asyncio
    async def test_handle_returns_string(self, test_handler, mock_twitch_context):
        """Test that handle method returns a string."""
        result = await test_handler.handle([], mock_twitch_context)
        
        assert isinstance(result, str)
        assert len(result) > 0
    
    def test_command_name_property_exists(self, test_handler):
        """Test that command_name property exists and returns a string."""
        name = test_handler.command_name
        
        assert isinstance(name, str)
        assert len(name) > 0
    
    def test_description_property_exists(self, test_handler):
        """Test that description property exists and returns a string."""
        description = test_handler.description
        
        assert isinstance(description, str)
        assert len(description) > 0

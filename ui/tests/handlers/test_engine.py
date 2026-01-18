"""Tests for EngineHandler."""

import pytest
from unittest.mock import AsyncMock
from commands.handlers.engine import EngineHandler


class TestEngineHandler:
    """Test cases for EngineHandler."""
    
    @pytest.fixture
    def engine_handler(self, mock_midi_client):
        """Create an engine handler instance."""
        return EngineHandler(mock_midi_client)
    
    @pytest.mark.asyncio
    async def test_handle_valid_engine(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test handling valid engine command."""
        response = await engine_handler.handle(["room"], mock_twitch_context)
        
        assert "Engine set to 'room' mode!" in response
        assert "🎵" in response
        mock_midi_client.send_control_change_message.assert_called_once()
    
    @pytest.mark.asyncio
    async def test_handle_invalid_engine(self, engine_handler, mock_twitch_context):
        """Test handling invalid engine type."""
        response = await engine_handler.handle(["invalid"], mock_twitch_context)
        
        assert "Invalid engine type" in response
        assert "invalid" in response
    
    @pytest.mark.asyncio
    async def test_handle_no_args(self, engine_handler, mock_twitch_context):
        """Test handling command with no arguments."""
        response = await engine_handler.handle([], mock_twitch_context)
        
        assert "Usage:" in response
        assert "room" in response
    
    @pytest.mark.asyncio
    async def test_handle_midi_error(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test handling MIDI service error."""
        mock_midi_client.send_control_change_message = AsyncMock(
            side_effect=Exception("MIDI service unavailable")
        )
        
        response = await engine_handler.handle(["room"], mock_twitch_context)
        
        assert "Failed to set engine" in response
        assert "❌" in response
    
    @pytest.mark.asyncio
    async def test_handle_case_insensitive(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test engine type is case-insensitive."""
        response = await engine_handler.handle(["ROOM"], mock_twitch_context)
        
        assert "Engine set to 'room' mode!" in response
        mock_midi_client.send_control_change_message.assert_called_once()

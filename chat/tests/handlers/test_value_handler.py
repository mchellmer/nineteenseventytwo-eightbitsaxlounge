"""Tests for the ValueHandler command handler."""

import pytest
from unittest.mock import AsyncMock, MagicMock

from commands.handlers.value_handler import ValueHandler, scale_value_to_midi
from services.midi_client import MidiClient


class TestScaleValueToMidi:
    """Tests for the scale_value_to_midi helper function."""
    
    def test_scale_min_value(self):
        """Test scaling minimum value (0 -> 0)."""
        result = scale_value_to_midi(0, 0, 10)
        assert result == 0
    
    def test_scale_max_value(self):
        """Test scaling maximum value (10 -> 127)."""
        result = scale_value_to_midi(10, 0, 10)
        assert result == 127
    
    def test_scale_middle_value(self):
        """Test scaling middle value (5 -> 64)."""
        result = scale_value_to_midi(5, 0, 10)
        assert result == 64  # 63.5 rounds to 64
    
    def test_scale_one(self):
        """Test scaling value 1 (1 -> 13)."""
        result = scale_value_to_midi(1, 0, 10)
        assert result == 13  # 12.7 rounds to 13
    
    def test_scale_custom_range(self):
        """Test scaling with custom input range."""
        result = scale_value_to_midi(50, 0, 100)
        assert result == 64  # 63.5 rounds to 64
    
    def test_scale_value_below_range(self):
        """Test that values below range raise ValueError."""
        with pytest.raises(ValueError, match="outside the range"):
            scale_value_to_midi(-1, 0, 10)
    
    def test_scale_value_above_range(self):
        """Test that values above range raise ValueError."""
        with pytest.raises(ValueError, match="outside the range"):
            scale_value_to_midi(11, 0, 10)


class TestValueHandler:
    """Tests for the ValueHandler class."""
    
    @pytest.fixture
    def mock_midi_client(self):
        """Create a mock MIDI client."""
        client = MagicMock(spec=MidiClient)
        client.set_effect = AsyncMock()
        return client
    
    @pytest.fixture
    def value_handler(self, mock_midi_client):
        """Create a ValueHandler instance for testing."""
        return ValueHandler(
            midi_client=mock_midi_client,
            command_name="time",
            device_name="VentrisDualReverb",
            device_effect_name="ReverbEngineA",
            device_effect_setting_name="Time",
            min_value=0,
            max_value=10
        )
    
    @pytest.fixture
    def mock_twitch_context(self):
        """Create a mock Twitch context."""
        context = MagicMock()
        context.author.name = "testuser"
        return context
    
    @pytest.mark.asyncio
    async def test_handle_valid_value(self, value_handler, mock_twitch_context, mock_midi_client):
        """Test handling a valid value command."""
        result = await value_handler.handle(["5"], mock_twitch_context)
        
        # Check response
        assert "time set to 5" in result.lower()
        assert "🎵" in result
        
        # Verify MIDI call with scaled value (5 -> 64)
        mock_midi_client.set_effect.assert_called_once_with(
            device_name="VentrisDualReverb",
            device_effect_name="ReverbEngineA",
            device_effect_setting_name="Time",
            value=64
        )
    
    @pytest.mark.asyncio
    async def test_handle_min_value(self, value_handler, mock_twitch_context, mock_midi_client):
        """Test handling minimum value (0)."""
        result = await value_handler.handle(["0"], mock_twitch_context)
        
        assert "time set to 0" in result.lower()
        mock_midi_client.set_effect.assert_called_once()
        call_args = mock_midi_client.set_effect.call_args
        assert call_args.kwargs['value'] == 0
    
    @pytest.mark.asyncio
    async def test_handle_max_value(self, value_handler, mock_twitch_context, mock_midi_client):
        """Test handling maximum value (10)."""
        result = await value_handler.handle(["10"], mock_twitch_context)
        
        assert "time set to 10" in result.lower()
        mock_midi_client.set_effect.assert_called_once()
        call_args = mock_midi_client.set_effect.call_args
        assert call_args.kwargs['value'] == 127
    
    @pytest.mark.asyncio
    async def test_handle_value_one(self, value_handler, mock_twitch_context, mock_midi_client):
        """Test handling value 1 (should scale to 13)."""
        result = await value_handler.handle(["1"], mock_twitch_context)
        
        assert "time set to 1" in result.lower()
        mock_midi_client.set_effect.assert_called_once()
        call_args = mock_midi_client.set_effect.call_args
        assert call_args.kwargs['value'] == 13
    
    @pytest.mark.asyncio
    async def test_handle_no_args(self, value_handler, mock_twitch_context, mock_midi_client):
        """Test handling command without arguments."""
        result = await value_handler.handle([], mock_twitch_context)
        
        assert "usage" in result.lower()
        assert "0-10" in result.lower()
        mock_midi_client.set_effect.assert_not_called()
    
    @pytest.mark.asyncio
    async def test_handle_invalid_number(self, value_handler, mock_twitch_context, mock_midi_client):
        """Test handling invalid numeric input."""
        result = await value_handler.handle(["abc"], mock_twitch_context)
        
        assert "❌" in result
        assert "invalid" in result.lower()
        mock_midi_client.set_effect.assert_not_called()
    
    @pytest.mark.asyncio
    async def test_handle_value_too_low(self, value_handler, mock_twitch_context, mock_midi_client):
        """Test handling value below minimum."""
        result = await value_handler.handle(["-1"], mock_twitch_context)
        
        assert "❌" in result
        assert "between 0 and 10" in result.lower()
        mock_midi_client.set_effect.assert_not_called()
    
    @pytest.mark.asyncio
    async def test_handle_value_too_high(self, value_handler, mock_twitch_context, mock_midi_client):
        """Test handling value above maximum."""
        result = await value_handler.handle(["11"], mock_twitch_context)
        
        assert "❌" in result
        assert "between 0 and 10" in result.lower()
        mock_midi_client.set_effect.assert_not_called()
    
    @pytest.mark.asyncio
    async def test_handle_decimal_value(self, value_handler, mock_twitch_context, mock_midi_client):
        """Test handling decimal value."""
        result = await value_handler.handle(["5.5"], mock_twitch_context)
        
        assert "time set to 5.5" in result.lower()
        mock_midi_client.set_effect.assert_called_once()
        call_args = mock_midi_client.set_effect.call_args
        # 5.5 / 10 * 127 = 69.85 -> rounds to 70
        assert call_args.kwargs['value'] == 70
    
    @pytest.mark.asyncio
    async def test_handle_api_error(self, value_handler, mock_twitch_context, mock_midi_client):
        """Test handling API error during set_effect call."""
        mock_midi_client.set_effect.side_effect = Exception("API Error")
        
        result = await value_handler.handle(["5"], mock_twitch_context)
        
        assert "❌" in result
        assert "failed" in result.lower()
    
    def test_command_name_property(self, value_handler):
        """Test command_name property."""
        assert value_handler.command_name == "time"
    
    def test_description_property(self, value_handler):
        """Test description property."""
        description = value_handler.description
        assert "time" in description.lower()
        assert "0-10" in description

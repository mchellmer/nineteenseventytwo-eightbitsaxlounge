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
    async def test_handle_valid_engine_lowercase(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test handling valid engine command in lowercase."""
        response = await engine_handler.handle(["room"], mock_twitch_context)
        
        assert "Engine set to 'room' mode!" in response
        assert "🎵" in response
        mock_midi_client.set_effect.assert_called_once_with(
            device_name="VentrisDualReverb",
            device_effect_name="ReverbEngineA",
            device_effect_setting_name="ReverbEngine",
            selection="Room"
        )
    
    @pytest.mark.asyncio
    async def test_handle_valid_engine_uppercase(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test handling valid engine command in uppercase."""
        response = await engine_handler.handle(["HALL"], mock_twitch_context)
        
        assert "Engine set to 'hall' mode!" in response
        assert "🎵" in response
        mock_midi_client.set_effect.assert_called_once_with(
            device_name="VentrisDualReverb",
            device_effect_name="ReverbEngineA",
            device_effect_setting_name="ReverbEngine",
            selection="Hall"
        )
    
    @pytest.mark.asyncio
    async def test_handle_valid_engine_mixedcase(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test handling valid engine command in mixed case."""
        response = await engine_handler.handle(["ShImMeR"], mock_twitch_context)
        
        assert "Engine set to 'shimmer' mode!" in response
        assert "🎵" in response
        mock_midi_client.set_effect.assert_called_once_with(
            device_name="VentrisDualReverb",
            device_effect_name="ReverbEngineA",
            device_effect_setting_name="ReverbEngine",
            selection="Shimmer"
        )
    
    @pytest.mark.asyncio
    async def test_handle_all_valid_engines(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test all valid engine types work correctly."""
        engines_to_test = [
            ("room", "Room"),
            ("plate", "Plate"),
            ("edome", "EDome"),
            ("truespring", "TrueSpring"),
            ("lofi", "LoFi"),
            ("modverb", "ModVerb"),
            ("echoVerb", "EchoVerb"),
            ("swell", "Swell"),
            ("offspring", "Offspring"),
            ("reverse", "Reverse"),
            ("outboardspring", "OutboardSpring"),
            ("metalbox", "MetalBox")
        ]
        
        for input_engine, expected_selection in engines_to_test:
            mock_midi_client.set_effect.reset_mock()
            response = await engine_handler.handle([input_engine], mock_twitch_context)
            
            assert f"Engine set to '{input_engine.lower()}' mode!" in response
            mock_midi_client.set_effect.assert_called_once_with(
                device_name="VentrisDualReverb",
                device_effect_name="ReverbEngineA",
                device_effect_setting_name="ReverbEngine",
                selection=expected_selection
            )
    
    @pytest.mark.asyncio
    async def test_handle_invalid_engine(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test handling invalid engine type."""
        response = await engine_handler.handle(["invalid"], mock_twitch_context)
        
        assert "Invalid engine type" in response
        assert "invalid" in response
        mock_midi_client.set_effect.assert_not_called()
    
    @pytest.mark.asyncio
    async def test_handle_no_args(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test handling command with no arguments."""
        response = await engine_handler.handle([], mock_twitch_context)
        
        assert "Usage:" in response
        assert "room" in response.lower()
        mock_midi_client.set_effect.assert_not_called()
    
    @pytest.mark.asyncio
    async def test_handle_midi_error(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test handling MIDI service error."""
        mock_midi_client.set_effect = AsyncMock(
            side_effect=Exception("MIDI service unavailable")
        )
        
        response = await engine_handler.handle(["room"], mock_twitch_context)
        
        assert "Failed to set engine" in response
        assert "❌" in response
    
    @pytest.mark.asyncio
    async def test_handle_preserves_proper_casing_in_api_call(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test that API call uses proper casing from config, not user input."""
        # User inputs lowercase
        await engine_handler.handle(["echoVerb"], mock_twitch_context)
        
        # But API should receive proper casing from config
        call_args = mock_midi_client.set_effect.call_args
        assert call_args.kwargs['selection'] == "EchoVerb"
    
    @pytest.mark.asyncio
    async def test_handle_case_insensitive_edome(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test special case: EDome with mixed capitalization."""
        test_cases = ["edome", "EDOME", "EDome", "eDome"]
        
        for test_input in test_cases:
            mock_midi_client.set_effect.reset_mock()
            response = await engine_handler.handle([test_input], mock_twitch_context)
            
            assert "Engine set to 'edome' mode!" in response
            mock_midi_client.set_effect.assert_called_once_with(
                device_name="VentrisDualReverb",
                device_effect_name="ReverbEngineA",
                device_effect_setting_name="ReverbEngine",
                selection="EDome"
            )
    
    @pytest.mark.asyncio
    async def test_handle_empty_string_arg(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test handling empty string argument."""
        response = await engine_handler.handle([""], mock_twitch_context)
        
        assert "Invalid engine type" in response
        mock_midi_client.set_effect.assert_not_called()
    
    @pytest.mark.asyncio
    async def test_handle_multiple_args_uses_first(self, engine_handler, mock_twitch_context, mock_midi_client):
        """Test that only the first argument is used."""
        response = await engine_handler.handle(["room", "hall", "plate"], mock_twitch_context)
        
        assert "Engine set to 'room' mode!" in response
        mock_midi_client.set_effect.assert_called_once_with(
            device_name="VentrisDualReverb",
            device_effect_name="ReverbEngineA",
            device_effect_setting_name="ReverbEngine",
            selection="Room"
        )


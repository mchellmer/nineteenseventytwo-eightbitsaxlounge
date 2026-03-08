"""Tests for StreamingBot interface."""

import pytest
import importlib.util
import importlib.machinery
import sys
from pathlib import Path

# Load StreamingBot directly from the source file to avoid importing the
# `bots` package which may perform eager imports and cause circular imports
streaming_path = Path(__file__).resolve().parents[2] / 'src' / 'bots' / 'streaming_bot.py'
spec = importlib.util.spec_from_file_location('streaming_bot_module', str(streaming_path))
streaming_mod = importlib.util.module_from_spec(spec)
sys.modules['streaming_bot_module'] = streaming_mod
spec.loader.exec_module(streaming_mod)
StreamingBot = streaming_mod.StreamingBot


class TestStreamingBot:
    """Test cases for StreamingBot interface contract."""
    
    class MockBot(StreamingBot):
        """Concrete test implementation of StreamingBot."""
        
        def __init__(self):
            self._connected = False
            self._name = "TestBot"
            self._channel = "test_channel"
            self._service = "TestService"
        
        async def start(self):
            """Test implementation of start."""
            self._connected = True
        
        async def shutdown(self):
            """Test implementation of shutdown."""
            self._connected = False
        
        async def send_message(self, channel: str, message: str):
            """Test implementation of send_message."""
            pass
        
        @property
        def is_connected(self) -> bool:
            """Test implementation of is_connected."""
            return self._connected
        
        @property
        def bot_name(self) -> str:
            """Test implementation of bot_name."""
            return self._name
        
        @property
        def primary_channel(self) -> str:
            """Test implementation of primary_channel."""
            return self._channel
        
        @property
        def service_name(self) -> str:
            """Test implementation of service_name."""
            return self._service
    
    @pytest.fixture
    def mock_bot(self):
        """Create a mock bot instance."""
        return self.MockBot()
    
    @pytest.mark.asyncio
    async def test_start_method_exists(self, mock_bot):
        """Test that start method exists and can be called."""
        await mock_bot.start()
        assert mock_bot.is_connected is True
    
    @pytest.mark.asyncio
    async def test_shutdown_method_exists(self, mock_bot):
        """Test that shutdown method exists and can be called."""
        mock_bot._connected = True
        await mock_bot.shutdown()
        assert mock_bot.is_connected is False
    
    @pytest.mark.asyncio
    async def test_send_message_method_exists(self, mock_bot):
        """Test that send_message method exists and can be called."""
        # Should not raise an exception
        await mock_bot.send_message("test_channel", "test message")
    
    def test_is_connected_property_exists(self, mock_bot):
        """Test that is_connected property exists and returns a boolean."""
        result = mock_bot.is_connected
        assert isinstance(result, bool)
    
    def test_bot_name_property_exists(self, mock_bot):
        """Test that bot_name property exists and returns a string."""
        name = mock_bot.bot_name
        assert isinstance(name, str)
        assert len(name) > 0
    
    def test_primary_channel_property_exists(self, mock_bot):
        """Test that primary_channel property exists and returns a string."""
        channel = mock_bot.primary_channel
        assert isinstance(channel, str)
        assert len(channel) > 0
    
    def test_service_name_property_exists(self, mock_bot):
        """Test that service_name property exists and returns a string."""
        service = mock_bot.service_name
        assert isinstance(service, str)
        assert len(service) > 0
    
    def test_streaming_bot_is_abstract(self):
        """Test that StreamingBot cannot be instantiated directly."""
        with pytest.raises(TypeError):
            StreamingBot()

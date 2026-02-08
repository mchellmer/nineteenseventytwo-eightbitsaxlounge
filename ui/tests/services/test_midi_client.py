"""Tests for MIDI client service."""

import pytest
from unittest.mock import Mock, AsyncMock, patch, MagicMock
from services.midi_client import MidiClient


def create_mock_response(status=200, json_data=None, text_data=None):
    """Helper to create a mock aiohttp response."""
    response = Mock()
    response.status = status
    response.json = AsyncMock(return_value=json_data or {})
    response.text = AsyncMock(return_value=text_data or "")
    response.raise_for_status = Mock()
    if status >= 400:
        from aiohttp import ClientResponseError
        response.raise_for_status.side_effect = ClientResponseError(
            request_info=Mock(),
            history=(),
            status=status,
            message=text_data or "Error"
        )
    response.__aenter__ = AsyncMock(return_value=response)
    response.__aexit__ = AsyncMock(return_value=None)
    return response


def create_mock_session_with_response(response):
    """Helper to create a mock aiohttp session with a response."""
    mock_session = AsyncMock()
    # Make get/post return the response which is itself an async context manager
    mock_session.get = MagicMock(return_value=response)
    mock_session.post = MagicMock(return_value=response)
    # Make the session itself an async context manager
    mock_session.__aenter__ = AsyncMock(return_value=mock_session)
    mock_session.__aexit__ = AsyncMock(return_value=None)
    return mock_session


class TestMidiClient:
    """Test cases for MidiClient."""
    
    @pytest.fixture
    def midi_client(self):
        """Create a MIDI client instance for testing."""
        return MidiClient(
            base_url="http://test-service:8080",
            client_id="test_client",
            client_secret="test_secret",
            timeout=5
        )
    
    @pytest.mark.asyncio
    async def test_authenticate_success(self, midi_client):
        """Test successful authentication."""
        mock_response = create_mock_response(
            status=200,
            json_data={"access_token": "test_token", "expires_in": 3600}
        )
        mock_session = create_mock_session_with_response(mock_response)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            token = await midi_client.authenticate("client_id", "client_secret")
            
            assert token == "test_token"
            assert midi_client._token == "test_token"
    
    @pytest.mark.asyncio
    async def test_authenticate_failure(self, midi_client):
        """Test authentication failure."""
        mock_response = create_mock_response(status=401, text_data="Unauthorized")
        mock_session = create_mock_session_with_response(mock_response)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            with pytest.raises(Exception):
                await midi_client.authenticate("bad_client", "bad_secret")
    
    @pytest.mark.asyncio
    async def test_get_endpoint(self, midi_client):
        """Test GET request to any endpoint."""
        mock_response = create_mock_response(
            status=200,
            json_data={"status": "ok"}
        )
        mock_session = create_mock_session_with_response(mock_response)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            result = await midi_client.get('api/token')
            
            assert result == {"status": "ok"}
    
    @pytest.mark.asyncio
    async def test_get_data_endpoint(self, midi_client):
        """Test GET request to data endpoint."""
        mock_response = create_mock_response(
            status=200,
            json_data={"data": "test"}
        )
        mock_session = create_mock_session_with_response(mock_response)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            result = await midi_client.get('api/data/something')
            
            assert result == {"data": "test"}
    
    @pytest.mark.asyncio
    async def test_post_with_authentication(self, midi_client):
        """Test POST request with authentication."""
        # First POST will get 401, triggering auth
        unauthorized_response = create_mock_response(status=401)
        
        # Auth POST will succeed
        auth_response = create_mock_response(
            status=200,
            json_data={"access_token": "new_token", "expires_in": 3600}
        )
        
        # Retry POST will succeed
        post_response = create_mock_response(
            status=200,
            json_data={"success": True}
        )
        
        # Create three mock sessions for the three POST calls
        unauth_session = AsyncMock()
        unauth_session.post = MagicMock(return_value=unauthorized_response)
        unauth_session.__aenter__ = AsyncMock(return_value=unauth_session)
        unauth_session.__aexit__ = AsyncMock(return_value=None)
        
        auth_session = AsyncMock()
        auth_session.post = MagicMock(return_value=auth_response)
        auth_session.__aenter__ = AsyncMock(return_value=auth_session)
        auth_session.__aexit__ = AsyncMock(return_value=None)
        
        post_session = AsyncMock()
        post_session.post = MagicMock(return_value=post_response)
        post_session.__aenter__ = AsyncMock(return_value=post_session)
        post_session.__aexit__ = AsyncMock(return_value=None)
        
        with patch('aiohttp.ClientSession', side_effect=[unauth_session, auth_session, post_session]):
            result = await midi_client.post(
                'api/Midi/SendControlChangeMessage',
                {"address": 1, "value": 8},
                authenticated=True
            )
            
            assert result == {"success": True}
    
    @pytest.mark.asyncio
    async def test_send_control_change_message(self, midi_client):
        """Test sending MIDI control change message."""
        auth_response = create_mock_response(
            status=200,
            json_data={"access_token": "token", "expires_in": 3600}
        )
        midi_response = create_mock_response(
            status=200,
            json_data={"success": True, "message": "Sent"}
        )
        
        mock_session = AsyncMock()
        mock_session.post = MagicMock(side_effect=[auth_response, midi_response])
        mock_session.__aenter__ = AsyncMock(return_value=mock_session)
        mock_session.__aexit__ = AsyncMock(return_value=None)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            result = await midi_client.send_control_change_message(
                device_midi_connect_name="Test Device",
                address=1,
                value=8
            )
            
            assert result["success"] is True
    
    @pytest.mark.asyncio
    async def test_set_effect_with_selection(self, midi_client):
        """Test set_effect with selection parameter."""
        auth_response = create_mock_response(
            status=200,
            json_data={"access_token": "token", "expires_in": 3600}
        )
        midi_response = create_mock_response(
            status=200,
            json_data={"success": True}
        )
        
        mock_session = AsyncMock()
        mock_session.post = MagicMock(side_effect=[auth_response, midi_response])
        mock_session.__aenter__ = AsyncMock(return_value=mock_session)
        mock_session.__aexit__ = AsyncMock(return_value=None)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            result = await midi_client.set_effect(
                device_name="VentrisDualReverb",
                device_effect_name="ReverbEngineA",
                device_effect_setting_name="ReverbEngine",
                selection="Room"
            )
            
            assert result["success"] is True
    
    @pytest.mark.asyncio
    async def test_set_effect_with_value(self, midi_client):
        """Test set_effect with value parameter."""
        auth_response = create_mock_response(
            status=200,
            json_data={"access_token": "token", "expires_in": 3600}
        )
        midi_response = create_mock_response(
            status=200,
            json_data={"success": True}
        )
        
        mock_session = AsyncMock()
        mock_session.post = MagicMock(side_effect=[auth_response, midi_response])
        mock_session.__aenter__ = AsyncMock(return_value=mock_session)
        mock_session.__aexit__ = AsyncMock(return_value=None)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            result = await midi_client.set_effect(
                device_name="VentrisDualReverb",
                device_effect_name="ReverbEngineA",
                device_effect_setting_name="ReverbEngine",
                value=50
            )
            
            assert result["success"] is True
    
    @pytest.mark.asyncio
    async def test_set_effect_with_both_selection_and_value(self, midi_client):
        """Test set_effect with both selection and value parameters."""
        auth_response = create_mock_response(
            status=200,
            json_data={"access_token": "token", "expires_in": 3600}
        )
        midi_response = create_mock_response(
            status=200,
            json_data={"success": True}
        )
        
        mock_session = AsyncMock()
        mock_session.post = MagicMock(side_effect=[auth_response, midi_response])
        mock_session.__aenter__ = AsyncMock(return_value=mock_session)
        mock_session.__aexit__ = AsyncMock(return_value=None)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            result = await midi_client.set_effect(
                device_name="VentrisDualReverb",
                device_effect_name="ReverbEngineA",
                device_effect_setting_name="ReverbEngine",
                selection="Room",
                value=75
            )
            
            assert result["success"] is True
    
    @pytest.mark.asyncio
    async def test_set_effect_validation_error_no_params(self, midi_client):
        """Test set_effect raises ValueError when neither selection nor value is provided."""
        with pytest.raises(ValueError, match="Either 'selection' or 'value' must be provided"):
            await midi_client.set_effect(
                device_name="VentrisDualReverb",
                device_effect_name="ReverbEngineA",
                device_effect_setting_name="ReverbEngine"
            )

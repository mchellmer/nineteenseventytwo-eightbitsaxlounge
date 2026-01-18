"""Tests for Twitch client service."""

import pytest
from unittest.mock import Mock, AsyncMock, patch, MagicMock
from datetime import datetime, timedelta
from services.twitch_client import TwitchClient


def create_mock_response(status=200, json_data=None):
    """Helper to create a mock aiohttp response."""
    response = Mock()
    response.status = status
    response.json = AsyncMock(return_value=json_data or {})
    response.raise_for_status = Mock()
    if status >= 400:
        from aiohttp import ClientResponseError
        response.raise_for_status.side_effect = ClientResponseError(
            request_info=Mock(),
            history=(),
            status=status
        )
    response.__aenter__ = AsyncMock(return_value=response)
    response.__aexit__ = AsyncMock(return_value=None)
    return response


def create_mock_session_with_response(response):
    """Helper to create a mock aiohttp session with a response."""
    mock_session = AsyncMock()
    mock_session.get = MagicMock(return_value=response)
    mock_session.__aenter__ = AsyncMock(return_value=mock_session)
    mock_session.__aexit__ = AsyncMock(return_value=None)
    return mock_session


class TestTwitchClient:
    """Test cases for TwitchClient."""
    
    @pytest.fixture
    def twitch_client(self):
        """Create a Twitch client instance for testing."""
        return TwitchClient(client_id="test_client_id")
    
    @pytest.mark.asyncio
    async def test_validate_token_success(self, twitch_client):
        """Test successful token validation."""
        mock_response = create_mock_response(
            status=200,
            json_data={
                "client_id": "test_client_id",
                "login": "testbot",
                "scopes": ["chat:read", "chat:edit"],
                "expires_in": 5036160
            }
        )
        mock_session = create_mock_session_with_response(mock_response)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            result = await twitch_client.validate_token("test_token")
            
            assert result["login"] == "testbot"
            assert result["expires_in"] == 5036160
    
    @pytest.mark.asyncio
    async def test_validate_token_expired(self, twitch_client):
        """Test validation of expired token."""
        mock_response = create_mock_response(status=401)
        mock_session = create_mock_session_with_response(mock_response)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            with pytest.raises(Exception):
                await twitch_client.validate_token("expired_token")
    
    @pytest.mark.asyncio
    async def test_validate_and_warn_expiring_soon(self, twitch_client, caplog):
        """Test warning when token expires soon."""
        mock_response = create_mock_response(
            status=200,
            json_data={
                "login": "testbot",
                "scopes": ["chat:read", "chat:edit"],
                "expires_in": 86400 * 5  # 5 days
            }
        )
        mock_session = create_mock_session_with_response(mock_response)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            result = await twitch_client.validate_and_warn("test_token")
            
            assert result["expires_in"] == 86400 * 5
            assert "URGENT" in caplog.text
    
    @pytest.mark.asyncio
    async def test_validate_and_warn_14_days(self, twitch_client, caplog):
        """Test warning at 14 days."""
        mock_response = create_mock_response(
            status=200,
            json_data={
                "login": "testbot",
                "scopes": ["chat:read", "chat:edit"],
                "expires_in": 86400 * 14  # 14 days
            }
        )
        mock_session = create_mock_session_with_response(mock_response)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            result = await twitch_client.validate_and_warn("test_token")
            
            assert "WARNING" in caplog.text
    
    @pytest.mark.asyncio
    async def test_validate_and_warn_long_expiry(self, twitch_client, caplog):
        """Test no warning when token has long expiry."""
        import logging
        caplog.set_level(logging.INFO)
        
        mock_response = create_mock_response(
            status=200,
            json_data={
                "login": "testbot",
                "scopes": ["chat:read", "chat:edit"],
                "expires_in": 86400 * 60  # 60 days
            }
        )
        mock_session = create_mock_session_with_response(mock_response)
        
        with patch('aiohttp.ClientSession', return_value=mock_session):
            result = await twitch_client.validate_and_warn("test_token")
            
            assert result["expires_in"] == 86400 * 60
            # Should have info log but not warning
            assert "Token status" in caplog.text

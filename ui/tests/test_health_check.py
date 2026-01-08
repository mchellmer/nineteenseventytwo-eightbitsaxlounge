import pytest
from unittest.mock import patch, AsyncMock, Mock
from aiohttp import web
from src.health_check import health_check, startup_health_server


class TestHealthCheck:
    """Test cases for health check functionality."""
    
    @pytest.mark.asyncio
    async def test_health_check_basic(self, mock_settings):
        """Test basic health check endpoint."""
        request = Mock()
        
        with patch('aiohttp.ClientSession') as mock_session:
            # Mock successful MIDI service response
            mock_response = AsyncMock()
            mock_response.status = 200
            mock_session.return_value.__aenter__.return_value.get.return_value.__aenter__.return_value = mock_response
            
            response = await health_check(request)
            
            assert response.status == 200
            body = response.body
            # Response should be JSON with status
            import json
            data = json.loads(body.decode())
            assert data["status"] == "healthy"
            assert data["service"] == "twitch-bot"
            assert "version" in data
    
    @pytest.mark.asyncio
    async def test_health_check_midi_unhealthy(self, mock_settings):
        """Test health check when MIDI service returns unhealthy."""
        request = Mock()
        
        with patch('aiohttp.ClientSession') as mock_session:
            # Mock unhealthy MIDI service response
            mock_response = AsyncMock()
            mock_response.status = 503
            mock_session.return_value.__aenter__.return_value.get.return_value.__aenter__.return_value = mock_response
            
            response = await health_check(request)
            
            assert response.status == 200  # Health check itself should still return 200
            import json
            data = json.loads(response.body.decode())
            assert data["status"] == "healthy"
            assert data["midi_service"] == "unhealthy"
    
    @pytest.mark.asyncio
    async def test_health_check_midi_unreachable(self, mock_settings):
        """Test health check when MIDI service is unreachable."""
        request = Mock()
        
        with patch('aiohttp.ClientSession') as mock_session:
            # Mock connection error to MIDI service
            mock_session.return_value.__aenter__.return_value.get.side_effect = Exception("Connection failed")
            
            response = await health_check(request)
            
            assert response.status == 200
            import json
            data = json.loads(response.body.decode())
            assert data["status"] == "healthy"
            assert data["midi_service"] == "unreachable"
    
    @pytest.mark.asyncio
    async def test_health_check_exception(self, mock_settings):
        """Test health check when an unexpected exception occurs."""
        request = Mock()
        
        with patch('src.health_check.settings', side_effect=Exception("Config error")):
            response = await health_check(request)
            
            assert response.status == 503
            import json
            data = json.loads(response.body.decode())
            assert data["status"] == "unhealthy"
            assert "error" in data
    
    @pytest.mark.asyncio
    async def test_startup_health_server(self, mock_settings):
        """Test health server startup."""
        with patch('aiohttp.web.AppRunner') as mock_runner, \
             patch('aiohttp.web.TCPSite') as mock_site:
            
            mock_runner_instance = AsyncMock()
            mock_runner.return_value = mock_runner_instance
            
            mock_site_instance = AsyncMock()
            mock_site.return_value = mock_site_instance
            
            runner = await startup_health_server()
            
            assert runner == mock_runner_instance
            mock_runner_instance.setup.assert_called_once()
            mock_site_instance.start.assert_called_once()
            
            # Verify the site is created with correct parameters
            mock_site.assert_called_once_with(mock_runner_instance, '0.0.0.0', mock_settings.health_check_port)


class TestHealthCheckIntegration:
    """Integration tests for health check endpoints."""
    
    @pytest.mark.asyncio
    async def test_health_endpoints_routing(self):
        """Test that all health endpoints are properly routed."""
        from src.health_check import startup_health_server
        
        with patch('aiohttp.web.AppRunner') as mock_runner, \
             patch('aiohttp.web.TCPSite'):
            
            mock_app = Mock()
            mock_router = Mock()
            mock_app.router = mock_router
            
            with patch('aiohttp.web.Application', return_value=mock_app):
                await startup_health_server()
                
                # Verify all expected routes are added
                expected_routes = ['/health', '/healthz', '/']
                actual_calls = [call[0][0] for call in mock_router.add_get.call_args_list]
                
                for route in expected_routes:
                    assert route in actual_calls
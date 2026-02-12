"""HTTP client for communicating with the MIDI API."""

import aiohttp
import logging
from typing import Optional, Dict, Any
from config.logging_config import get_correlation_id

logger = logging.getLogger(__name__)


class MidiClient:
    """
    Asynchronous HTTP client for the MIDI API service.
    
    Provides methods for making GET and POST requests to the MIDI service.
    """
    
    def __init__(
        self, 
        base_url: str,
        client_id: Optional[str] = None,
        client_secret: Optional[str] = None,
        timeout: int = 5
    ):
        """
        Initialize the MIDI client.
        
        Args:
            base_url: Base URL of the MIDI service
            client_id: Client ID for authentication (optional)
            client_secret: Client secret for authentication (optional)
            timeout: Request timeout in seconds (default: 5)
        """
        self.base_url = base_url.rstrip('/')
        self.timeout = aiohttp.ClientTimeout(total=timeout)
        self._token: Optional[str] = None
        self._client_id = client_id
        self._client_secret = client_secret
        self._authenticating = False
    
    async def get(self, endpoint: str, authenticated: bool = False, _retry: bool = True) -> Dict[str, Any]:
        """
        Make a GET request to the MIDI API.
        
        Args:
            endpoint: API endpoint path
            authenticated: Whether to include authentication header
            _retry: Internal flag to control retry on auth failure
            
        Returns:
            JSON response as a dictionary
            
        Raises:
            Exception: If the request fails
        """
        url = f"{self.base_url}/{endpoint.lstrip('/')}"
        
        headers = {}
        if authenticated and self._token:
            headers['Authorization'] = f'Bearer {self._token}'
        
        try:
            async with aiohttp.ClientSession(timeout=self.timeout) as session:
                async with session.get(url, headers=headers) as response:
                    # Handle authentication errors with retry
                    if response.status in (401, 403) and authenticated and _retry:
                        logger.warning(f"Authentication failed (HTTP {response.status}), refreshing token...")
                        await self._ensure_authenticated()
                        return await self.get(endpoint, authenticated=True, _retry=False)
                    
                    response.raise_for_status()
                    return await response.json()
                    
        except aiohttp.ClientError as e:
            logger.error(f"Error making GET request to {url}: {e}")
            raise Exception(f"Failed to communicate with MIDI service: {str(e)}")
        except Exception as e:
            logger.error(f"Unexpected error in GET request to {url}: {e}")
            raise
    
    async def post(self, endpoint: str, data: Dict[str, Any], authenticated: bool = False, _retry: bool = True) -> Dict[str, Any]:
        """
        Make a POST request to the MIDI API.
        
        Args:
            endpoint: API endpoint path
            data: JSON data to send in the request body
            authenticated: Whether to include authentication header
            _retry: Internal flag to control retry on auth failure
            
        Returns:
            JSON response as a dictionary
            
        Raises:
            Exception: If the request fails
        """
        url = f"{self.base_url}/{endpoint.lstrip('/')}"
        
        headers = {
            'X-Correlation-ID': get_correlation_id()
        }
        if authenticated and self._token:
            headers['Authorization'] = f'Bearer {self._token}'
        
        try:
            async with aiohttp.ClientSession(timeout=self.timeout) as session:
                async with session.post(url, json=data, headers=headers) as response:
                    # Handle authentication errors with retry
                    if response.status in (401, 403) and authenticated and _retry:
                        logger.warning(f"Authentication failed (HTTP {response.status}), refreshing token...")
                        await self._ensure_authenticated()
                        return await self.post(endpoint, data, authenticated=True, _retry=False)
                    
                    response.raise_for_status()
                    return await response.json()
                    
        except aiohttp.ClientError as e:
            logger.error(f"Error making POST request to {url}: {e}")
            raise Exception(f"Failed to communicate with MIDI service: {str(e)}")
        except Exception as e:
            logger.error(f"Unexpected error in POST request to {url}: {e}")
            raise
    
    async def authenticate(self, client_id: str, client_secret: str) -> str:
        """
        Authenticate with the MIDI API and store the token.
        
        Args:
            client_id: Client identifier
            client_secret: Client secret for authentication
            
        Returns:
            JWT access token
            
        Raises:
            Exception: If authentication fails
        """
        try:
            response = await self.post(
                'api/token',
                {
                    'clientId': client_id,
                    'clientSecret': client_secret
                },
                authenticated=False,
                _retry=False
            )
            
            token = response.get('token') or response.get('access_token')
            if not token:
                raise Exception("No token found in authentication response")
            
            self._token = token
            logger.info(f"Successfully authenticated as {client_id}")
            return token
            
        except Exception as e:
            logger.error(f"Authentication failed: {e}")
            raise
    
    async def send_control_change_message(
        self,
        device_midi_connect_name: str,
        address: int,
        value: int
    ) -> Dict[str, Any]:
        """
        Send a MIDI control change message to a device.
        Automatically authenticates if not already authenticated.
        
        Args:
            device_midi_connect_name: Name of the MIDI device
            address: MIDI control change address
            value: MIDI control change value
            
        Returns:
            Response from the MIDI service
            
        Raises:
            Exception: If the request fails
        """
        if not self._token:
            await self._ensure_authenticated()
        
        return await self.post(
            'api/Midi/SendControlChangeMessage',
            {
                'deviceMidiConnectName': device_midi_connect_name,
                'address': address,
                'value': value
            },
            authenticated=True
        )
    
    async def set_effect(
        self,
        device_name: str,
        device_effect_name: str,
        device_effect_setting_name: str,
        selection: Optional[str] = None,
        value: Optional[int] = None
    ) -> Dict[str, Any]:
        """
        Set an effect on a MIDI device.
        Automatically authenticates if not already authenticated.
        
        Args:
            device_name: Name of the MIDI device
            device_effect_name: Name of the device effect
            device_effect_setting_name: Name of the device effect setting
            selection: Optional selection value (e.g., engine name)
            value: Optional numeric value
            
        Returns:
            Response from the MIDI service
            
        Raises:
            ValueError: If neither selection nor value is provided
            Exception: If the request fails
        """
        # Validate that at least one of selection or value is provided
        if selection is None and value is None:
            raise ValueError("Either 'selection' or 'value' must be provided for set_effect")
        
        if not self._token:
            await self._ensure_authenticated()
        
        payload = {
            'deviceName': device_name,
            'deviceEffectName': device_effect_name,
            'deviceEffectSettingName': device_effect_setting_name
        }
        
        if selection is not None:
            payload['selection'] = selection
        
        if value is not None:
            payload['value'] = value
        
        return await self.post(
            'api/Midi/SetEffect',
            payload,
            authenticated=True
        )
    
    async def _ensure_authenticated(self) -> None:
        """
        Ensure the client is authenticated, refreshing token if needed.
        Uses stored credentials if available.
        
        Raises:
            Exception: If authentication fails or credentials not available
        """
        if self._authenticating:
            import asyncio
            while self._authenticating:
                await asyncio.sleep(0.1)
            return
        
        if not self._client_id or not self._client_secret:
            raise Exception("Cannot authenticate: client credentials not configured")
        
        self._authenticating = True
        try:
            await self.authenticate(self._client_id, self._client_secret)
        finally:
            self._authenticating = False



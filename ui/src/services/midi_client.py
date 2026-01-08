"""HTTP client for communicating with the MIDI API."""

import aiohttp
import logging
from typing import Optional, Dict, Any

logger = logging.getLogger(__name__)


class MidiClient:
    """
    Asynchronous HTTP client for the MIDI API service.
    
    Provides methods for making GET and POST requests to the MIDI service
    with proper error handling and logging.
    """
    
    def __init__(self, base_url: str, timeout: int = 5):
        """
        Initialize the MIDI client.
        
        Args:
            base_url: Base URL of the MIDI API service
            timeout: Request timeout in seconds (default: 5)
        """
        self.base_url = base_url.rstrip('/')
        self.timeout = aiohttp.ClientTimeout(total=timeout)
    
    async def get(self, endpoint: str) -> Dict[str, Any]:
        """
        Make a GET request to the MIDI API.
        
        Args:
            endpoint: API endpoint path
            
        Returns:
            JSON response as a dictionary
            
        Raises:
            Exception: If the request fails
        """
        url = f"{self.base_url}/{endpoint.lstrip('/')}"
        
        try:
            async with aiohttp.ClientSession(timeout=self.timeout) as session:
                async with session.get(url) as response:
                    response.raise_for_status()
                    return await response.json()
                    
        except aiohttp.ClientError as e:
            logger.error(f"Error making GET request to {url}: {e}")
            raise Exception(f"Failed to communicate with MIDI service: {str(e)}")
        except Exception as e:
            logger.error(f"Unexpected error in GET request to {url}: {e}")
            raise
    
    async def post(self, endpoint: str, data: Dict[str, Any]) -> Dict[str, Any]:
        """
        Make a POST request to the MIDI API.
        
        Args:
            endpoint: API endpoint path
            data: JSON data to send in the request body
            
        Returns:
            JSON response as a dictionary
            
        Raises:
            Exception: If the request fails
        """
        url = f"{self.base_url}/{endpoint.lstrip('/')}"
        
        try:
            async with aiohttp.ClientSession(timeout=self.timeout) as session:
                async with session.post(url, json=data) as response:
                    response.raise_for_status()
                    return await response.json()
                    
        except aiohttp.ClientError as e:
            logger.error(f"Error making POST request to {url}: {e}")
            raise Exception(f"Failed to communicate with MIDI service: {str(e)}")
        except Exception as e:
            logger.error(f"Unexpected error in POST request to {url}: {e}")
            raise

"""
Twitch token validation client.
Validates tokens and warns about expiry.
"""

import aiohttp
import logging
from datetime import datetime, timedelta

logger = logging.getLogger(__name__)


class TwitchClient:
    """
    Simple Twitch token validator.
    
    Validates access tokens and provides warnings when they're approaching expiry.
    """
    
    OAUTH_BASE_URL = "https://id.twitch.tv/oauth2"
    
    def __init__(self, client_id: str):
        """
        Initialize the Twitch validator.
        
        Args:
            client_id: Your application's Client ID from dev.twitch.tv
        """
        self.client_id = client_id
    
    async def validate_token(self, access_token: str) -> dict:
        """
        Validate an access token and get information about it.
        
        Args:
            access_token: Token to validate (without 'oauth:' prefix)
            
        Returns:
            Validation response with client_id, login, scopes, expires_in
            
        Raises:
            Exception: If validation fails (token is invalid/expired)
        """
        url = f"{self.OAUTH_BASE_URL}/validate"
        headers = {
            'Authorization': f'OAuth {access_token}'
        }
        
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(url, headers=headers) as response:
                    if response.status == 401:
                        raise Exception("Token is invalid or expired")
                    
                    response.raise_for_status()
                    result = await response.json()
                    
                    logger.info(f"Token validated for user: {result.get('login')}")
                    return result
                    
        except aiohttp.ClientError as e:
            logger.error(f"Error validating token: {e}")
            raise Exception(f"Failed to validate token: {str(e)}")
    
    async def validate_and_warn(self, access_token: str) -> dict:
        """
        Validate token and log warnings if it's expiring soon.
        
        Args:
            access_token: Token to validate (without 'oauth:' prefix)
            
        Returns:
            Validation response
        """
        result = await self.validate_token(access_token)
        
        expires_in_seconds = result.get('expires_in', 0)
        expires_in_days = expires_in_seconds / 86400
        
        # Log detailed info
        logger.info(f"Token status for user '{result.get('login')}':")
        logger.info(f"  Scopes: {', '.join(result.get('scopes', []))}")
        logger.info(f"  Expires in: {expires_in_days:.1f} days ({expires_in_seconds} seconds)")
        
        # Warn if expiring soon
        if expires_in_days <= 7:
            logger.error(
                f"⚠️  URGENT: Twitch token expires in {expires_in_days:.1f} days! "
                f"Update TWITCH_TOKEN in GitHub secrets immediately."
            )
        elif expires_in_days <= 14:
            logger.warning(
                f"⚠️  WARNING: Twitch token expires in {expires_in_days:.1f} days. "
                f"Consider updating TWITCH_TOKEN in GitHub secrets soon."
            )
        elif expires_in_days <= 30:
            logger.info(
                f"ℹ️  Token expires in {expires_in_days:.1f} days. "
                f"You'll need to update it before {datetime.now() + timedelta(seconds=expires_in_seconds):%Y-%m-%d}"
            )
        
        return result

"""
Command handlers for the Twitch chatbot.
Each handler implements the logic for specific commands.
"""

import aiohttp
import logging
from typing import List, Any
from .config import settings

logger = logging.getLogger(__name__)

class BaseHandler:
    """Base class for command handlers."""
    
    async def make_midi_request(self, endpoint: str, data: dict = None) -> dict:
        """Make a request to the MIDI API."""
        url = f"{settings.midi_api_url.rstrip('/')}/{endpoint.lstrip('/')}"
        
        try:
            async with aiohttp.ClientSession() as session:
                if data:
                    async with session.post(url, json=data, timeout=settings.midi_api_timeout) as response:
                        response.raise_for_status()
                        return await response.json()
                else:
                    async with session.get(url, timeout=settings.midi_api_timeout) as response:
                        response.raise_for_status()
                        return await response.json()
        except aiohttp.ClientError as e:
            logger.error(f"Error making request to MIDI API: {e}")
            raise Exception(f"Failed to communicate with MIDI service: {str(e)}")
        except Exception as e:
            logger.error(f"Unexpected error in MIDI request: {e}")
            raise

class EngineHandler(BaseHandler):
    """Handler for engine-related commands."""
    
    VALID_ENGINES = ["room", "jazz", "ambient", "rock", "electronic"]
    
    async def handle_engine_command(self, args: List[str], context: Any) -> str:
        """Handle !engine commands."""
        if not args:
            return f"Usage: !engine <type>. Available engines: {', '.join(self.VALID_ENGINES)}"
        
        engine_type = args[0].lower()
        
        if engine_type not in self.VALID_ENGINES:
            return f"Invalid engine type: {engine_type}. Available engines: {', '.join(self.VALID_ENGINES)}"
        
        try:
            # Make request to MIDI service to change engine
            response = await self.make_midi_request("api/engine", {
                "engine_type": engine_type,
                "requester": context.author.name if hasattr(context, 'author') else "chatbot"
            })
            
            logger.info(f"Engine changed to {engine_type} by {context.author.name if hasattr(context, 'author') else 'chatbot'}")
            return f"🎵 Engine set to '{engine_type}' mode! 🎵"
            
        except Exception as e:
            logger.error(f"Failed to set engine to {engine_type}: {e}")
            return f"❌ Failed to set engine to '{engine_type}'. Please try again later."

class HelpHandler(BaseHandler):
    """Handler for help commands."""
    
    async def handle_help_command(self, args: List[str], context: Any) -> str:
        """Handle !help commands."""
        help_text = [
            "🎵 EightBitSaxLounge Bot Commands 🎵",
            "!engine <type> - Change MIDI engine (room, jazz, ambient, rock, electronic)",
            "!status - Show bot and system status", 
            "!help - Show this help message",
            "For more info, visit: https://github.com/yourusername/nineteenseventytwo-eightbitsaxlounge"
        ]
        return " | ".join(help_text)

class StatusHandler(BaseHandler):
    """Handler for status commands."""
    
    async def handle_status_command(self, args: List[str], context: Any) -> str:
        """Handle !status commands."""
        try:
            # Check MIDI service health
            midi_status = await self.make_midi_request("health")
            midi_healthy = midi_status.get("status") == "healthy"
            
            status_parts = [
                f"🤖 Bot: Online",
                f"🎵 MIDI Service: {'✅ Healthy' if midi_healthy else '❌ Unhealthy'}",
                f"📺 Channel: #{settings.twitch_channel}"
            ]
            
            return " | ".join(status_parts)
            
        except Exception as e:
            logger.error(f"Failed to get status: {e}")
            return "🤖 Bot: Online | 🎵 MIDI Service: ❌ Unavailable"
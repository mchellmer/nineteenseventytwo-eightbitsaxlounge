"""
Health check endpoint for the Twitch bot.
Used by Kubernetes and Docker for health monitoring.
"""

import asyncio
import aiohttp
import sys
from aiohttp import web
from .config import settings

async def health_check(request):
    """Health check endpoint."""
    try:
        # Basic health check - bot is running
        health_status = {
            "status": "healthy",
            "service": "twitch-bot",
            "version": "1.0.0"  # Could read from version.txt
        }
        
        # Optional: Check MIDI service connectivity
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(f"{settings.midi_api_url}/health", timeout=5) as response:
                    if response.status == 200:
                        health_status["midi_service"] = "healthy"
                    else:
                        health_status["midi_service"] = "unhealthy"
        except:
            health_status["midi_service"] = "unreachable"
        
        return web.json_response(health_status)
    
    except Exception as e:
        return web.json_response(
            {"status": "unhealthy", "error": str(e)}, 
            status=503
        )

async def startup_health_server():
    """Start the health check server."""
    app = web.Application()
    app.router.add_get('/health', health_check)
    app.router.add_get('/healthz', health_check)  # K8s style
    app.router.add_get('/', health_check)  # Root for simple checks
    
    runner = web.AppRunner(app)
    await runner.setup()
    
    site = web.TCPSite(runner, '0.0.0.0', settings.health_check_port)
    await site.start()
    
    return runner

async def main():
    """Main function for standalone health check."""
    try:
        runner = await startup_health_server()
        print(f"Health check server running on port {settings.health_check_port}")
        
        # Keep running
        while True:
            await asyncio.sleep(3600)
    
    except KeyboardInterrupt:
        print("Shutting down health check server...")
    finally:
        if 'runner' in locals():
            await runner.cleanup()

if __name__ == '__main__':
    asyncio.run(main())
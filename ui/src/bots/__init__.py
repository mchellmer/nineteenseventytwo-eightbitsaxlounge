"""Bot implementations for various streaming platforms."""

from .twitch_bot import TwitchBot, EightBitSaxBot
from .factory import BotFactory

__all__ = ['TwitchBot', 'EightBitSaxBot', 'BotFactory']

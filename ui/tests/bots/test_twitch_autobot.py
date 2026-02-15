"""Tests for the Twitch `AutoBot`-based implementation.

These exercise lifecycle methods and database setup for the
`TwitchBot` implemented as an AutoBot (newer TwitchIO API).
"""

import os
import pytest
from unittest.mock import Mock, AsyncMock, patch

# Ensure minimal env vars for Settings() during test collection
os.environ.setdefault('TWITCH_BOT_ID', '1424580736')
os.environ.setdefault('TWITCH_OWNER_ID', '896950964')
os.environ.setdefault('TWITCH_CLIENT_SECRET', 'test_midi_secret')
os.environ.setdefault('TWITCH_CLIENT_ID', 'test_client_id')
os.environ.setdefault('TWITCH_ACCESS_TOKEN', 'oauth:test_token')
os.environ.setdefault('TWITCH_REFRESH_TOKEN', 'test_refresh_token')

from bots.twitch.twitch_autobot import TwitchAutoBot
from bots.twitch.eightbitsaxlounge_component import EightBitSaxLoungeComponent


@pytest.fixture
def mock_settings():
    s = Mock()
    s.twitch_access_token = "oauth:test_token"
    s.twitch_refresh_token = "test_refresh_token"
    s.twitch_client_id = "test_client_id"
    s.twitch_channel = "test_channel"
    s.twitch_prefix = "!"
    s.bot_name = "TestBot"
    s.twitch_client_secret = "secret"
    s.twitch_bot_id = "bot_id"
    s.twitch_owner_id = "owner"
    return s


@pytest.fixture
def autobot(mock_settings):
    with patch('bots.twitch.twitch_autobot.settings', mock_settings):
        token_db = Mock()
        bot = TwitchAutoBot(token_database=token_db, subs=[])

        # Minimal twitchio attributes
        bot.twitchio = Mock()
        bot.twitchio.start = AsyncMock()
        bot.twitchio.close = AsyncMock()
        bot.twitchio.get_channel = Mock()

        # token validator used by start()
        bot.token_validator = Mock()
        bot.token_validator.validate_and_warn = AsyncMock()

        return bot


def test_autobot_initialization(autobot):
    assert autobot is not None
    assert hasattr(autobot, 'twitchio')


@pytest.mark.asyncio
async def test_setup_hook_adds_component(autobot):
    autobot.add_component = AsyncMock()
    await autobot.setup_hook()
    autobot.add_component.assert_awaited()


@pytest.mark.asyncio
async def test_event_oauth_authorized_subscribes(autobot):
    # Ensure add_token and multi_subscribe are called for non-bot user
    payload = Mock()
    payload.access_token = 'at'
    payload.refresh_token = 'rt'
    payload.user_id = '999'

    autobot.add_token = AsyncMock()
    autobot.multi_subscribe = AsyncMock(return_value=Mock(errors=[]))

    await autobot.event_oauth_authorized(payload)

    autobot.add_token.assert_awaited()
    autobot.multi_subscribe.assert_awaited()


@pytest.mark.asyncio
async def test_component_engine_calls_registry(autobot):
    mock_registry = Mock()
    mock_registry.execute_command = AsyncMock(return_value='engine command executed')

    with patch('bots.twitch.eightbitsaxlounge_component.CommandRegistry', return_value=mock_registry):
        comp = EightBitSaxLoungeComponent(bot=Mock())
        ctx = Mock()
        ctx.send = AsyncMock()
        ctx.author = Mock()
        ctx.author.name = 'tester'

        # Exercise _execute_command directly
        await comp._execute_command('engine', ['room'], ctx)
        ctx.send.assert_awaited_with('engine command executed')

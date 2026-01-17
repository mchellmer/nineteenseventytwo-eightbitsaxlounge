# UI Tests

Comprehensive test suite for the Eight Bit Sax Lounge Twitch bot.

## Test Structure

Tests are organized by component type:

```
tests/
├── conftest.py                      # Shared fixtures and test configuration
├── handlers/                        # Command handler tests
│   ├── test_engine.py              # Engine command handler
│   ├── test_help.py                # Help command handler
│   └── test_status.py              # Status command handler
├── commands/                        # Command system tests
│   └── test_command_registry.py    # Command registration and execution
├── config/                          # Configuration tests
│   └── test_settings.py            # Settings and config loading
├── services/                        # Service integration tests
│   ├── test_midi_client.py         # MIDI service client
│   └── test_twitch_client.py       # Twitch OAuth validation
└── bots/                            # Bot implementation tests
    └── test_twitch_bot.py          # TwitchIO bot core
```

## Running Tests

Run all tests:
```bash
pytest
```

Run with coverage:
```bash
pytest --cov=src --cov-report=term-missing
```

Run specific test directory:
```bash
pytest handlers/
pytest services/
```

Run specific test file:
```bash
pytest handlers/test_engine.py
pytest services/test_midi_client.py
```

Run specific test class:
```bash
pytest handlers/test_engine.py::TestEngineHandler
```

Run specific test:
```bash
pytest handlers/test_engine.py::TestEngineHandler::test_handle_valid_engine
```

## Test Coverage

Current coverage includes:

### Handlers (tests/handlers/)
- **test_engine.py**: Engine type switching with MIDI control changes
- **test_help.py**: General and command-specific help
- **test_status.py**: MIDI service status checks

### Commands (tests/commands/)
- **test_command_registry.py**: Command registration, lookup, and execution

### Config (tests/config/)
- **test_settings.py**: Environment variable loading and validation

### Services (tests/services/)
- **test_midi_client.py**: MIDI API client, authentication, retries
- **test_twitch_client.py**: OAuth token validation and expiry warnings

### Bots (tests/bots/)
- **test_twitch_bot.py**: TwitchIO integration, message handling, command routing

## Fixtures (conftest.py)

Shared test fixtures:
- `mock_midi_client`: Mocked MIDI service client
- `mock_twitch_context`: Mocked Twitch message context
- `mock_settings`: Test configuration settings

## Writing New Tests

1. Place tests in the appropriate directory based on component type
2. Use descriptive test names: `test_<behavior>_<scenario>`
3. Use fixtures from conftest.py for common mocks
4. Mark async tests with `@pytest.mark.asyncio`
5. Include docstrings explaining test intent
6. Group related tests in a single test class

Example:
```python
"""Tests for ExampleHandler."""

import pytest
from commands.handlers.example import ExampleHandler


class TestExampleHandler:
    """Test cases for ExampleHandler."""
    
    @pytest.fixture
    def handler(self, mock_midi_client):
        """Create handler instance."""
        return ExampleHandler(mock_midi_client)
    
    @pytest.mark.asyncio
    async def test_handle_valid_input(self, handler, mock_twitch_context):
        """Test handler with valid input."""
        response = await handler.handle(["arg"], mock_twitch_context)
        assert "expected" in response
```

## Total Test Count

56 tests across 7 test files

## CI/CD Integration

Tests run automatically on:
- Pull requests to main/dev branches
- Push to main/dev branches
- Manual workflow dispatch

See `.github/workflows/ui-release.yaml` for CI/CD configuration.

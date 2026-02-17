"""Tests for configuration settings."""

import pytest
from pydantic import ValidationError
from pydantic_settings import BaseSettings
from pydantic import ConfigDict


class TestSettings:
    """Test cases for Settings configuration behavior."""
    
    def test_settings_can_load_from_env(self, monkeypatch):
        """Test that BaseSettings subclass can load from environment variables."""
        # Set some environment variables
        monkeypatch.setenv("TEST_FIELD", "test_value")
        monkeypatch.setenv("ANOTHER_FIELD", "another_value")
        
        # Define a test settings class
        class TestConfig(BaseSettings):
            # Avoid loading the repository .env during tests; rely on monkeypatch env instead
            model_config = ConfigDict(env_file=None, case_sensitive=False)
            test_field: str
            another_field: str
        
        config = TestConfig()
        
        # Verify environment variables were loaded
        assert config.test_field == "test_value"
        assert config.another_field == "another_value"
    
    def test_settings_can_use_defaults(self, monkeypatch):
        """Test that BaseSettings subclass uses default values for optional fields."""
        monkeypatch.setenv("REQUIRED_FIELD", "value")
        
        class TestConfig(BaseSettings):
            model_config = ConfigDict(env_file=None, case_sensitive=False)
            required_field: str
            optional_field: str = "default_value"
        
        config = TestConfig()
        
        # Verify default was applied
        assert config.optional_field == "default_value"
    
    def test_settings_validates_required_fields(self):
        """Test that BaseSettings validation fails when required fields are missing."""
        class TestConfig(BaseSettings):
            model_config = ConfigDict(env_file=None, case_sensitive=False)
            required_field: str
        
        # Should raise validation error when required field is missing
        with pytest.raises(ValidationError):
            TestConfig()

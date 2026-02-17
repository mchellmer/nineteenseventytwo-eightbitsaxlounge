# Twitch Bot Setup

Quick guide for getting a Twitch configured before running the bot

## Overview

Twitchio v3 handles token lifecycle automatically so only a client id and secret are required.

## One-Time Setup Steps

### 1. Create Twitch Accounts

**Main Streaming Account**: Your existing Twitch account

**Bot Account** (recommended): Create a separate account at [twitch.tv](https://www.twitch.tv) with a bot-like username (e.g., "YourChannelBot")

### 2. Register Your Application

1. Log in to [dev.twitch.tv/console](https://dev.twitch.tv/console) with your **main account**
2. Click **"Register Your Application"**
3. Fill in:
   - **Name**: `EightBitSaxLoungeBot`
   - **OAuth Redirect URLs**: ` http://localhost:4343/oauth/callback`
   - **Category**: `Chat Bot`
4. Click **"Create"**
5. **Save** your **Client ID** -> provide in env on deployment e.g. via github workflow
6. Create a **Client Secret** -> ditto

# Twitch Bot Setup

Quick guide for getting a Twitch access token and storing it in GitHub secrets.

## Overview

Twitch api access to chat is permissioned by oauth token which expire after ~60 days. There is some setup to permission the bot account to access the streaming account which tools to streamline the process.

## One-Time Setup Steps

### 1. Create Twitch Accounts

**Main Streaming Account**: Your existing Twitch account

**Bot Account** (recommended): Create a separate account at [twitch.tv](https://www.twitch.tv) with a bot-like username (e.g., "YourChannelBot")

### 2. Register Your Application

1. Log in to [dev.twitch.tv/console](https://dev.twitch.tv/console) with your **main account**
2. Click **"Register Your Application"**
3. Fill in:
   - **Name**: `EightBitSaxLoungeBot`
   - **OAuth Redirect URLs**: `http://localhost:3000`
   - **Category**: `Chat Bot`
4. Click **"Create"**
5. **Save** your **Client ID**

### 3. Generate Access Token

1. Build this URL (replace `YOUR_CLIENT_ID` with your Client ID from step 2):
   ```
   https://id.twitch.tv/oauth2/authorize?client_id=YOUR_CLIENT_ID&redirect_uri=http://localhost:3000&response_type=token&scope=chat:read+chat:edit+channel:moderate
   ```

2. **Log in to Twitch with your BOT account**
3. Visit the URL in your browser
4. Click **"Authorize"**
5. You'll be redirected to a page so **Copy the URL** from your browser's address bar
   - Example: `http://localhost:3000/#access_token=abc123xyz...&scope=chat:read+chat:edit`
7. Extract the `access_token` value
8. Store with `oauth:` prefix

## Token Expiry Management

### Monitoring

The bot automatically validates the token on startup and logs warnings:

- **30+ days remaining**: Info message
- **14-30 days remaining**: Warning
- **< 7 days remaining**: Error (urgent)

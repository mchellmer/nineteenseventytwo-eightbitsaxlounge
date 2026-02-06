#!/bin/bash

# Note that the runner will remove itself from github due to inactivity after some time
#  in this case run: sudo ./svc.sh uninstall && ./config.sh remove && rm -rf $HOME/actions-runner-*

# Variables
RUNNER_VERSION="2.331.0"
RUNNER_URL="https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-arm64-${RUNNER_VERSION}.tar.gz"
REPO_URL="https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge"
HASH="f5863a211241436186723159a111f352f25d5d22711639761ea24c98caef1a9a"

# Check if tokens are provided as arguments
if [ -z "$1" ] || [ -z "$2" ]; then
  echo "Error: Two GitHub Actions Runner tokens are required."
  echo "Usage: $0 <TOKEN_RUNNER_1> <TOKEN_RUNNER_2>"
  exit 1
fi

TOKEN_1=$1
TOKEN_2=$2

# Install prerequisites
echo "Installing prerequisites..."
sudo apt update
sudo apt install libicu-dev libssl-dev libcurl4-openssl-dev -y

# Function to setup a runner
setup_runner() {
  local RUNNER_NAME=$1
  local TOKEN=$2
  local RUNNER_FOLDER="$HOME/actions-runner-${RUNNER_NAME}"
  
  echo ""
  echo "========================================"
  echo "Setting up runner: ${RUNNER_NAME}"
  echo "========================================"
  
  # Create a folder for the runner
  echo "Creating folder for GitHub Actions Runner in $RUNNER_FOLDER..."
  mkdir -p $RUNNER_FOLDER && cd $RUNNER_FOLDER
  
  # Download the latest runner package (only if not already present)
  if [ ! -f "actions-runner-linux-arm64-${RUNNER_VERSION}.tar.gz" ]; then
    echo "Downloading GitHub Actions Runner version $RUNNER_VERSION..."
    curl -o actions-runner-linux-arm64-${RUNNER_VERSION}.tar.gz -L $RUNNER_URL
    
    # Validate the hash
    echo "Validating the hash..."
    echo "${HASH}  actions-runner-linux-arm64-${RUNNER_VERSION}.tar.gz" | shasum -a 256 -c
  else
    echo "Runner package already downloaded, skipping..."
  fi
  
  # Extract the installer
  echo "Extracting the runner package..."
  tar xzf ./actions-runner-linux-arm64-${RUNNER_VERSION}.tar.gz
  
  # Configure the runner
  echo "Configuring the GitHub Actions Runner: ${RUNNER_NAME}..."
  ./config.sh --url $REPO_URL --token $TOKEN --name $RUNNER_NAME --labels self-hosted,Linux,ARM64,$RUNNER_NAME
  
  # Set up as a systemd service
  echo "Setting up the GitHub Actions Runner as a systemd service..."
  sudo ./svc.sh install
  
  # Start the service
  echo "Starting the GitHub Actions Runner service..."
  sudo ./svc.sh start
  
  echo "Runner ${RUNNER_NAME} setup complete!"
}

# Setup both runners
setup_runner "runner-1" "$TOKEN_1"
setup_runner "runner-2" "$TOKEN_2"

echo ""
echo "========================================"
echo "Both runners setup complete!"
echo "========================================"
echo "Check status with:"
echo "  sudo ./svc.sh status (from each runner directory)"
echo "Or check systemctl services:"
echo "  sudo systemctl status actions.runner.*"
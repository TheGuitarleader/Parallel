#!/usr/bin/env bash
set -e

REPO="EntexInteractive/Parallel"
ASSET_PATTERN="Parallel-.*-cmd-linux-x64.zip"
INSTALL_ROOT="/usr/local/lib/parallel"
BIN_LINK="/usr/local/bin/parallel"
TMP_DIR="$(mktemp -d)"
ZIP_FILE="$TMP_DIR/parallel.zip"
BIN_NAME=$(ls "$INSTALL_ROOT" | grep -i parallel | head -n 1)

command -v curl >/dev/null || { echo "curl required"; exit 1; }
command -v unzip >/dev/null || { echo "unzip required"; exit 1; }

echo "Fetching latest release info..."
DOWNLOAD_URL=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" | grep -E "browser_download_url.*$ASSET_PATTERN" | cut -d '"' -f 4)
[[ -z "$DOWNLOAD_URL" ]] && { echo "Could not find a valid release."; exit 1; }

echo "Downloading latest release..."
curl -sSL -o "$ZIP_FILE" "$DOWNLOAD_URL"

echo "Installing..."
rm -rf "$INSTALL_ROOT"
mkdir -p "$INSTALL_ROOT"
unzip -q "$ZIP_FILE" -d "$INSTALL_ROOT"
chmod 755 "$INSTALL_ROOT/$BIN_NAME"
ln -sf "$INSTALL_ROOT/$BIN_NAME" "$BIN_LINK"

echo Cleaning up...
rm -rf "$TMP_DIR"

echo "Installation complete. You can now run Parallel from the command line."

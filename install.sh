#!/usr/bin/env bash
set -e

REPO="TheGuitarleader/Parallel"
ASSET_PATTERN="Parallel-.*-cmd-linux-x64.zip"
INSTALL_ROOT="/usr/local/lib/parallel"
BIN_LINK="/usr/local/bin/parallel"
TMP_DIR="$(mktemp -d)"
ZIP_FILE="$TMP_DIR/parallel.zip"

DOWNLOAD_URL=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" | grep -E "browser_download_url.*$ASSET_PATTERN" | cut -d '"' -f 4)
[[ -z "$DOWNLOAD_URL" ]] && {
  echo "Could not find a release."
  exit 1
}

echo "Downloading $DOWNLOAD_URL"
curl -L "$DOWNLOAD_URL" -o "$ZIP_FILE"

echo "Installing to $INSTALL_ROOT"
rm -rf "$INSTALL_ROOT"
mkdir -p "$INSTALL_ROOT"

unzip -q "$ZIP_FILE" -d "$INSTALL_ROOT"
chmod 755 "$INSTALL_ROOT/Parallel"
ln -sf "$INSTALL_ROOT/Parallel" "$BIN_LINK"
rm -rf "$TMP_DIR"

echo "Complete!"
parallel --version || true

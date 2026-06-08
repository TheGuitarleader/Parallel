#!/usr/bin/env bash
set -e

REPO="TheGuitarleader/Parallel"
ASSET_PATTERN="Parallel-.*-cmd-linux-x64.zip"
DOWNLOAD_URL=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" | grep -E "browser_download_url.*$ASSET_PATTERN" | cut -d '"' -f 4)

[[ -z "$DOWNLOAD_URL" ]] && {
  echo "Could not find a current release."
  exit 1
}

#!/bin/bash

set -e

TARGET_DIR="/opt/mplauthservice"
SERVICE_NAME="mplauth.service"
OWNER="authapi:authapi"

echo "Searching for latest auth*.zip archive..."

ARCHIVE=$(ls -t mpl-auth-service-*.zip 2>/dev/null | head -n 1)

if [ -z "$ARCHIVE" ]; then
    echo "No mpl-auth-service-*.zip archive found."
    exit 1

fi

echo "Using archive: $ARCHIVE"

WORKDIR=$(mktemp -d)
unzip -q "$ARCHIVE" -d "$WORKDIR"
EXTRACT_BASE="$WORKDIR"

# Step into single top-level folder if present (foldered archive)
COUNT=$(find "$WORKDIR" -mindepth 1 -maxdepth 1 | wc -l)
INNER=$(find "$WORKDIR" -mindepth 1 -maxdepth 1 -type d | head -n 1)
if [ "$COUNT" -eq 1 ] && [ -d "$INNER" ]; then
    WORKDIR="$INNER"
fi

if [ ! -f "$WORKDIR/MplAuthService" ]; then
    echo "Executable 'MplAuthService' not found in archive."
    exit 1
fi

if [ ! -f "$WORKDIR/MplAuthService.pdb" ]; then
    echo "'MplAuthService.pdb' not found in archive."
    exit 1
fi

echo "Stopping service..."
systemctl stop "$SERVICE_NAME"

echo "Ensuring target directory exists..."
mkdir -p "$TARGET_DIR"

echo "Copying executable and pdb..."
cp "$WORKDIR/MplAuthService" "$TARGET_DIR/"
cp "$WORKDIR/MplAuthService.pdb" "$TARGET_DIR/"

if [ -f "$WORKDIR/appsettings.json" ]; then
    if [ -f "$TARGET_DIR/appsettings.json" ]; then
        read -p "appsettings.json exists. Overwrite? (y/N): " CONFIRM
        if [[ "$CONFIRM" =~ ^[Yy]$ ]]; then
            cp "$WORKDIR/appsettings.json" "$TARGET_DIR/"
            echo "appsettings.json overwritten."
        else
            echo "Keeping existing appsettings.json."
        fi
    else
        cp "$WORKDIR/appsettings.json" "$TARGET_DIR/"
        echo "appsettings.json copied."
    fi
fi

echo "Setting ownership..."
chown "$OWNER" "$TARGET_DIR/MplAuthService" "$TARGET_DIR/MplAuthService.pdb"

if [ -f "$TARGET_DIR/appsettings.json" ]; then
    chown "$OWNER" "$TARGET_DIR/appsettings.json"
fi

echo "Making executable runnable..."
chmod +x "$TARGET_DIR/MplAuthService"

echo "Starting service..."
systemctl start "$SERVICE_NAME"

echo "Service status:"
systemctl status "$SERVICE_NAME" --no-pager

echo "Cleaning up..."
rm -rf "$EXTRACT_BASE"

echo "Deployment complete."
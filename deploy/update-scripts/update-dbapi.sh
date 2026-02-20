#!/bin/bash

set -e

TARGET_DIR="/opt/mpldbapi"
SERVICE_NAME="mpldbapi.service"
OWNER="dbapi:dbapi"

echo "Searching for latest mpl-dbapi-service-*.zip archive..."

ARCHIVE=$(ls -t mpl-dbapi-service-*.zip 2>/dev/null | head -n 1)

if [ -z "$ARCHIVE" ]; then
    echo "No mpl-dbapi-service-*.zip archive found."
    exit 1

fi

echo "Using archive: $ARCHIVE"

WORKDIR=$(mktemp -d)
unzip -q "$ARCHIVE" -d "$WORKDIR"

if [ ! -f "$WORKDIR/MplDbApi" ]; then
    echo "Executable 'MplDbApi' not found in archive."
    exit 1
fi

if [ ! -f "$WORKDIR/MplDbApi.pdb" ]; then
    echo "'MplDbApi.pdb' not found in archive."
    exit 1
fi

echo "Stopping service..."
systemctl stop "$SERVICE_NAME"

echo "Ensuring target directory exists..."
mkdir -p "$TARGET_DIR"

echo "Copying executable and pdb..."
cp "$WORKDIR/MplDbApi" "$TARGET_DIR/"
cp "$WORKDIR/MplDbApi.pdb" "$TARGET_DIR/"

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
chown "$OWNER" "$TARGET_DIR/MplDbApi" "$TARGET_DIR/MplDbApi.pdb"

if [ -f "$TARGET_DIR/appsettings.json" ]; then
    chown "$OWNER" "$TARGET_DIR/appsettings.json"
fi

echo "Making executable runnable..."
chmod +x "$TARGET_DIR/MplDbApi"

echo "Starting service..."
systemctl start "$SERVICE_NAME"

echo "Service status:"
systemctl status "$SERVICE_NAME" --no-pager

echo "Cleaning up..."
rm -rf "$WORKDIR"

echo "Deployment complete."
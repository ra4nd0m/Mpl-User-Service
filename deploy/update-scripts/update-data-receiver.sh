#!/bin/bash

set -e

TARGET_DIR="/opt/mpl-data-receiver"
SERVICE_NAME="mpl-data-receiver.service"
OWNER="datareceiver:datareceiver"

echo "Searching for latest mpl-data-receiver-*.zip archive..."

ARCHIVE=$(ls -t mpl-data-receiver-*.zip 2>/dev/null | head -n 1)

if [ -z "$ARCHIVE" ]; then
    echo "No mpl-data-receiver-*.zip archive found."
    exit 1

fi

echo "Using archive: $ARCHIVE"

WORKDIR=$(mktemp -d)
unzip -q "$ARCHIVE" -d "$WORKDIR"

if [ ! -f "$WORKDIR/MplDataReceiver" ]; then
    echo "Executable 'MplDataReceiver' not found in archive."
    exit 1
fi

if [ ! -f "$WORKDIR/MplDataReceiver.pdb" ]; then
    echo "'MplDataReceiver.pdb' not found in archive."
    exit 1
fi

echo "Stopping service..."
systemctl stop "$SERVICE_NAME"

echo "Ensuring target directory exists..."
mkdir -p "$TARGET_DIR"

echo "Copying executable and pdb..."
cp "$WORKDIR/MplDataReceiver" "$TARGET_DIR/"
cp "$WORKDIR/MplDataReceiver.pdb" "$TARGET_DIR/"

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
chown "$OWNER" "$TARGET_DIR/MplDataReceiver" "$TARGET_DIR/MplDataReceiver.pdb"

if [ -f "$TARGET_DIR/appsettings.json" ]; then
    chown "$OWNER" "$TARGET_DIR/appsettings.json"
fi

echo "Making executable runnable..."
chmod +x "$TARGET_DIR/MplDataReceiver"

echo "Starting service..."
systemctl start "$SERVICE_NAME"

echo "Service status:"
systemctl status "$SERVICE_NAME" --no-pager

echo "Cleaning up..."
rm -rf "$WORKDIR"

echo "Deployment complete."
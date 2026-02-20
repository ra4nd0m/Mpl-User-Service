#!/bin/bash

set -e

TARGET_DIR="/var/www/dashboard_spa"
SERVICE_NAME="nginx"

echo "Searching for latest mpl-ui-*.zip archive..."

ARCHIVE=$(ls -t mpl-ui-*.zip 2>/dev/null | head -n 1)

if [ -z "$ARCHIVE" ]; then
    echo "No mpl-ui-*.zip archive found."
    exit 1

fi

echo "Using archive: $ARCHIVE"

WORKDIR=$(mktemp -d)
unzip -q "$ARCHIVE" -d "$WORKDIR"

echo "Ensuring target directory exists..."
mkdir -p "$TARGET_DIR"

echo "Clearing old SPA files..."
rm -rf "${TARGET_DIR:?}/"*

echo "Copying new SPA files..."
cp -r "$WORKDIR"/* "$TARGET_DIR/"

echo "Setting ownership..."
chown -R www-data:www-data "$TARGET_DIR"

echo "Reloading nginx..."
systemctl reload "$SERVICE_NAME"

echo "nginx status:"
systemctl status "$SERVICE_NAME" --no-pager

echo "Cleaning up..."
rm -rf "$WORKDIR"

echo "UI deployment complete."
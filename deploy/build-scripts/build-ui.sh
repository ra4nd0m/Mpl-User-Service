#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/../.." &>/dev/null && pwd)"
UI_DIR="${PROJECT_DIR}/ui"
RELEASES_DIR="${PROJECT_DIR}/releases/ui"
TIMESTAMP=$(date +"%Y%m%d%H%M%S")
RELEASE_NAME="mpl-ui-${TIMESTAMP}"
BUILD_DIR="${RELEASES_DIR}/temp/${RELEASE_NAME}"

echo "Building MPL UI application..."

# Create releases directory if it doesn't exist
mkdir -p "${RELEASES_DIR}"
mkdir -p "${BUILD_DIR}"

# Navigate to UI directory
cd "${UI_DIR}"

# Build the UI
npm run build

# Copy the built files to the build directory
cp -r build/* "${BUILD_DIR}"

# Create zip file
cd "${BUILD_DIR}/.."
zip -r "${RELEASES_DIR}/${RELEASE_NAME}.zip" "${RELEASE_NAME}"

# Clean up temp directory
rm -rf "${RELEASES_DIR}/temp"

echo "Build completed successfully. Release package is available at: ${RELEASES_DIR}/${RELEASE_NAME}.zip"
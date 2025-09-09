#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/../.." &>/dev/null && pwd)"
DATA_RECEIVER_DIR="${PROJECT_DIR}/parsers/data-receiver/MplDataReceiver"
RELEASES_DIR="${PROJECT_DIR}/releases/data-receiver"
TIMESTAMP=$(date +"%Y%m%d%H%M%S")
RELEASE_NAME="mpl-data-receiver-service-${TIMESTAMP}"
BUILD_DIR="${RELEASES_DIR}/temp/${RELEASE_NAME}"

echo "Building MplDataReceiver for Linux x64..."

# Create releases directory if it doesn't exist
mkdir -p "${RELEASES_DIR}"
mkdir -p "${BUILD_DIR}"

# Navigate to data receiver directory
cd "${DATA_RECEIVER_DIR}"

# Clean and build the solution
dotnet clean MplDataReceiver.sln --configuration Release
dotnet publish MplDataReceiver.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --output "${BUILD_DIR}" \
    /p:PublishSingleFile=true \
    /p:SelfContained=false
# Create zip file
cd "${BUILD_DIR}/.."
zip -r "${RELEASES_DIR}/${RELEASE_NAME}.zip" "${RELEASE_NAME}"

# Clean up temp directory
rm -rf "${RELEASES_DIR}/temp"

echo "Build completed successfully. Release package is available at: ${RELEASES_DIR}/${RELEASE_NAME}.zip"
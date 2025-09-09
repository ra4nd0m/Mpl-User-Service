#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/../.." &>/dev/null && pwd)"
USERAPI_DIR="${PROJECT_DIR}/userapi"
RELEASES_DIR="${PROJECT_DIR}/releases/userapi"
TIMESTAMP=$(date +"%Y%m%d%H%M%S")
RELEASE_NAME="mpl-user-api-${TIMESTAMP}"
BUILD_DIR="${RELEASES_DIR}/temp/${RELEASE_NAME}"

echo "Building MplUserApi for Linux x64..."

# Create releases directory if it doesn't exist
mkdir -p "${RELEASES_DIR}"
mkdir -p "${BUILD_DIR}"

# Navigate to userapi directory
cd "${USERAPI_DIR}"

# Clean and build the solution
dotnet clean MplUserApi.sln --configuration Release
dotnet publish src/MplUserService/MplUserService.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --output "${BUILD_DIR}" \
    /p:PublishSingleFile=true \
    /p:SelfContained=false \
    /p:DebugType=portable \
    /p:Deterministic=true \
    /p:ContinuousIntegrationBuild=true
# Create zip file
cd "${BUILD_DIR}/.."
zip -r "${RELEASES_DIR}/${RELEASE_NAME}.zip" "${RELEASE_NAME}"

# Clean up temp directory
rm -rf "${RELEASES_DIR}/temp"

echo "Build completed successfully. Release package is available at: ${RELEASES_DIR}/${RELEASE_NAME}.zip"
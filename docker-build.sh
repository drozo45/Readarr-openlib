#!/bin/bash
# Docker build script for Readarr with OpenLibrary

set -e

echo "=================================="
echo "Readarr with OpenLibrary"
echo "Docker Build Script"
echo "=================================="
echo ""

# Default values
IMAGE_NAME="readarr-openlibrary"
TAG="latest"
PLATFORM="linux/amd64"
NO_CACHE=""

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --tag|-t)
      TAG="$2"
      shift 2
      ;;
    --platform|-p)
      PLATFORM="$2"
      shift 2
      ;;
    --no-cache)
      NO_CACHE="--no-cache"
      shift
      ;;
    --push)
      PUSH=true
      shift
      ;;
    --help|-h)
      echo "Usage: ./docker-build.sh [OPTIONS]"
      echo ""
      echo "Options:"
      echo "  --tag, -t TAG         Set image tag (default: latest)"
      echo "  --platform, -p PLAT   Set platform (default: linux/amd64)"
      echo "  --no-cache            Build without cache"
      echo "  --push                Push to registry after build"
      echo "  --help, -h            Show this help"
      echo ""
      echo "Examples:"
      echo "  ./docker-build.sh"
      echo "  ./docker-build.sh --tag v1.0.0"
      echo "  ./docker-build.sh --platform linux/arm64"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      echo "Use --help for usage information"
      exit 1
      ;;
  esac
done

FULL_IMAGE="${IMAGE_NAME}:${TAG}"

echo "Building Docker image:"
echo "  Image: $FULL_IMAGE"
echo "  Platform: $PLATFORM"
echo ""

# Build the image
echo "🔨 Building image..."
docker build \
  $NO_CACHE \
  --platform "$PLATFORM" \
  -t "$FULL_IMAGE" \
  -f Dockerfile \
  .

if [ $? -eq 0 ]; then
  echo ""
  echo "=================================="
  echo "✅ Build successful!"
  echo "=================================="
  echo ""
  echo "Image: $FULL_IMAGE"
  echo ""
  echo "To run the container:"
  echo "  docker-compose up -d"
  echo ""
  echo "Or run manually:"
  echo "  docker run -d \\"
  echo "    --name readarr-openlibrary \\"
  echo "    -p 8787:8787 \\"
  echo "    -v ./config:/config \\"
  echo "    -v ./books:/books \\"
  echo "    $FULL_IMAGE"
  echo ""
  
  if [ "$PUSH" = true ]; then
    echo "🚀 Pushing image to registry..."
    docker push "$FULL_IMAGE"
  fi
else
  echo ""
  echo "❌ Build failed"
  exit 1
fi

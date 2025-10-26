# 🛠️ Architecture Mismatch Fix (QEMU Error)

## Problem
Getting this error when running Docker:
```
qemu-x86_64: Could not open '/lib64/ld-linux-x86-64.so.2': No such file or directory
```

**Cause:** You're on ARM64 (Apple Silicon Mac) but the container was built for x86_64.

---

## ✅ Solution: Rebuild for Your Architecture

The Dockerfile now **auto-detects** your architecture and builds natively!

### Step 1: Remove Old Image
```bash
# Stop and remove the container
docker-compose down

# Remove the old x86_64 image
docker rmi readarr-openlibrary:latest
```

### Step 2: Rebuild for ARM64 (Native)
```bash
# Using docker-compose (auto-detects platform)
docker-compose build

# Or using the build script (auto-detects platform)
./docker-build.sh

# Or manually specify ARM64
./docker-build.sh --platform linux/arm64
```

### Step 3: Run the Container
```bash
docker-compose up -d
```

---

## How It Works

The Dockerfile now:
1. **Auto-detects** your architecture using Docker's `TARGETARCH` variable
2. **Builds natively** for ARM64 on Apple Silicon (fast!)
3. **Builds for x86_64** on Intel/AMD systems
4. **No QEMU emulation needed** - full native performance

### Build Detection Logic
```dockerfile
ARG TARGETARCH  # Docker automatically sets this

# If ARM64, use linux-arm64
# If AMD64/x86_64, use linux-x64
RUN if [ "$TARGETARCH" = "arm64" ]; then \
      dotnet restore --runtime linux-arm64; \
    else \
      dotnet restore --runtime linux-x64; \
    fi
```

---

## Verify Your Architecture

```bash
# Check your system architecture
uname -m
# arm64 or aarch64 = Apple Silicon / ARM
# x86_64 = Intel/AMD

# Check what the container was built for
docker inspect readarr-openlibrary:latest | grep Architecture
```

---

## Force Specific Architecture (Advanced)

If you need to build for a specific platform:

```bash
# Force x86_64 (slower on ARM, needs QEMU emulation)
./docker-build.sh --platform linux/amd64

# Force ARM64 (won't work on x86_64 systems)
./docker-build.sh --platform linux/arm64

# Multi-architecture build (requires buildx)
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t readarr-openlibrary:latest \
  .
```

---

## Expected Build Output

You should see:
```
Building for architecture: arm64
```
or
```
Building for architecture: amd64
```

Then the build proceeds with the correct runtime identifier.

---

## Still Getting Errors?

1. **Make sure Docker is updated:**
   ```bash
   docker --version  # Should be 20.10+ for buildx
   ```

2. **Enable Docker buildx (if needed):**
   ```bash
   docker buildx create --use
   ```

3. **Clean rebuild:**
   ```bash
   docker-compose down
   docker system prune -a  # WARNING: Removes all unused images
   docker-compose build --no-cache
   docker-compose up -d
   ```

---

## Performance Comparison

| Build Type | Performance on ARM64 | Notes |
|------------|---------------------|-------|
| Native ARM64 | ⚡ **Fast** | No emulation, recommended |
| x86_64 via QEMU | 🐌 Slow (2-10x slower) | Works but not recommended |

**Always build natively for best performance!**

---

## Summary

1. ✅ **Delete old image**: `docker rmi readarr-openlibrary:latest`
2. ✅ **Rebuild natively**: `./docker-build.sh` (auto-detects)
3. ✅ **Run**: `docker-compose up -d`
4. ✅ **Enjoy native ARM64 speed!** 🚀

The fix is complete - your Dockerfile now supports both architectures automatically!

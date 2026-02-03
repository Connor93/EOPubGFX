#!/bin/bash

# SOE Pub Editor - Cross-Platform Build Script
# Usage:
#   ./build.sh windows   - Build for Windows
#   ./build.sh linux     - Build for Linux  
#   ./build.sh mac       - Build for macOS (creates .app bundle)
#   ./build.sh all       - Build all platforms

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR"
RELEASE_DIR="$PROJECT_DIR/release"
APP_NAME="SOE_PubEditor"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${GREEN}[BUILD]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Create release directories
setup_directories() {
    mkdir -p "$RELEASE_DIR/windows"
    mkdir -p "$RELEASE_DIR/linux"
    mkdir -p "$RELEASE_DIR/mac"
}

# Build for Windows
build_windows() {
    print_status "Building for Windows (x64)..."
    
    dotnet publish "$PROJECT_DIR" \
        -c Release \
        -r win-x64 \
        -o "$RELEASE_DIR/windows" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:EnableCompressionInSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true
    
    print_status "Windows build complete: $RELEASE_DIR/windows/$APP_NAME.exe"
}

# Build for Linux
build_linux() {
    print_status "Building for Linux (x64)..."
    
    dotnet publish "$PROJECT_DIR" \
        -c Release \
        -r linux-x64 \
        -o "$RELEASE_DIR/linux" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:EnableCompressionInSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true
    
    # Make executable
    chmod +x "$RELEASE_DIR/linux/$APP_NAME"
    
    # Copy icon for Linux desktop integration
    cp "$PROJECT_DIR/Assets/icon.png" "$RELEASE_DIR/linux/"
    
    print_status "Linux build complete: $RELEASE_DIR/linux/$APP_NAME"
}

# Build for macOS
build_mac() {
    print_status "Building for macOS (x64 + arm64)..."
    
    MAC_DIR="$RELEASE_DIR/mac"
    APP_BUNDLE="$MAC_DIR/$APP_NAME.app"
    
    # Clean previous build
    rm -rf "$APP_BUNDLE"
    
    # Build for both architectures
    print_status "Building macOS x64..."
    dotnet publish "$PROJECT_DIR" \
        -c Release \
        -r osx-x64 \
        -o "$MAC_DIR/x64" \
        --self-contained true \
        -p:PublishSingleFile=false
    
    print_status "Building macOS ARM64..."
    dotnet publish "$PROJECT_DIR" \
        -c Release \
        -r osx-arm64 \
        -o "$MAC_DIR/arm64" \
        --self-contained true \
        -p:PublishSingleFile=false
    
    # Create .app bundle structure
    print_status "Creating macOS .app bundle..."
    mkdir -p "$APP_BUNDLE/Contents/MacOS"
    mkdir -p "$APP_BUNDLE/Contents/Resources"
    
    # Create universal binary using lipo (if both architectures built)
    if [ -f "$MAC_DIR/x64/$APP_NAME" ] && [ -f "$MAC_DIR/arm64/$APP_NAME" ]; then
        print_status "Creating universal binary..."
        lipo -create \
            "$MAC_DIR/x64/$APP_NAME" \
            "$MAC_DIR/arm64/$APP_NAME" \
            -output "$APP_BUNDLE/Contents/MacOS/$APP_NAME"
        
        # Copy dependencies from arm64 build (they should be compatible)
        cp -R "$MAC_DIR/arm64/"*.dylib "$APP_BUNDLE/Contents/MacOS/" 2>/dev/null || true
        cp -R "$MAC_DIR/arm64/"*.dll "$APP_BUNDLE/Contents/MacOS/" 2>/dev/null || true
        cp -R "$MAC_DIR/arm64/"*.json "$APP_BUNDLE/Contents/MacOS/" 2>/dev/null || true
    else
        # Fall back to single architecture
        if [ -f "$MAC_DIR/arm64/$APP_NAME" ]; then
            cp -R "$MAC_DIR/arm64/"* "$APP_BUNDLE/Contents/MacOS/"
        else
            cp -R "$MAC_DIR/x64/"* "$APP_BUNDLE/Contents/MacOS/"
        fi
    fi
    
    # Create .icns icon
    create_mac_icon
    
    # Create Info.plist
    cat > "$APP_BUNDLE/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>SOE Pub Editor</string>
    <key>CFBundleDisplayName</key>
    <string>Shards of Eternity - Pub Editor</string>
    <key>CFBundleIdentifier</key>
    <string>com.shardsofeternity.pubeditor</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
</dict>
</plist>
EOF
    
    # Clean up temp build directories
    rm -rf "$MAC_DIR/x64" "$MAC_DIR/arm64"
    
    # Make executable
    chmod +x "$APP_BUNDLE/Contents/MacOS/$APP_NAME"
    
    print_status "macOS build complete: $APP_BUNDLE"
}

# Create macOS .icns icon
create_mac_icon() {
    print_status "Creating macOS icon..."
    
    ICON_SRC="$PROJECT_DIR/Assets/icon.png"
    ICONSET_DIR="$PROJECT_DIR/Assets/AppIcon.iconset"
    ICNS_OUTPUT="$RELEASE_DIR/mac/$APP_NAME.app/Contents/Resources/AppIcon.icns"
    
    if [ ! -f "$ICON_SRC" ]; then
        print_warning "Icon file not found: $ICON_SRC"
        return
    fi
    
    # Create iconset directory
    mkdir -p "$ICONSET_DIR"
    
    # Generate all required icon sizes
    sips -z 16 16     "$ICON_SRC" --out "$ICONSET_DIR/icon_16x16.png" 2>/dev/null
    sips -z 32 32     "$ICON_SRC" --out "$ICONSET_DIR/icon_16x16@2x.png" 2>/dev/null
    sips -z 32 32     "$ICON_SRC" --out "$ICONSET_DIR/icon_32x32.png" 2>/dev/null
    sips -z 64 64     "$ICON_SRC" --out "$ICONSET_DIR/icon_32x32@2x.png" 2>/dev/null
    sips -z 128 128   "$ICON_SRC" --out "$ICONSET_DIR/icon_128x128.png" 2>/dev/null
    sips -z 256 256   "$ICON_SRC" --out "$ICONSET_DIR/icon_128x128@2x.png" 2>/dev/null
    sips -z 256 256   "$ICON_SRC" --out "$ICONSET_DIR/icon_256x256.png" 2>/dev/null
    sips -z 512 512   "$ICON_SRC" --out "$ICONSET_DIR/icon_256x256@2x.png" 2>/dev/null
    sips -z 512 512   "$ICON_SRC" --out "$ICONSET_DIR/icon_512x512.png" 2>/dev/null
    sips -z 1024 1024 "$ICON_SRC" --out "$ICONSET_DIR/icon_512x512@2x.png" 2>/dev/null
    
    # Convert to .icns
    iconutil -c icns "$ICONSET_DIR" -o "$ICNS_OUTPUT"
    
    # Clean up iconset
    rm -rf "$ICONSET_DIR"
    
    print_status "macOS icon created: $ICNS_OUTPUT"
}

# Build all platforms
build_all() {
    print_status "Building for all platforms..."
    build_windows
    build_linux
    build_mac
    print_status "All builds complete!"
    echo ""
    echo "Release files:"
    echo "  Windows: $RELEASE_DIR/windows/$APP_NAME.exe"
    echo "  Linux:   $RELEASE_DIR/linux/$APP_NAME"
    echo "  macOS:   $RELEASE_DIR/mac/$APP_NAME.app"
}

# Show usage
show_usage() {
    echo "SOE Pub Editor - Cross-Platform Build Script"
    echo ""
    echo "Usage: $0 <platform>"
    echo ""
    echo "Platforms:"
    echo "  windows  - Build for Windows (x64)"
    echo "  linux    - Build for Linux (x64)"
    echo "  mac      - Build for macOS (Universal: x64 + ARM64)"
    echo "  all      - Build for all platforms"
    echo ""
    echo "Output: ./release/<platform>/"
}

# Main
setup_directories

case "${1:-}" in
    windows)
        build_windows
        ;;
    linux)
        build_linux
        ;;
    mac)
        build_mac
        ;;
    all)
        build_all
        ;;
    *)
        show_usage
        exit 1
        ;;
esac

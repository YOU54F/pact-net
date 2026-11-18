#!/usr/bin/env bash
# Pre-installs the Pact plugins needed by the samples.
#
# The plugin driver can auto-install a missing plugin, but when several tests
# start at once they race on creating the plugin directory and some fail with
# "Was not able to start plugin process". Installing up front avoids that.
set -euo pipefail

PROTOBUF_PLUGIN_VERSION="0.4.0"

case "$(uname -s)" in
    Linux*)               os="linux"; ext="" ;;
    Darwin*)              os="osx"; ext="" ;;
    MINGW*|MSYS*|CYGWIN*) os="windows"; ext=".exe" ;;
    *) echo "Unsupported OS: $(uname -s)" >&2; exit 1 ;;
esac

case "$(uname -m)" in
    x86_64|amd64)  arch="x86_64" ;;
    arm64|aarch64) arch="aarch64" ;;
    *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
esac

plugin_dir="${PACT_PLUGIN_DIR:-$HOME/.pact/plugins}/protobuf-$PROTOBUF_PLUGIN_VERSION"
base_url="https://github.com/pact-foundation/pact-protobuf-plugin/releases/download/v-$PROTOBUF_PLUGIN_VERSION"
asset="pact-protobuf-plugin-$os-$arch$ext.gz"

echo "Installing protobuf plugin $PROTOBUF_PLUGIN_VERSION ($asset) to $plugin_dir"
mkdir -p "$plugin_dir"
cd "$plugin_dir"

curl --silent --show-error --fail -L "$base_url/pact-plugin.json" -o pact-plugin.json
curl --silent --show-error --fail -L "$base_url/$asset" -o "$asset"
curl --silent --show-error --fail -L "$base_url/$asset.sha256" -o "$asset.sha256"

expected=$(awk '{print $1}' "$asset.sha256")
if command -v sha256sum > /dev/null; then
    actual=$(sha256sum "$asset" | awk '{print $1}')
else
    actual=$(shasum -a 256 "$asset" | awk '{print $1}')
fi
if [[ "$expected" != "$actual" ]]; then
    echo "Checksum mismatch for $asset: expected $expected, got $actual" >&2
    exit 1
fi

gunzip -f "$asset"
mv -f "${asset%.gz}" "pact-protobuf-plugin$ext"
chmod +x "pact-protobuf-plugin$ext"
rm -f "$asset.sha256"

echo "Installed: $(ls "$plugin_dir")"

#!/usr/bin/env bash
set -euo pipefail

FFI_VERSION="0.4.22"
FFI_BASE_URL="https://github.com/you54f/pact-reference/releases/download/libpact_ffi-v$FFI_VERSION"

GREEN="\e[32m"
YELLOW="\e[33m"
BLUE="\e[34m"
CLEAR="\e[0m"

base_path=$(dirname "$0")

download_native() {
    file="$1"
    os="$2"
    platform="$3"
    extension="$4"

    # e.g.
    #   pact_ffi-windows-x86_64.dll.gz
    #   libpact_ffi-linux-x86_64.so.gz
    #   libpact_ffi-osx-x86_64.dylib.gz
    src_file="$file-$os-$platform.$extension.gz"
    url="$FFI_BASE_URL/$src_file"
    sha="$url.sha256"

    path="$base_path/$os/$platform"
    dest_file="$file.$extension.gz"
    mkdir -p "$path"

    echo -e "Downloading FFI library for ${YELLOW}$os/$platform${CLEAR}"
    echo -e "    Destination: ${BLUE}$path/$dest_file${CLEAR}"
    echo -e "    URL: ${BLUE}$url${CLEAR}"

    echo -n "    Downloading... "
    curl --silent -L "$url" -o "$path/$dest_file"
    curl --silent -L "$sha" -o "$path/$dest_file.sha256"
    echo -e "${GREEN}OK${CLEAR}"

    echo -n "    Verifying... "

    if [[ "$OSTYPE" == "darwin"* ]]; then
        # OSX requires an empty arg passed to -i, but this doesn't work on Lin/Win
        sed -Ei '' "s|../release_artifacts/.+$|$path/$dest_file|" "$path/$dest_file.sha256"
        shasum -a 256 --check --quiet "$path/$dest_file.sha256"
    else
        sed -Ei "s|../release_artifacts/.+$|$path/$dest_file|" "$path/$dest_file.sha256"
        if [[ "$OSTYPE" == "linux"* ]]; then
            if ldd /bin/ls >/dev/null 2>&1; then
                ldd_output=$(ldd /bin/ls)
                case "$ldd_output" in
                    *musl*) 
                        sha256sum -c -s "$path/$dest_file.sha256"
                        ;;
                    *) 
                        sha256sum --check --quiet "$path/$dest_file.sha256"
                        ;;
                esac
            else
                sha256sum --check --quiet "$path/$dest_file.sha256"   
            fi
        else
            sha256sum --check --quiet "$path/$dest_file.sha256"
        fi
    fi

    rm "$path/$dest_file.sha256"
    echo -e "${GREEN}OK${CLEAR}"

    echo -n "    Extracting... "
    gunzip -f "$path/$dest_file"
    echo -e "${GREEN}OK${CLEAR}"
    echo ""
}

download_native "pact_ffi" "windows" "x86_64" "dll"
download_native "libpact_ffi" "linux" "x86_64" "so"
download_native "libpact_ffi" "linux" "aarch64" "so"
download_native "libpact_ffi" "linux" "x86_64-musl" "so"
download_native "libpact_ffi" "linux" "aarch64-musl" "so"
download_native "libpact_ffi" "osx" "x86_64" "dylib"
download_native "libpact_ffi" "osx" "aarch64-apple-darwin" "dylib"

#!/bin/bash
set -e
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${1:-$ROOT/dist}"
DOTNET="${DOTNET:-dotnet}"
command -v "$DOTNET" >/dev/null || DOTNET="$HOME/.dotnet/dotnet"
rm -rf "$OUT"

publish() {
    local runtime="$1" target="$2"
    "$DOTNET" publish "$ROOT/src/CompostaUI" -c Release -r "$runtime" --self-contained true \
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none \
        -o "$OUT/$target"
    cp "$ROOT/src/CompostaUI/Database/composta_postgres.sql" "$OUT/$target/"
    cp "$ROOT/docs/panduan-instalasi.md" "$OUT/$target/PANDUAN.md"
}

publish win-x64 Composta-Windows
mv "$OUT/Composta-Windows/CompostaUI.exe" "$OUT/Composta-Windows/Composta.exe"
sed 's/$/\r/' "$ROOT/scripts/setup-database.bat" > "$OUT/Composta-Windows/setup-database.bat"

publish osx-arm64 Composta-macOS
mv "$OUT/Composta-macOS/CompostaUI" "$OUT/Composta-macOS/Composta"
cp "$ROOT/scripts/setup-database.command" "$OUT/Composta-macOS/"
chmod +x "$OUT/Composta-macOS/Composta" "$OUT/Composta-macOS/setup-database.command"

cd "$OUT"
zip -qr Composta-Windows.zip Composta-Windows
zip -qry Composta-macOS.zip Composta-macOS
echo "Paket siap di $OUT"

#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SQL_FILE="$SCRIPT_DIR/composta_postgres.sql"
[ -f "$SQL_FILE" ] || SQL_FILE="$SCRIPT_DIR/../src/CompostaUI/Database/composta_postgres.sql"

for dir in /opt/homebrew/bin /usr/local/bin /Applications/Postgres.app/Contents/Versions/latest/bin /Library/PostgreSQL/*/bin; do
    [ -x "$dir/psql" ] && PATH="$dir:$PATH"
done
if ! command -v psql >/dev/null; then
    echo "psql tidak ditemukan. Install PostgreSQL terlebih dahulu (misalnya: brew install postgresql@16)."
    exit 1
fi

read -r -s -p "Password user postgres (Enter untuk 'postgres'): " PGPASSWORD
echo
export PGPASSWORD="${PGPASSWORD:-postgres}"
read -r -p "Port PostgreSQL (Enter untuk 5432): " PGPORT
export PGPORT="${PGPORT:-5432}"
export PGOPTIONS="--client-min-messages=warning"

if ! psql -h localhost -U postgres -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='composta'" | grep -q 1; then
    echo "Membuat database composta..."
    psql -h localhost -U postgres -d postgres -c "CREATE DATABASE composta" || exit 1
fi

echo "Mengisi tabel, fungsi, dan data awal..."
psql -h localhost -U postgres -d composta -q -v ON_ERROR_STOP=1 -f "$SQL_FILE" || {
    echo "Setup database gagal. Pastikan PostgreSQL berjalan dan password benar."
    exit 1
}

if [ "$PGPASSWORD" != "postgres" ] || [ "$PGPORT" != "5432" ]; then
    echo "Koneksi berbeda dari bawaan. Jalankan aplikasi dengan:"
    echo "  COMPOSTA_DB=\"Host=localhost;Port=$PGPORT;Username=postgres;Password=$PGPASSWORD;Database=composta\" ./Composta"
fi
echo "Database composta siap."

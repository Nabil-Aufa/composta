@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "SQL_FILE=%SCRIPT_DIR%composta_postgres.sql"
if not exist "%SQL_FILE%" set "SQL_FILE=%SCRIPT_DIR%..\src\CompostaUI\Database\composta_postgres.sql"

where psql >nul 2>nul
if errorlevel 1 (
    for /d %%D in ("C:\Program Files\PostgreSQL\*") do if exist "%%D\bin\psql.exe" set "PATH=%%D\bin;%PATH%"
)
where psql >nul 2>nul
if errorlevel 1 (
    echo psql tidak ditemukan. Install PostgreSQL terlebih dahulu dari https://www.postgresql.org/download/windows/
    pause
    exit /b 1
)

set "PGUSER=postgres"
set "PGOPTIONS=--client-min-messages=warning"
set /p "PGPASSWORD=Password user postgres (Enter untuk 'postgres'): "
if "%PGPASSWORD%"=="" set "PGPASSWORD=postgres"
set /p "PGPORT=Port PostgreSQL (Enter untuk 5432): "
if "%PGPORT%"=="" set "PGPORT=5432"

psql -h localhost -p %PGPORT% -U postgres -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='composta'" | findstr 1 >nul
if errorlevel 1 (
    echo Membuat database composta...
    psql -h localhost -p %PGPORT% -U postgres -d postgres -c "CREATE DATABASE composta"
    if errorlevel 1 goto gagal
)

echo Mengisi tabel, fungsi, dan data awal...
psql -h localhost -p %PGPORT% -U postgres -d composta -q -v ON_ERROR_STOP=1 -f "%SQL_FILE%"
if errorlevel 1 goto gagal

set "NEEDS_ENV="
if not "%PGPASSWORD%"=="postgres" set "NEEDS_ENV=1"
if not "%PGPORT%"=="5432" set "NEEDS_ENV=1"
if defined NEEDS_ENV (
    setx COMPOSTA_DB "Host=localhost;Port=%PGPORT%;Username=postgres;Password=%PGPASSWORD%;Database=composta" >nul
    echo Koneksi database disimpan di variabel COMPOSTA_DB.
)

echo.
echo Database composta siap. Jalankan Composta.exe.
pause
exit /b 0

:gagal
echo.
echo Setup database gagal. Pastikan PostgreSQL berjalan dan password benar.
pause
exit /b 1

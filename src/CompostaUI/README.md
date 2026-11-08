# Composta UI + PostgreSQL (Homework Modul 8)

Prototipe UI Composta (Avalonia, berjalan di macOS/Windows/Linux) yang terhubung ke PostgreSQL memakai Npgsql.
Model, enum, interface, `CarbonCalculatorService`, dan `CompostAdvisorService` diambil langsung dari `src/Composta`.

## Menyiapkan database
1. Buat database `composta` (lewat pgAdmin4 atau `createdb -U postgres composta`).
2. Jalankan `Database/composta_postgres.sql` pada database tersebut (Query Tool pgAdmin4, atau
   `psql -U postgres -d composta -f Database/composta_postgres.sql`).
   Script ini membuat tabel, fungsi `st_select_*`, `st_insert_*`, `st_update_batch`, `st_delete_batch`, dan data awal.

## Connection string
Default: `Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=composta` (di `AppData.cs`).
Jika server berbeda (misalnya PostgreSQL 18 di Windows memakai port 5434), ubah `DefaultConnstring`
atau atur environment variable `COMPOSTA_DB`.

## Menjalankan
Dari folder `src/CompostaUI`: `dotnet run`.

## Struktur penting
- `Data/PostgresRepository.cs`: kelas induk abstrak (encapsulation connection string, method abstrak `MapRow`).
- `Data/CompostBatchRepository.cs`: CRUD batch + `AddWasteItem` (proses Catat Sampah dalam satu transaksi).
- `Data/UserRepository.cs`, `Data/WasteCategoryRepository.cs`: membaca user dan kategori sampah.

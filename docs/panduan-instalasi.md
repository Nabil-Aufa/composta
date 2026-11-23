# Panduan Instalasi dan Penggunaan Composta

Composta adalah aplikasi desktop untuk mencatat sampah organik, memberi saran pengomposan berdasarkan cuaca,
dan menghitung emisi CO2e yang berhasil dicegah.

## 1. Yang perlu disiapkan
- **PostgreSQL** (diuji dengan versi 16 dan 18). Saat instalasi, catat password user `postgres` dan port-nya
  (bawaan `5432`).
- Paket aplikasi sesuai sistem operasi:
  - Windows: `Composta-Windows.zip` (berisi `Composta.exe`, tidak perlu install .NET)
  - macOS (Apple Silicon): `Composta-macOS.zip`
- Opsional: API key gratis dari [OpenWeatherMap](https://openweathermap.org/api) untuk cuaca aktual.

## 2. Instalasi di Windows
1. Ekstrak `Composta-Windows.zip`.
2. Klik dua kali `setup-database.bat`, isi password `postgres` dan port (tekan Enter jika memakai bawaan).
   Skrip membuat database `composta`, tabel, fungsi, dan data awal demo. Jika password atau port berbeda dari bawaan,
   koneksi otomatis disimpan di variabel `COMPOSTA_DB`.
3. Klik dua kali `Composta.exe`. Jika muncul peringatan Windows SmartScreen, pilih **More info → Run anyway**.

## 3. Instalasi di macOS
1. Ekstrak `Composta-macOS.zip`, buka Terminal di folder tersebut.
2. Jalankan `./setup-database.command`, isi password `postgres` dan port.
3. Jalankan `./Composta`. Jika macOS memblokir aplikasi, buka **System Settings → Privacy & Security → Open Anyway**,
   atau jalankan `xattr -dr com.apple.quarantine .` di folder tersebut.

## 4. Mengaktifkan cuaca aktual (opsional)
Tanpa API key aplikasi tetap berjalan memakai data cuaca simulasi yang diberi label "simulasi".

- Windows (Command Prompt): `setx COMPOSTA_WEATHER_KEY "api_key_anda"`, lalu buka ulang aplikasi.
- macOS: `export COMPOSTA_WEATHER_KEY="api_key_anda"` sebelum `./Composta`, atau tambahkan ke `~/.zshrc`.

API key baru dari OpenWeatherMap biasanya aktif beberapa menit sampai 2 jam setelah dibuat.

## 5. Konfigurasi database manual
Jika tidak memakai skrip setup:
1. Buat database `composta` (pgAdmin4 atau `createdb -U postgres composta`).
2. Jalankan `composta_postgres.sql` pada database tersebut (Query Tool pgAdmin4 atau
   `psql -U postgres -d composta -f composta_postgres.sql`).
3. Koneksi bawaan aplikasi: `Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=composta`.
   Jika berbeda, atur variabel `COMPOSTA_DB`, misalnya
   `setx COMPOSTA_DB "Host=localhost;Port=5434;Username=postgres;Password=rahasia;Database=composta"`.

Menjalankan ulang `composta_postgres.sql` akan mengembalikan data ke kondisi awal demo.

## 6. Cara menggunakan
| Langkah | Cara |
|---|---|
| Masuk | Pilih profil **Nabil Aufa Danaputra** (data demo lengkap) atau **Pengguna Demo**, klik **Masuk ke profil**. Profil baru bisa dibuat di **Buat profil baru**. |
| Pengingat jadwal | Setelah masuk, muncul daftar jadwal perawatan yang terlambat atau jatuh tempo hari ini. |
| Dashboard | Menampilkan total CO2e dicegah, jumlah batch berjalan, total sampah, grafik CO2e akumulatif, dan daftar batch. |
| Rekomendasi | Pilih batch untuk melihat saran perawatan berdasarkan rasio brown/green, kelembapan, jadwal, dan cuaca. |
| Batch baru | Klik **+ Batch Baru**. Estimasi waktu matang dihitung dari prakiraan cuaca dan jadwal perawatan dibuat otomatis. |
| Catat sampah | Klik **+ Catat Sampah**, isi nama, kategori, jenis brown/green, berat, dan kelembapan, lalu simpan ke batch Aktif. Plastik dan kaca/logam ditandai tidak dapat dikompos. |
| Jadwal perawatan | Pada batch terpilih, klik **Selesai** di jadwal yang sudah dikerjakan. Jadwal terlambat diberi tanda merah. |
| Cuaca | Klik **Ubah lokasi** untuk mengganti kota, **↻ Muat ulang** untuk mengambil data terbaru. |
| Panen dan hapus | **Panen batch** aktif saat batch berstatus Siap Panen. **Hapus batch** meminta konfirmasi dulu. |
| Profil dan badge | Buka menu nama pengguna → **Lihat profil** untuk ringkasan dan koleksi badge. Badge baru diumumkan lewat pop-up. |
| Ganti pengguna | Menu nama pengguna → **Ganti pengguna**. |

## 7. Masalah umum
| Pesan | Penyebab dan solusi |
|---|---|
| Tidak dapat terhubung ke server PostgreSQL | PostgreSQL belum berjalan atau port salah. Nyalakan service PostgreSQL, cek `COMPOSTA_DB`. |
| Database ... belum dibuat | Jalankan skrip setup database. |
| Skema database ... belum sesuai | Database memakai skema lama. Jalankan ulang `composta_postgres.sql`. |
| permission denied for table users | Skema dijalankan memakai user lain. Jalankan ulang skrip setup sebagai user `postgres`. |
| API key cuaca tidak valid | Key salah atau belum aktif. Aplikasi tetap memakai data simulasi. |

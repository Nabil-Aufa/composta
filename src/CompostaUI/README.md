# Composta UI + PostgreSQL (Homework Modul 8)

Prototipe UI Composta (Avalonia, berjalan di macOS/Windows/Linux) yang terhubung ke PostgreSQL memakai Npgsql.
Model, enum, interface, `CarbonCalculatorService`, dan `CompostAdvisorService` diambil langsung dari `src/Composta`.

## Menyiapkan database
1. Buat database `composta` (lewat pgAdmin4 atau `createdb -U postgres composta`).
2. Jalankan `Database/composta_postgres.sql` pada database tersebut (Query Tool pgAdmin4, atau
   `psql -U postgres -d composta -f Database/composta_postgres.sql`).
   Script ini membuat tabel, fungsi `st_select_*`, `st_insert_*`, `st_update_batch`, `st_delete_batch`, dan data awal.
3. Jalankan ulang script setiap kali isinya berubah setelah `git pull`. Data lama akan diganti dengan data awal.

## Connection string
Default: `Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=composta` (di `AppData.cs`).
Jika server berbeda (misalnya PostgreSQL 18 di Windows memakai port 5434), ubah `DefaultConnstring`
atau atur environment variable `COMPOSTA_DB`.

## Menjalankan
Dari folder `src/CompostaUI`: `dotnet run`.

## Struktur folder
| Folder | Isi |
|---|---|
| `Views/` | Tampilan `.axaml`. Code-behind (`.axaml.cs`) hanya berisi `InitializeComponent()`. |
| `ViewModels/` | Data dan perintah yang ditampilkan View: `MainWindowViewModel`, `AddWasteViewModel`, `BatchItemViewModel`. |
| `Data/` | Repository PostgreSQL. `PostgresRepository<T>` adalah kelas induk abstrak (encapsulation connection string, method abstrak `MapRow`). |
| `Services/` | `DialogService` (pesan, konfirmasi, membuka jendela) dan `OfflineWeatherService`. |
| `Database/` | Script skema, fungsi `st_*`, dan data awal. Satu-satunya sumber skema database. |
| `../Composta/` | Model, enum, interface, `CarbonCalculatorService`, dan `CompostAdvisorService` yang dipakai bersama. |

## Pola MVVM
Aplikasi memakai pola MVVM dengan paket [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/).

- **Model** (`src/Composta/Models`): data dan aturan bisnis, misalnya `CompostBatch.CalculateRatio()`.
- **View** (`Views/*.axaml`): hanya tata letak dan tampilan. Tidak ada logika dan tidak ada akses database.
- **ViewModel** (`ViewModels/*.cs`): menyiapkan data untuk View dan menjalankan perintah. ViewModel tidak mengenal
  kontrol UI (`TextBlock`, `Button`, dll.), sehingga bisa diuji tanpa membuka jendela.

### Menambah data yang tampil di layar
```csharp
public partial class ContohViewModel : ObservableObject
{
    [ObservableProperty]
    private string _judul = string.Empty;
}
```
Atribut `[ObservableProperty]` membuat properti `Judul` secara otomatis. Setiap kali `Judul` diubah, layar ikut berubah.

```xml
<TextBlock Text="{Binding Judul}" />
```

### Menambah tombol
```csharp
[RelayCommand(CanExecute = nameof(BisaSimpan))]
private async Task SimpanAsync()
{
    await _dialogs.ShowMessageAsync("Berhasil", "Data tersimpan.");
}

private bool BisaSimpan()
{
    return !string.IsNullOrWhiteSpace(Judul);
}
```
Method `SimpanAsync` menghasilkan `SimpanCommand`. Tombol otomatis nonaktif ketika `BisaSimpan()` bernilai `false`.
Tambahkan `[NotifyCanExecuteChangedFor(nameof(SimpanCommand))]` pada properti yang memengaruhi `BisaSimpan()`.

```xml
<Button Content="Simpan" Command="{Binding SimpanCommand}" />
```

### Warna berdasarkan kondisi
Gunakan properti `bool` di ViewModel dan style class di `App.axaml`, bukan warna di ViewModel:
```xml
<Border Classes="pill" Classes.ready="{Binding IsReady}" />
```

### Aturan
- Setiap `Window` di `Views/` diberi `x:DataType` dan `x:CompileBindings="True"` agar salah ketik nama properti
  terdeteksi saat build.
- Pesan, konfirmasi, dan membuka jendela lain dilakukan lewat `IDialogService`, bukan langsung dari ViewModel.
- Jendela baru dibuat dari pasangan `Views/NamaWindow.axaml` dan `ViewModels/NamaViewModel.cs`, lalu dibuka lewat
  method baru di `IDialogService`.

## Kartu cuaca (#9)
- Isi nama lokasi (default Yogyakarta), lalu tekan **Refresh**. Lokasi pada judul menunjukkan lokasi
  permintaan terakhir yang selesai; mengedit kotak lokasi saja belum mengambil data baru.
- Saat mengambil cuaca, indikator **Memuat cuaca...** tampil dan input/refresh dinonaktifkan.
- Ikon mengikuti kondisi cerah, berawan, hujan, petir, salju, atau kabut. Kondisi tidak dikenal memakai `?`.
- Waktu pembaruan berasal dari `WeatherData.RetrievedAt`. Data offline diberi label **simulasi**,
  sehingga tidak dianggap sebagai cuaca aktual lokasi yang dimasukkan.
- Setelah refresh selesai, rekomendasi batch terpilih menggunakan objek cuaca yang sama dengan kartu.
- Panel kanan bisa digulir agar rekomendasi tetap dapat dibaca pada jendela kecil.

### Integrasi backend cuaca (#6)
`AppData.WeatherService` membaca environment variable `COMPOSTA_WEATHER_KEY`.
- Jika `COMPOSTA_WEATHER_KEY` terisi, aplikasi memakai `OpenWeatherMapService` (`src/Composta/Services/OpenWeatherMapService.cs`, di-link melalui `CompostaUI.csproj`) untuk mengambil cuaca aktual dari OpenWeatherMap API (default lokasi `Yogyakarta`).
- Jika `COMPOSTA_WEATHER_KEY` kosong/tidak diatur, aplikasi otomatis memakai `OfflineWeatherService` tanpa error.
- Saat menggunakan `OpenWeatherMapService`, pemanggilan `GetCurrentWeather` dijalankan di background dengan batas waktu 10 detik. Jika internet terputus, timeout, atau API key tidak valid, kartu otomatis melakukan fallback ke `OfflineWeatherService` dengan label simulasi.

#### Mengatur API key cuaca (`COMPOSTA_WEATHER_KEY`)
1. Daftar dan buat API key gratis di [OpenWeatherMap](https://openweathermap.org/api).
2. Atur environment variable `COMPOSTA_WEATHER_KEY` sebelum menjalankan aplikasi (jangan menyimpan API key di dalam kode/repository):
  - **Windows (PowerShell):**
    ```powershell
    $env:COMPOSTA_WEATHER_KEY="api_key_anda"
    dotnet run
    ```
  - **Windows (Command Prompt / CMD):**
    ```cmd
    set COMPOSTA_WEATHER_KEY=api_key_anda
    dotnet run
    ```
  - **macOS / Linux:**
    ```bash
    export COMPOSTA_WEATHER_KEY="api_key_anda"
    dotnet run
    ```

### Pengujian kartu cuaca
```bash
dotnet test tests/CompostaUI.Tests/CompostaUI.Tests.csproj --filter FullyQualifiedName~WeatherViewModelTests
```
Jalankan dari root repo. Test ini tidak memerlukan database maupun API key: layanan cuaca pengganti
menguji lokasi, timestamp, ikon, loading, timeout, fallback/retry, dan pembaruan rekomendasi.

Cek manual: jalankan aplikasi, ubah lokasi lalu refresh; pastikan label simulasi tampil. Coba lokasi
kosong untuk melihat validasi. Pilih batch berbeda lalu refresh, dan kecilkan jendela untuk mengecek
scroll panel kanan. Setelah #6 tersedia, ulangi dengan API aktif serta koneksi internet terputus.


## Pilih user, buat profil, dan logout (#11)
Saat aplikasi dibuka, `LoginWindow` muncul lebih dulu. Pilih profil tersimpan lalu tekan **Masuk**,
atau buka tab **Buat profil** untuk memasukkan nama dan email. Tidak ada profil yang dipilih atau
login otomatis oleh layar ini. Jika tabel user kosong, tab Buat profil dibuka otomatis.

Form memakai `UserValidator` dari backend #5 untuk memeriksa nama/email kosong, batas panjang,
dan format email. Email dinormalisasi dengan aturan backend. Email yang sudah terdaftar ditolak.
Saat database gagal dihubungi, pesan kesalahan ditampilkan di layar; tombol **Muat ulang daftar profil**
bisa digunakan setelah koneksi diperbaiki. Form dinonaktifkan selama permintaan berlangsung.

Setelah berhasil masuk, dashboard memakai user aktif melalui `AppData.Login`/`AppData.Load`.
Header menampilkan namanya. **Profil Saya** membuka ringkasan nama, email, tanggal bergabung,
total sampah, estimasi CO2e, serta jumlah seluruh batch (termasuk yang sudah dipanen).
Perhitungan memakai `CarbonCalculatorService` yang sama dengan dashboard.
**Ganti user / Keluar** mengosongkan sesi dan dashboard lama, lalu kembali ke layar pilih profil.
Menutup layar pilih profil menutup aplikasi; tidak ada fallback login ke user pertama pada alur startup UI.

### Struktur dan integrasi
- `LoginViewModel` berisi pilihan user, form profil, validasi, status proses/error, dan command.
- `IUserSessionService`/`UserSessionService` memanggil backend `AppData` di background agar UI tetap responsif.
- `ProfileViewModel` menyiapkan ringkasan khusus user aktif; `DialogService.ShowProfileAsync` membuka jendelanya.
- `App.axaml.cs` mengatur perpindahan jendela setelah `LoginSucceeded` atau `LogoutRequested`.
  Code-behind setiap View tetap hanya `InitializeComponent()`.
- Jika profil sudah tersimpan tetapi login gagal, profil tersebut tetap tersedia/dipilih untuk mencoba
  Masuk kembali tanpa membuat duplikat. Jendela login yang sudah ditutup tidak membuka dashboard dari hasil terlambat.
- Alur ini merupakan pemilihan profil lokal sesuai #11; tidak menambahkan autentikasi dengan kata sandi.
- Tidak ada perubahan skema SQL pada issue ini. Backend user/skema dari PR #20 harus sudah tersedia.

### Pengujian login dan profil
Dari root repo:
```bash
dotnet test tests/CompostaUI.Tests/CompostaUI.Tests.csproj --filter "FullyQualifiedName~LoginViewModelTests|FullyQualifiedName~ProfileViewModelTests"
```
Tes tersebut tidak memerlukan database. Dua tes pada `LoginIntegrationTests` memerlukan database
`COMPOSTA_TEST_DB`, mengikuti pengamanan fixture test yang sudah ada. Tes integrasi memeriksa
registrasi dengan PostgreSQL serta pergantian user sampai dashboard dan profil; jalankan dengan:
```bash
dotnet test tests/CompostaUI.Tests/CompostaUI.Tests.csproj --filter FullyQualifiedName~LoginIntegrationTests
```

Cek manual setelah database siap: buka aplikasi, pilih Nabil, buka Profil Saya, lalu Ganti user / Keluar
untuk memilih user kedua dan pastikan batch/ringkasan berubah. Buat profil baru dan pastikan dashboard
berisi nol batch. Uji nama/email kosong, format email salah, email duplikat, serta muat ulang saat database mati.

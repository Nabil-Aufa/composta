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

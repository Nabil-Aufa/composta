# Aturan Target Rasio Brown/Green Berbasis Cuaca

`CompostAdvisorService.GetTargetRatio(WeatherData?)` menyesuaikan rentang target rasio bahan *brown* (karbon) terhadap *green* (nitrogen) berdasarkan kondisi cuaca saat ini, lalu menampilkan target tersebut pada pesan rekomendasi kompos.

## Aturan Penentuan Target Rasio

| Kondisi Cuaca | Kriteria | Target Rasio (Brown : Green) | Alasan |
|---|---|---|---|
| Hujan atau sangat lembap | `weather.IsRainy()` atau kelembapan > 85% | **2,5 – 3,5** | Menambah porsi bahan *brown* untuk menyerap kelebihan air dan menjaga aerasi tumpukan. |
| Panas dan kering | Suhu > 32 °C dan kelembapan < 50% | **1,5 – 2,5** | Menambah porsi bahan *green* (kadar air lebih tinggi) agar tumpukan tidak cepat mengering. |
| Normal atau cuaca tidak tersedia | Kondisi lainnya atau data cuaca `null` | **2,0 – 3,0** | Batas standar pengomposan seimbang. |

> **Catatan prioritas pengecekan:** Kondisi hujan/lembap dicek terlebih dahulu sebelum kondisi panas/kering. Jika layanan cuaca gagal dihubungi atau mengembalikan `null`, sistem otomatis kembali memakai batas standar **2,0 – 3,0**.

## Contoh Perhitungan & Rekomendasi

Misalkan sebuah batch memiliki **4,4 kg bahan brown** dan **2,0 kg bahan green**, sehingga rasio aktualnya adalah:

$$\text{Rasio} = \frac{4,4}{2,0} = 2,2$$

1. **Saat cuaca normal** (suhu 28 °C, kelembapan 65%, tidak hujan):
   - Target rasio yang berlaku adalah **2,0 – 3,0**.
   - Karena `2,2` berada di dalam rentang `2,0–3,0`, rasio dianggap **ideal** dan pesan rekomendasi mencatat `(target rasio 2-3)`.
2. **Saat cuaca hujan atau kelembapan > 85%** (misalnya hujan ringan dengan kelembapan 80%):
   - Target rasio naik menjadi **2,5 – 3,5**.
   - Karena `2,2 < 2,5`, sistem menyarankan penambahan bahan *brown* dengan pesan:  
     *"Rasio brown/green 2,2 terlalu rendah (target 2,5-3,5), tambahkan bahan brown seperti daun kering atau kardus."*
3. **Saat cuaca panas dan kering** (misalnya suhu 35 °C, kelembapan 40%):
   - Target rasio turun menjadi **1,5 – 2,5**.
   - Karena `2,2` berada di dalam rentang `1,5–2,5`, rasio dianggap **ideal** untuk kondisi panas kering (tidak muncul peringatan rasio).
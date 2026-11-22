# Estimasi Waktu Matang Kompos Dinamis

## Latar Belakang
Proses pengomposan dipengaruhi langsung oleh suhu lingkungan dan kelembapan udara karena kedua faktor tersebut menentukan aktivitas mikroorganisme pengurai. Oleh karena itu, Composta menghitung perkiraan tanggal matang kompos (`estimated_maturity_date`) secara dinamis menggunakan rata-rata prakiraan cuaca 5 hari ke depan, menggantikan durasi statis 60 hari.

---

## Rumus Perhitungan

$$\text{Estimasi Hari} = \max\left(14,\ 60 + \Delta_{\text{suhu}} + \Delta_{\text{kelembapan}}\right)$$

### Komponen Rumus
1. **Durasi Dasar (`DefaultMaturityDays`)**: `60 hari`.
   * Nilai ini juga digunakan sebagai *fallback* apabila layanan cuaca offline atau data prakiraan cuaca tidak tersedia.
2. **Penyesuaian Suhu Rata-rata ($\Delta_{\text{suhu}}$)**:
   * **Optimal (`25°C – 35°C`)**: **`-8 hari`** — aktivitas mikroba termofilik/mesofilik berada pada puncaknya sehingga penguraian lebih cepat.
   * **Terlalu Panas (`> 35°C`)**: **`+5 hari`** — penguapan air berlebihan menghambat aktivitas mikroba.
   * **Sejuk (`15°C – < 25°C`)**: **`+4 hari`** — laju reaksi biologis sedikit melambat.
   * **Dingin (`< 15°C`)**: **`+12 hari`** — aktivitas mikroba melambat signifikan.
3. **Penyesuaian Kelembapan Rata-rata ($\Delta_{\text{kelembapan}}$)**:
   * **Ideal (`45% – 85%`)**: **`0 hari`**.
   * **Ekstrem (`< 45%` terlalu kering atau `> 85%` terlalu lembap)**: **`+6 hari`** — kondisi terlalu kering menghambat dekomposisi, sedangkan terlalu basah mengurangi aerasi.
4. **Batas Minimum (`MinimumMaturityDays`)**: **`14 hari`**.

---

## Contoh Perhitungan

### Contoh 1: Cuaca Hangat & Kelembapan Ideal (Yogyakarta Cerah/Berawan)
* **Prakiraan 5 hari**: rata-rata suhu **`29°C`**, rata-rata kelembapan **`68%`**.
* **Penyesuaian suhu**: `29°C` berada di rentang `25°C – 35°C` $\rightarrow$ **`-8 hari`**.
* **Penyesuaian kelembapan**: `68%` berada di rentang `45% – 85%` $\rightarrow$ **`0 hari`**.
* **Hasil**:
  $$\max(14,\ 60 + (-8) + 0) = \mathbf{52\ \text{hari}}$$
* Jika bahan organik terakhir masuk pada **1 November 2026**, maka `estimated_maturity_date` bergeser menjadi **23 Desember 2026**.

### Contoh 2: Cuaca Dingin & Sangat Lembap (Musim Hujan Dataran Tinggi)
* **Prakiraan 5 hari**: rata-rata suhu **`13°C`**, rata-rata kelembapan **`90%`**.
* **Penyesuaian suhu**: `13°C < 15°C` $\rightarrow$ **`+12 hari`**.
* **Penyesuaian kelembapan**: `90% > 85%` $\rightarrow$ **`+6 hari`**.
* **Hasil**:
  $$\max(14,\ 60 + 12 + 6) = \mathbf{78\ \text{hari}}$$

### Contoh 3: Cuaca Sangat Panas & Kering
* **Prakiraan 5 hari**: rata-rata suhu **`37°C`**, rata-rata kelembapan **`40%`**.
* **Penyesuaian suhu**: `37°C > 35°C` $\rightarrow$ **`+5 hari`**.
* **Penyesuaian kelembapan**: `40% < 45%` $\rightarrow$ **`+6 hari`**.
* **Hasil**:
  $$\max(14,\ 60 + 5 + 6) = \mathbf{71\ \text{hari}}$$

### Contoh 4: Offline / Prakiraan Cuaca Tidak Tersedia
* Apabila aplikasi berjalan dalam mode offline atau API cuaca gagal merespons, sistem menggunakan durasi standar **`60 hari`**.

---

## Aturan Sinkronisasi Jadwal & Status Batch
* **Titik Awal Hitung**: Tanggal matang dihitung dari tanggal masuk sampah organik (`CompostableWaste`) terbaru di dalam batch, atau dari `start_date` jika batch masih kosong.
* **Pergeseran Jadwal Panen**: Setiap kali `estimated_maturity_date` diperbarui, jadwal perawatan bertipe `Harvest` yang belum selesai (`is_completed = FALSE`) otomatis ikut bergeser secara atomik di database.
* **Integritas Status Batch**: Status batch hanya bergerak maju (`Active` → `Curing` → `Ready` → `Archived`) dan tidak pernah mundur meskipun estimasi tanggal matang bertambah panjang. Batch yang sudah berstatus `Ready` atau `Archived` tidak lagi dihitung ulang.
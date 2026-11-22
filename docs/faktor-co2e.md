# Faktor CO2e Composta

Composta menghitung **emisi yang dicegah** ketika sampah organik dikompos, bukan dibuang ke TPA (tempat pemrosesan akhir).
Angkanya berasal dari **EPA Waste Reduction Model (WARM) versi 16** (U.S. Environmental Protection Agency, Desember 2023).

## Rumus

```
emisi dicegah (kg CO2e) = berat sampah (kg) × faktor kategori (kg CO2e/kg)

faktor kategori = (emisi bersih TPA − emisi bersih pengomposan) × 1,1023
```

- WARM memakai satuan **MTCO2E per short ton**. 1 short ton = 907,185 kg, sehingga
  1 MTCO2E/short ton = 1.000 kg ÷ 907,185 kg = **1,1023 kg CO2e/kg**.
- Nilai negatif di WARM berarti pengurangan emisi atau penyimpanan karbon.
- Faktor disimpan di kolom `waste_categories.co2e_factor_per_kg` dan dipakai oleh `CarbonCalculatorService`
  serta fungsi `st_select_co2e_timeline`.

## Faktor per kategori

| Kategori | Baris WARM yang dipakai | TPA | Pengomposan | Selisih | Faktor (kg CO2e/kg) |
|---|---|---|---|---|---|
| Sisa Sayur & Buah | Food Waste → Fruits and Vegetables | 0,23 | (0,15) | 0,38 | **0,419** |
| Ampas Kopi & Teh | Food Waste (umum) | 0,50 | (0,15) | 0,65 | **0,717** |
| Kardus & Kertas | Corrugated Containers (TPA); pengomposan tidak dimodelkan WARM, dianggap 0 | 0,18 | 0 | 0,18 | **0,198** |
| Daun Kering & Ranting | Yard Trimmings → Leaves / Branches | (0,53) / (0,54) | (0,05) | negatif | **0,000** |
| Tanpa kategori | Food Waste (umum), nilai bawaan `CarbonCalculatorService` | 0,50 | (0,15) | 0,65 | **0,717** |
| Plastik, Kaca & Logam | Tidak dapat dikompos, tidak dihitung | – | – | – | **0,000** |

Satuan kolom TPA, Pengomposan, dan Selisih: MTCO2E/short ton. Angka dalam kurung bernilai negatif.

## Asumsi dan keterbatasan

1. **Pembanding adalah TPA.** Faktor menghitung selisih terhadap skenario sampah dibuang ke TPA rata-rata nasional
   Amerika Serikat versi WARM (sebagian TPA memiliki penangkapan gas metana). Di Indonesia banyak TPA masih
   *open dumping* tanpa penangkapan gas, sehingga emisi yang benar-benar dicegah kemungkinan **lebih besar**.
   Angka Composta sengaja dibuat konservatif.
2. **Daun kering dan ranting bernilai 0.** Menurut WARM, daun dan ranting yang ditimbun di TPA justru menyimpan
   karbon karena lignin sulit terurai, sehingga pengomposan tidak mengurangi emisi dibanding TPA. Composta tidak
   mengklaim dampak negatif, jadi faktornya dibuat 0. Bahan brown ini tetap penting untuk menjaga rasio brown/green.
3. **Kardus dan kertas.** WARM tidak menyediakan faktor pengomposan untuk produk kertas. Emisi pengomposan kardus
   dianggap 0, sehingga faktornya hanya berasal dari emisi TPA *Corrugated Containers*.
4. **Ampas kopi dan teh** tidak punya baris khusus di WARM, sehingga memakai *Food Waste* umum.
5. **Emisi hulu tidak dihitung.** Nilai *source reduction* (mencegah sampah makanan sejak awal) jauh lebih besar,
   tetapi tidak relevan karena Composta mengolah sampah yang sudah ada.

## Sumber

- U.S. EPA. *Documentation for Greenhouse Gas Emission and Energy Factors Used in the Waste Reduction Model (WARM):
  Organic Materials Chapters*, Version 16, Desember 2023. Exhibit 1-10 (*Food Waste*) dan Exhibit 2-4 (*Yard Trimmings*).
  https://www.epa.gov/system/files/documents/2023-12/warm_organic_materials_v16_dec.pdf
- U.S. EPA. *Documentation for Greenhouse Gas Emission and Energy Factors Used in the Waste Reduction Model (WARM):
  Containers, Packaging, and Non-Durable Goods Materials Chapters*, Version 16, Desember 2023. Exhibit 3-7 (*Paper Products*).
  https://www.epa.gov/system/files/documents/2023-12/warm_containers_packaging_and_non-durable_goods_materials_v16_dec.pdf
- Daftar seluruh bab dokumentasi WARM:
  https://www.epa.gov/warm/documentation-chapters-greenhouse-gas-emission-energy-and-economic-factors-used-waste-reduction

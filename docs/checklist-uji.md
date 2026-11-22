# Checklist Uji Manual Sebelum Demo

Jalankan checklist ini pada database yang baru di-reset dengan `src/CompostaUI/Database/composta_postgres.sql`.
Centang setiap baris yang hasilnya sesuai. Baris bertanda *(menyusul)* diisi setelah issue terkait selesai.

## Persiapan
- [ ] `dotnet test tests/Composta.Tests` lolos semua
- [ ] `dotnet test tests/CompostaUI.Tests` lolos semua dengan `COMPOSTA_TEST_DB` aktif (tidak ada yang skipped)
- [ ] Database `composta` di-reset dengan `composta_postgres.sql`
- [ ] Aplikasi dibuka dengan `dotnet run` dari `src/CompostaUI`

## Dashboard
| No | Langkah | Hasil yang diharapkan | OK |
|---|---|---|---|
| 1 | Buka aplikasi | Sapaan "Halo, Nabil Aufa Danaputra!", status "Terhubung ke PostgreSQL", 4 batch tampil | [ ] |
| 2 | Lihat kartu ringkasan | CO2e dicegah 6,14 kg, batch berjalan 3, total sampah 30,5 kg | [ ] |
| 3 | Lihat daftar batch | Keempat status tampil dengan warna berbeda: Sudah Dipanen, Siap Panen, Pematangan, Aktif | [ ] |
| 4 | Lihat batch terpilih saat dibuka | Batch 2 (Siap Panen) terpilih, rekomendasi "Kompos sudah matang dan siap dipanen", prioritas tinggi | [ ] |
| 5 | Pilih Batch 3 (Pematangan) | Rekomendasi menyebut jadwal perawatan yang terlewat | [ ] |
| 6 | Pilih Batch 4 (Aktif) | Rekomendasi menyebut rasio brown/green terlalu rendah, tombol Panen nonaktif | [ ] |
| 7 | Pilih Batch 1 (Sudah Dipanen) | Rekomendasi "Kompos dari batch ini sudah dipanen.", tombol Panen dan Hapus nonaktif | [ ] |

## Batch
| No | Langkah | Hasil yang diharapkan | OK |
|---|---|---|---|
| 8 | Klik "+ Batch Baru", jawab "Ya, lanjutkan" | Muncul konfirmasi dengan tombol hijau, lalu pesan "Batch 5 berhasil ditambahkan", Batch 5 berstatus Aktif dan terpilih | [ ] |
| 9 | Pilih Batch 2, klik "Panen" | Pesan "Batch 2 berhasil dipanen", status menjadi Sudah Dipanen, batch berjalan berkurang 1 | [ ] |
| 10 | Pilih Batch 5, klik "Hapus", jawab "Batal" | Batch tidak terhapus | [ ] |
| 11 | Pilih Batch 5, klik "Hapus", jawab "Ya, lanjutkan" | Pesan berhasil dihapus, Batch 5 hilang dari daftar | [ ] |

## Catat Sampah
| No | Langkah | Hasil yang diharapkan | OK |
|---|---|---|---|
| 12 | Klik "+ Catat Sampah" | Hanya batch berstatus Aktif yang bisa dipilih | [ ] |
| 13 | Klik "Simpan" dengan nama kosong | Pesan "Nama sampah wajib diisi.", jendela tidak tertutup | [ ] |
| 14 | Kosongkan kolom berat lalu simpan | Pesan "Berat harus lebih dari 0 kg ..." | [ ] |
| 15 | Buka daftar kategori, lalu pilih Plastik | Plastik dan Kaca & Logam bertanda "(tidak dapat dikompos)"; setelah dipilih, pilihan brown/green dan kelembapan disembunyikan, muncul keterangan bahwa sampah ini tidak perlu disimpan, pratinjau kuning "Tidak dapat dikomposkan" berisi saran ke bank sampah | [ ] |
| 16 | Lihat tombol "Simpan catatan" saat kategori Plastik dipilih | Tombol nonaktif; kembali ke kategori organik membuat tombol aktif lagi | [ ] |
| 17 | Geser kelembapan ke bawah 40% | Pratinjau menyarankan menambah sedikit air | [ ] |
| 18 | Isi "Daun mangga", Daun Kering & Ranting, Brown, 3 kg, simpan ke Batch 4 | Jendela tertutup, total sampah bertambah 3 kg, rasio Batch 4 berubah | [ ] |
| 19 | Klik "Batal" | Jendela tertutup tanpa menyimpan | [ ] |

## Kondisi Gagal
| No | Langkah | Hasil yang diharapkan | OK |
|---|---|---|---|
| 20 | Hentikan server PostgreSQL lalu buka aplikasi | Pesan "Tidak dapat terhubung ke server PostgreSQL", status merah, semua tombol nonaktif, aplikasi tidak crash | [ ] |
| 21 | Atur `COMPOSTA_DB` ke database yang belum dibuat | Pesan "Database ... belum dibuat" | [ ] |
| 22 | Jalankan aplikasi pada database dengan skema lama | Pesan "Skema database ... belum sesuai dengan versi aplikasi ini" | [ ] |
| 23 | API cuaca mati atau tanpa API key *(menyusul #6)* | Aplikasi tetap berjalan dengan data cuaca offline | [ ] |

## Fitur Menyusul
| No | Fitur | Issue | OK |
|---|---|---|---|
| 24 | Window login, pilih user, buat user baru, ganti user | #11 | [ ] |
| 25 | Daftar jadwal perawatan dan tombol selesai | #7, #10 | [ ] |
| 26 | Cuaca dari API, tombol refresh, pilihan lokasi | #6, #9 | [ ] |
| 27 | Estimasi waktu matang berbasis cuaca | #16 | [ ] |
| 28 | Grafik CO2e akumulatif | #17, #18 | [ ] |
| 29 | Badge dan notifikasi badge baru | #8, #12 | [ ] |

## Video Demo
- [ ] Rekam alur: buka aplikasi, lihat rekomendasi tiap batch, buat batch baru, catat sampah, panen, hapus, dan contoh pesan error
- [ ] Unggah video dan cantumkan link-nya di README bagian "Video Demo"

# Checklist Uji Manual Sebelum Demo

Jalankan checklist ini pada database yang baru di-reset dengan `src/CompostaUI/Database/composta_postgres.sql`.
Centang setiap baris yang hasilnya sesuai.

## Persiapan
- [ ] `dotnet test tests/Composta.Tests` lolos semua
- [ ] `dotnet test tests/CompostaUI.Tests` lolos semua dengan `COMPOSTA_TEST_DB` aktif (tidak ada yang skipped)
- [ ] Database `composta` di-reset dengan `composta_postgres.sql`
- [ ] Aplikasi dibuka dengan `dotnet run` dari `src/CompostaUI`

## Login dan Dashboard
| No | Langkah | Hasil yang diharapkan | OK |
|---|---|---|---|
| 1 | Buka aplikasi, pilih "Nabil Aufa Danaputra", klik "Masuk ke profil" | Jendela "Composta - Pilih Pengguna" tampil lebih dulu; setelah masuk muncul pengingat jadwal perawatan yang terlambat, lalu dashboard dengan sapaan "Halo, Nabil Aufa Danaputra!", status "Terhubung ke PostgreSQL", 4 batch tampil | [ ] |
| 2 | Lihat kartu ringkasan | CO2e dicegah 6,13 kg, batch berjalan 3, total sampah 30,5 kg | [ ] |
| 3 | Lihat daftar batch | Keempat status tampil dengan warna berbeda: Sudah Dipanen, Siap Panen, Pematangan, Aktif | [ ] |
| 4 | Lihat batch terpilih saat dibuka | Batch 2 (Siap Panen) terpilih, rekomendasi "Kompos sudah matang dan siap dipanen", prioritas tinggi | [ ] |
| 5 | Pilih Batch 3 (Pematangan) | Rekomendasi menyebut jadwal perawatan yang terlewat | [ ] |
| 6 | Pilih Batch 4 (Aktif) | Rekomendasi menyebut rasio brown/green terlalu rendah, tombol Panen nonaktif | [ ] |
| 7 | Pilih Batch 1 (Sudah Dipanen) | Rekomendasi "Kompos dari batch ini sudah dipanen.", tombol Panen dan Hapus nonaktif | [ ] |

## Batch
| No | Langkah | Hasil yang diharapkan | OK |
|---|---|---|---|
| 8 | Klik "+ Batch Baru", jawab "Ya, lanjutkan" | Muncul konfirmasi dengan tombol hijau, lalu pesan "Batch 5 berhasil ditambahkan", Batch 5 berstatus Aktif dan terpilih | [ ] |
| 9 | Pilih Batch 2, klik "Panen batch" | Pesan "Batch 2 berhasil dipanen", status menjadi Sudah Dipanen, batch berjalan berkurang 1 | [ ] |
| 10 | Pilih Batch 5, klik "Hapus batch", jawab "Batal" | Batch tidak terhapus | [ ] |
| 11 | Pilih Batch 5, klik "Hapus batch", jawab "Ya, lanjutkan" | Pesan berhasil dihapus, Batch 5 hilang dari daftar | [ ] |

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
| 20 | Hentikan server PostgreSQL lalu buka aplikasi | Jendela pilih pengguna menampilkan pesan "Tidak dapat terhubung ke server PostgreSQL", daftar pengguna kosong, aplikasi tidak crash; setelah server dinyalakan, "↻ Muat ulang" menampilkan daftar pengguna lagi | [ ] |
| 21 | Atur `COMPOSTA_DB` ke database yang belum dibuat | Pesan "Database ... belum dibuat" | [ ] |
| 22 | Jalankan aplikasi pada database dengan skema lama | Pesan "Skema database ... belum sesuai dengan versi aplikasi ini" | [ ] |
| 23 | Jalankan tanpa `COMPOSTA_WEATHER_KEY`, lalu dengan API key salah atau internet dimatikan, tekan "↻ Muat ulang" pada kartu cuaca | Aplikasi tetap berjalan, kartu cuaca berlabel "simulasi", pesan tidak menampilkan API key, rekomendasi tetap muncul | [ ] |

## Fitur Lanjutan
| No | Langkah | Hasil yang diharapkan | OK |
|---|---|---|---|
| 24 | Buka menu nama pengguna, pilih "Ganti pengguna", lalu buat profil baru di tab "Buat profil baru" | Kembali ke jendela pilih pengguna; profil baru tersimpan dan bisa langsung dipakai masuk dengan dashboard kosong | [ ] |
| 25 | Masuk lagi sebagai Nabil, pilih Batch 3, klik "Selesai" pada jadwal yang terlambat | Jadwal tersebut dan jadwal sejenis yang lebih awal ditandai selesai, label "Terlambat" hilang | [ ] |
| 26 | Klik "Ubah lokasi", isi "Bandung", klik "Muat cuaca" | Judul kartu menjadi "Cuaca · Bandung"; dengan `COMPOSTA_WEATHER_KEY` valid data berlabel waktu pembaruan, tanpa key berlabel simulasi | [ ] |
| 27 | Klik "+ Batch Baru" | Status menampilkan "Menghitung estimasi panen...", menu "Ganti pengguna" nonaktif sampai proses selesai, konfirmasi menyebut jumlah hari estimasi | [ ] |
| 28 | Lihat grafik CO2e di dashboard, lalu buka "Lihat profil" | Grafik akumulatif naik sesuai tanggal sampah; total di profil sama dengan kartu CO2e dicegah (6,13 kg) | [ ] |
| 29 | Masuk sebagai "Pengguna Demo", buka "Lihat profil" | Bagian "Koleksi badge" menampilkan "1 dari 4 badge diperoleh"; "Batch Pertama" berwarna dengan tanggal diperoleh, tiga badge lain abu-abu dengan progres (misalnya "Belum diperoleh • 4 / 10 kg") | [ ] |
| 30 | Tutup profil, catat sampah 6 kg "Daun Kering & Ranting", lalu buka "Lihat profil" lagi | Muncul pesan "Badge Baru Terbuka!" berisi "10 kg Sampah"; di profil badge itu berwarna dan ringkasan menjadi "2 dari 4 badge diperoleh" | [ ] |

## Video Demo
- [ ] Rekam alur: buka aplikasi, lihat rekomendasi tiap batch, buat batch baru, catat sampah, panen, hapus, dan contoh pesan error
- [ ] Unggah video dan cantumkan link-nya di README bagian "Video Demo"

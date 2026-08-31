Kelompok Composta (15)  

Ketua Kelompok: Nayla Thalita - 24/535820/TK/59467  

Anggota 1: Nabil Aufa Danaputra - 24/535223/TK/59357  

Anggota 2: Gilbert S. H. Nainggolan - 24/54341/TK/60447  




Project Junior Project TI  
Departemen Teknologi Elektro dan Teknologi Informasi, Fakultas Teknik, Universitas Gadjah Mada  




Proposal Proyek: EcoTani - Smart Waste-to-Compost Advisor

Proyek ini mengusung tema Climate Action dengan fokus pada masalah spesifik, yaitu sampah organik rumah tangga yang berkontribusi besar terhadap emisi gas metana jika berakhir di TPA tanpa diolah, mengingat data IPCC/EPA menunjukkan sampah organik menyumbang sekitar 8% emisi global. EcoTani hadir sebagai aplikasi desktop berbasis C# WPF yang membantu petani urban maupun rumah tangga mengelola proses composting secara optimal, dengan rekomendasi berbasis data cuaca real-time, pelacakan progres, dan dashboard dampak lingkungan yang terukur.





Aplikasi ini memiliki beberapa fitur utama. Pertama, input dan klasifikasi sampah, yaitu pengguna dapat mengkategorikan sampah organik seperti sisa sayur, buah, atau daun. Kedua, rekomendasi cerdas berbasis cuaca, di mana rasio bahan brown/green dan estimasi waktu matang kompos disesuaikan dengan suhu dan kelembapan lokal. Ketiga, compost health tracker yang memberikan reminder jadwal mengaduk dan menyiram kompos beserta timeline progresnya. Keempat, impact dashboard yang menghitung estimasi CO2e yang berhasil dicegah dan menampilkannya dalam grafik akumulatif. Sebagai fitur tambahan opsional, aplikasi juga dapat dilengkapi gamifikasi berupa badge dan leaderboard lokal untuk mendorong engagement pengguna.





Untuk integrasi API pihak ketiga, aplikasi akan menggunakan OpenWeatherMap API sebagai sumber data cuaca utama, yang menyediakan free tier hingga 1.000 calls per hari mencakup data cuaca saat ini dan forecast, termasuk suhu dan kelembapan yang menjadi dasar perhitungan rekomendasi kompos. Layanan cuaca ini akan diimplementasikan melalui interface IWeatherService sehingga arsitekturnya bersifat decoupled dan providernya dapat diganti di kemudian hari, misalnya ke Azure Maps Weather apabila tersedia Azure credit, tanpa perlu mengubah logic inti aplikasi. Ini sekaligus menunjukkan penerapan prinsip dependency inversion dalam desain sistem. Untuk penyimpanan data, aplikasi akan menggunakan SQLite lokal guna menyimpan riwayat input sampah, jadwal perawatan, dan data kompos per pengguna.





Dari sisi rancangan teknis, aplikasi akan menerapkan konsep OOP secara jelas, seperti kelas WasteItem dan WasteCategory dengan pola inheritance melalui turunan CompostableWaste dan NonCompostableWaste, serta interface seperti IWeatherService dan ICarbonCalculator untuk abstraksi layanan eksternal dan kalkulasi. Pola arsitektur MVVM akan digunakan untuk memisahkan logic dari tampilan UI WPF.





Pembagian tanggung jawab tim terdiri atas tiga peran. Software architect bertanggung jawab menyusun class diagram, struktur database, dan dokumentasi arsitektur di GitHub. Backend developer menangani logic kalkulasi kompos dan CO2e, integrasi API cuaca, serta data access layer. Frontend developer bertanggung jawab atas tampilan UI WPF, dashboard visualisasi, dan alur UX untuk fitur reminder.

Terkait rencana dokumentasi dan demo, proyek akan dikelola melalui repository GitHub dengan commit history yang rutin per sprint. README akan dilengkapi dengan panduan instalasi, screenshot, video demo, dan akun demo apabila diperlukan. Pengujian edge case, seperti input kosong atau kondisi API offline, juga akan dilakukan untuk memastikan demo dapat berjalan tanpa error.





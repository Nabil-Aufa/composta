using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Composta.Enums;
using Composta.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Composta
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Opened += MainWindow_Opened;
        }

        private async void MainWindow_Opened(object? sender, EventArgs e)
        {
            await LoadDashboard(null);
        }

        // READ: ambil data dari PostgreSQL lalu tampilkan di dashboard
        private async Task LoadDashboard(string? selectedBatchId)
        {
            try
            {
                AppData.Load();
            }
            catch (Exception ex)
            {
                lblDbStatus.Text = "● Tidak terhubung ke PostgreSQL";
                lblDbStatus.Foreground = Brush.Parse("#FFCDD2");
                brdDbStatus.Background = Brush.Parse("#8E1B1B");
                SetBatchButtonsEnabled(false);
                btnNewBatch.IsEnabled = false;
                btnAddWaste.IsEnabled = false;
                await ShowMessage("Database Gagal",
                    "Gagal terhubung ke database PostgreSQL (" + AppData.DatabaseInfo + ").\n\n" + ex.Message);
                return;
            }

            lblDbStatus.Text = "● Terhubung ke PostgreSQL  •  " + AppData.DatabaseInfo;
            lblDbStatus.Foreground = Brush.Parse("#C8E6C9");
            brdDbStatus.Background = Brush.Parse("#1B5E20");
            btnNewBatch.IsEnabled = true;
            btnAddWaste.IsEnabled = true;

            User user = AppData.CurrentUser;
            lblGreeting.Text = "Halo, " + user.Name + "! Ayo ubah sampah organik jadi kompos.";

            double totalCo2e = user.Batches.Sum(b => AppData.CarbonCalculator.CalculateBatchImpact(b));
            lblTotalCo2e.Text = totalCo2e.ToString("0.##") + " kg";
            lblActiveBatches.Text = user.GetActiveBatches().Count.ToString();
            lblTotalWeight.Text = user.Batches.Sum(b => b.GetTotalWeight()).ToString("0.##") + " kg";

            WeatherData weather = AppData.WeatherService.GetCurrentWeather(AppData.Location);
            lblWeatherLocation.Text = "Cuaca hari ini • " + AppData.Location;
            lblWeatherTemp.Text = weather.Temperature.ToString("0.#") + "°C";
            lblWeatherCondition.Text = weather.Condition;
            lblWeatherDetail.Text = "Kelembapan " + weather.Humidity.ToString("0") + "%  •  Curah hujan "
                + weather.RainfallMm.ToString("0.#") + " mm";

            List<BatchRow> rows = user.Batches.Select(b => new BatchRow(b)).ToList();
            lstBatches.ItemsSource = rows;
            lblEmpty.IsVisible = rows.Count == 0;

            BatchRow? selectedRow = rows.FirstOrDefault(r => r.Batch.Id == selectedBatchId) ?? rows.FirstOrDefault();
            lstBatches.SelectedItem = selectedRow;
            if (selectedRow == null)
            {
                ShowEmptyRecommendation();
            }
        }

        private CompostBatch? SelectedBatch
        {
            get { return (lstBatches.SelectedItem as BatchRow)?.Batch; }
        }

        private void ShowRecommendation(CompostBatch batch)
        {
            if (batch.Status == BatchStatus.Archived)
            {
                lblRecommendationTitle.Text = "Rekomendasi " + AppData.GetBatchName(batch);
                lblRecommendation.Text = "Kompos dari batch ini sudah dipanen.";
                SetPriority("Low");
                return;
            }

            WeatherData weather = AppData.WeatherService.GetCurrentWeather(AppData.Location);
            CompostRecommendation recommendation = AppData.Advisor.GenerateRecommendation(batch, weather);

            lblRecommendationTitle.Text = "Rekomendasi " + AppData.GetBatchName(batch);
            lblRecommendation.Text = recommendation.Message;
            SetPriority(recommendation.Priority);
        }

        private void ShowEmptyRecommendation()
        {
            lblRecommendationTitle.Text = "Rekomendasi";
            lblRecommendation.Text = "Belum ada batch yang dipilih.";
            SetPriority("Low");
            SetBatchButtonsEnabled(false);
        }

        private void SetPriority(string priority)
        {
            switch (priority)
            {
                case "High":
                    lblPriority.Text = "Prioritas Tinggi";
                    lblPriority.Foreground = Brush.Parse("#B71C1C");
                    brdPriority.Background = Brush.Parse("#FFEBEE");
                    break;
                case "Medium":
                    lblPriority.Text = "Prioritas Sedang";
                    lblPriority.Foreground = Brush.Parse("#8D5A00");
                    brdPriority.Background = Brush.Parse("#FFF4D6");
                    break;
                default:
                    lblPriority.Text = "Prioritas Rendah";
                    lblPriority.Foreground = Brush.Parse("#1B5E20");
                    brdPriority.Background = Brush.Parse("#E8F5E9");
                    break;
            }
        }

        private void SetBatchButtonsEnabled(bool enabled)
        {
            btnDeleteBatch.IsEnabled = enabled;
            btnHarvest.IsEnabled = enabled && SelectedBatch?.Status == BatchStatus.Ready;
        }

        private void lstBatches_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            CompostBatch? batch = SelectedBatch;
            if (batch == null)
            {
                ShowEmptyRecommendation();
                return;
            }

            ShowRecommendation(batch);
            SetBatchButtonsEnabled(true);
        }

        private async void btnAddWaste_Click(object? sender, RoutedEventArgs e)
        {
            AddWasteWindow addWasteWindow = new AddWasteWindow(SelectedBatch);

            bool saved = await addWasteWindow.ShowDialog<bool>(this);
            if (saved)
            {
                await LoadDashboard(addWasteWindow.SavedBatchId);
            }
        }

        // CREATE: batch kompos baru dengan estimasi matang 60 hari
        private async void btnNewBatch_Click(object? sender, RoutedEventArgs e)
        {
            CompostBatch batch = new CompostBatch(AppData.CurrentUser.Id, DateTime.Now, 60);
            try
            {
                AppData.BatchRepository.Add(batch);
            }
            catch (Exception ex)
            {
                await ShowMessage("Insert Gagal", "Batch baru gagal disimpan.\n\n" + ex.Message);
                return;
            }

            await LoadDashboard(batch.Id);
            await ShowMessage("Berhasil", AppData.GetBatchName(batch) + " berhasil ditambahkan ke database.");
        }

        // UPDATE: batch yang sudah matang ditandai sudah dipanen
        private async void btnHarvest_Click(object? sender, RoutedEventArgs e)
        {
            CompostBatch? batch = SelectedBatch;
            if (batch == null || batch.Status != BatchStatus.Ready)
            {
                await ShowMessage("Panen", "Pilih batch yang berstatus Siap Panen terlebih dahulu.");
                return;
            }

            string batchName = AppData.GetBatchName(batch);
            batch.Status = BatchStatus.Archived;
            try
            {
                AppData.BatchRepository.Update(batch);
            }
            catch (Exception ex)
            {
                batch.Status = BatchStatus.Ready;
                await ShowMessage("Update Gagal", batchName + " gagal diperbarui.\n\n" + ex.Message);
                return;
            }

            await LoadDashboard(batch.Id);
            await ShowMessage("Berhasil", batchName + " berhasil dipanen.");
        }

        // DELETE: hapus batch beserta seluruh sampah di dalamnya
        private async void btnDeleteBatch_Click(object? sender, RoutedEventArgs e)
        {
            CompostBatch? batch = SelectedBatch;
            if (batch == null)
            {
                await ShowMessage("Hapus Batch", "Mohon pilih batch yang akan dihapus.");
                return;
            }

            string batchName = AppData.GetBatchName(batch);
            ButtonResult answer = await MessageBoxManager
                .GetMessageBoxStandard("Konfirmasi", "Yakin ingin menghapus " + batchName
                    + " beserta seluruh sampah di dalamnya?", ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question)
                .ShowWindowDialogAsync(this);
            if (answer != ButtonResult.Yes)
            {
                return;
            }

            try
            {
                AppData.BatchRepository.Delete(batch.Id);
            }
            catch (Exception ex)
            {
                await ShowMessage("Delete Gagal", batchName + " gagal dihapus.\n\n" + ex.Message);
                return;
            }

            await LoadDashboard(null);
            await ShowMessage("Berhasil", batchName + " berhasil dihapus dari database.");
        }

        private async Task ShowMessage(string title, string message)
        {
            await MessageBoxManager.GetMessageBoxStandard(title, message).ShowWindowDialogAsync(this);
        }

        // Data satu baris pada daftar batch
        private class BatchRow
        {
            public BatchRow(CompostBatch batch)
            {
                Batch = batch;
                Name = AppData.GetBatchName(batch);
                StatusText = AppData.GetStatusText(batch.Status);
                Detail = batch.GetTotalWeight().ToString("0.##") + " kg  •  rasio brown/green "
                    + batch.CalculateRatio().ToString("0.##") + "  •  matang "
                    + batch.EstimatedMaturityDate.ToString("dd MMM yyyy");

                double totalDays = (batch.EstimatedMaturityDate - batch.StartDate).TotalDays;
                double elapsedDays = (DateTime.Now - batch.StartDate).TotalDays;
                Progress = totalDays > 0 ? Math.Clamp(elapsedDays / totalDays * 100, 0, 100) : 100;

                switch (batch.Status)
                {
                    case BatchStatus.Active:
                        StatusBackground = Brush.Parse("#E3F2FD");
                        StatusForeground = Brush.Parse("#0D47A1");
                        break;
                    case BatchStatus.Curing:
                        StatusBackground = Brush.Parse("#FFF4D6");
                        StatusForeground = Brush.Parse("#8D5A00");
                        break;
                    case BatchStatus.Ready:
                        StatusBackground = Brush.Parse("#E8F5E9");
                        StatusForeground = Brush.Parse("#1B5E20");
                        break;
                    default:
                        StatusBackground = Brush.Parse("#ECEFF1");
                        StatusForeground = Brush.Parse("#455A64");
                        break;
                }
            }

            public CompostBatch Batch { get; }
            public string Name { get; }
            public string StatusText { get; }
            public string Detail { get; }
            public double Progress { get; }
            public IBrush StatusBackground { get; }
            public IBrush StatusForeground { get; }
        }
    }
}

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Composta.Enums;
using Composta.Models;

namespace Composta
{
    public partial class AddWasteWindow : Window
    {
        // Hanya batch yang masih berjalan yang bisa ditambah sampah
        private readonly List<CompostBatch> _batches;

        public AddWasteWindow() : this(null)
        {
        }

        public AddWasteWindow(CompostBatch? selectedBatch)
        {
            InitializeComponent();

            _batches = AppData.CurrentUser.Batches
                .Where(b => b.Status == BatchStatus.Active || b.Status == BatchStatus.Curing)
                .ToList();
            cbBatch.ItemsSource = _batches.Select(b => AppData.GetBatchName(b) + " (" + AppData.GetStatusText(b.Status) + ")").ToList();
            cbBatch.SelectedIndex = selectedBatch != null ? Math.Max(_batches.IndexOf(selectedBatch), 0) : 0;
            if (_batches.Count == 0)
            {
                btnSave.IsEnabled = false;
                ShowError("Belum ada batch yang sedang berjalan. Buat batch baru di dashboard terlebih dahulu.");
            }

            cbCategory.ItemsSource = AppData.Categories.Select(c => c.Name).ToList();
            cbCategory.SelectedIndex = 0;

            UpdateMoistureLabel();
            UpdatePreview();
        }

        public string? SavedBatchId { get; private set; }

        private WasteCategory SelectedCategory
        {
            get { return AppData.Categories[Math.Max(cbCategory.SelectedIndex, 0)]; }
        }

        private WasteItem CreateWasteItem()
        {
            string name = tbWasteName.Text?.Trim() ?? string.Empty;
            double weight = (double)(nudWeight.Value ?? 0);

            if (SelectedCategory.IsCompostable)
            {
                BrownGreenType type = rbBrown.IsChecked == true ? BrownGreenType.Brown : BrownGreenType.Green;
                return new CompostableWaste(name, weight, SelectedCategory, type, sldMoisture.Value);
            }

            return new NonCompostableWaste(name, weight, SelectedCategory, tbReason.Text?.Trim() ?? string.Empty);
        }

        private void UpdateMoistureLabel()
        {
            lblMoistureValue.Text = sldMoisture.Value.ToString("0") + "%";
        }

        private void UpdatePreview()
        {
            if (lblPreview == null)
            {
                return;
            }

            bool isCompostable = SelectedCategory.IsCompostable;
            pnlCompostable.IsVisible = isCompostable;
            pnlNonCompostable.IsVisible = !isCompostable;

            WasteItem item = CreateWasteItem();
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                item.Name = "Sampah ini";
            }

            if (item is NonCompostableWaste nonCompostable)
            {
                brdPreview.Background = Brush.Parse("#FFF4D6");
                lblPreviewTitle.Foreground = Brush.Parse("#8D5A00");
                lblPreviewTitle.Text = "Tidak dapat dikomposkan";
                lblPreview.Text = nonCompostable.GetDisposalAdvice();
            }
            else
            {
                double co2e = AppData.CarbonCalculator.CalculateCO2eSaved(new List<WasteItem> { item });
                brdPreview.Background = Brush.Parse("#E8F5E9");
                lblPreviewTitle.Foreground = Brush.Parse("#1B5E20");
                lblPreviewTitle.Text = "Estimasi emisi dicegah: " + co2e.ToString("0.##") + " kg CO2e";
                lblPreview.Text = ((CompostableWaste)item).NeedsWater()
                    ? "Bahan cukup kering, tambahkan sedikit air saat dimasukkan ke batch."
                    : "Kelembapan bahan sudah cukup untuk proses pengomposan.";
            }
        }

        private void Input_Changed(object? sender, TextChangedEventArgs e)
        {
            lblError.IsVisible = _batches.Count == 0;
            UpdatePreview();
        }

        private void cbCategory_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void nudWeight_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void sldMoisture_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (lblMoistureValue == null)
            {
                return;
            }

            UpdateMoistureLabel();
            UpdatePreview();
        }

        private void btnSave_Click(object? sender, RoutedEventArgs e)
        {
            if (cbBatch.SelectedIndex < 0 || cbBatch.SelectedIndex >= _batches.Count)
            {
                ShowError("Pilih batch tujuan terlebih dahulu.");
                return;
            }

            if (string.IsNullOrWhiteSpace(tbWasteName.Text))
            {
                ShowError("Nama sampah wajib diisi.");
                return;
            }

            WasteItem item = CreateWasteItem();
            if (item is NonCompostableWaste)
            {
                // Sampah anorganik tidak dimasukkan ke batch kompos
                ShowError("Sampah anorganik tidak dapat dimasukkan ke batch kompos. Pisahkan ke bank sampah.");
                return;
            }

            CompostBatch batch = _batches[cbBatch.SelectedIndex];
            try
            {
                AppData.BatchRepository.AddWasteItem(batch, item);
            }
            catch (Exception ex)
            {
                ShowError("Gagal menyimpan ke database: " + ex.Message);
                return;
            }

            SavedBatchId = batch.Id;
            Close(true);
        }

        private void btnCancel_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.IsVisible = true;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Composta.Enums;
using Composta.Models;
using Composta.Services;

namespace Composta.ViewModels
{
    public partial class AddWasteViewModel : ObservableObject
    {
        public const int MaxNameLength = 100;
        public const decimal MaxWeight = 100;

        private const string NoBatchMessage =
            "Belum ada batch berstatus Aktif. Batch yang sudah masuk Pematangan tidak bisa ditambah sampah, buat batch baru di dashboard terlebih dahulu.";

        [ObservableProperty]
        private BatchItemViewModel? _selectedBatch;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCompostable))]
        [NotifyPropertyChangedFor(nameof(IsNonCompostable))]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private WasteCategory? _selectedCategory;

        [ObservableProperty]
        private string _wasteName = string.Empty;

        [ObservableProperty]
        private decimal? _weight = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGreen))]
        [NotifyPropertyChangedFor(nameof(IsBrown))]
        private BrownGreenType _materialType = BrownGreenType.Green;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MoistureText))]
        private double _moisture = 60;

        [ObservableProperty]
        private string _previewTitle = string.Empty;

        [ObservableProperty]
        private string _previewText = string.Empty;

        [ObservableProperty]
        private bool _isPreviewWarning;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string _errorMessage = string.Empty;

        public AddWasteViewModel(CompostBatch? selectedBatch)
        {
            Batches = AppData.CurrentUser.Batches
                .Where(b => b.Status == BatchStatus.Active)
                .Select(b => new BatchItemViewModel(b))
                .ToList();
            Categories = AppData.Categories;

            _selectedBatch = Batches.FirstOrDefault(b => b.Batch == selectedBatch) ?? Batches.FirstOrDefault();
            _selectedCategory = Categories.FirstOrDefault();

            if (!HasBatches)
            {
                _errorMessage = NoBatchMessage;
            }

            UpdatePreview();
        }

        public event Action<bool>? CloseRequested;

        public IReadOnlyList<BatchItemViewModel> Batches { get; }

        public IReadOnlyList<WasteCategory> Categories { get; }

        public string? SavedBatchId { get; private set; }

        public bool HasBatches
        {
            get { return Batches.Count > 0; }
        }

        public bool HasError
        {
            get { return !string.IsNullOrEmpty(ErrorMessage); }
        }

        public bool IsCompostable
        {
            get { return SelectedCategory?.IsCompostable ?? true; }
        }

        public bool IsNonCompostable
        {
            get { return !IsCompostable; }
        }

        public bool IsGreen
        {
            get { return MaterialType == BrownGreenType.Green; }
            set
            {
                if (value)
                {
                    MaterialType = BrownGreenType.Green;
                }
            }
        }

        public bool IsBrown
        {
            get { return MaterialType == BrownGreenType.Brown; }
            set
            {
                if (value)
                {
                    MaterialType = BrownGreenType.Brown;
                }
            }
        }

        public string MoistureText
        {
            get { return Moisture.ToString("0") + "%"; }
        }

        partial void OnSelectedCategoryChanged(WasteCategory? value)
        {
            UpdatePreview();
        }

        partial void OnWasteNameChanged(string value)
        {
            ClearInputError();
            UpdatePreview();
        }

        partial void OnWeightChanged(decimal? value)
        {
            ClearInputError();
            UpdatePreview();
        }

        partial void OnMaterialTypeChanged(BrownGreenType value)
        {
            UpdatePreview();
        }

        partial void OnMoistureChanged(double value)
        {
            UpdatePreview();
        }

        private bool CanSave()
        {
            return HasBatches && IsCompostable;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            if (SelectedBatch == null)
            {
                ErrorMessage = "Pilih batch tujuan terlebih dahulu.";
                return;
            }

            if (string.IsNullOrWhiteSpace(WasteName))
            {
                ErrorMessage = "Nama sampah wajib diisi.";
                return;
            }

            if (WasteName.Trim().Length > MaxNameLength)
            {
                ErrorMessage = "Nama sampah maksimal " + MaxNameLength + " karakter.";
                return;
            }

            if (Weight == null || Weight <= 0 || Weight > MaxWeight)
            {
                ErrorMessage = "Berat harus lebih dari 0 kg dan paling banyak " + MaxWeight + " kg.";
                return;
            }

            WasteItem item = CreateWasteItem();
            if (item is NonCompostableWaste)
            {
                ErrorMessage = "Sampah anorganik tidak dapat dimasukkan ke batch kompos. Pisahkan ke bank sampah.";
                return;
            }

            try
            {
                int estimatedDays = AppData.Advisor.EstimateMaturityDays(AppData.Location);
                AppData.BatchRepository.AddWasteItem(SelectedBatch.Batch, item, estimatedDays);
            }
            catch (Exception ex)
            {
                ErrorMessage = DatabaseErrorMessage.From(ex, AppData.DatabaseInfo);
                return;
            }

            SavedBatchId = SelectedBatch.Batch.Id;
            CloseRequested?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseRequested?.Invoke(false);
        }

        private WasteItem CreateWasteItem()
        {
            string name = WasteName.Trim();
            double weight = Math.Max((double)(Weight ?? 0), 0);

            if (IsCompostable)
            {
                return new CompostableWaste(name, weight, SelectedCategory, MaterialType, Moisture);
            }

            return new NonCompostableWaste(name, weight, SelectedCategory, string.Empty);
        }

        private void ClearInputError()
        {
            ErrorMessage = HasBatches ? string.Empty : NoBatchMessage;
        }

        private void UpdatePreview()
        {
            WasteItem item = CreateWasteItem();
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                item.Name = "Sampah ini";
            }

            if (item is NonCompostableWaste nonCompostable)
            {
                IsPreviewWarning = true;
                PreviewTitle = "Tidak dapat dikomposkan";
                PreviewText = nonCompostable.GetDisposalAdvice();
                return;
            }

            double co2e = AppData.CarbonCalculator.CalculateCO2eSaved(new List<WasteItem> { item });
            IsPreviewWarning = false;
            PreviewTitle = "Estimasi emisi dicegah: " + co2e.ToString("0.##") + " kg CO2e";
            PreviewText = ((CompostableWaste)item).NeedsWater()
                ? "Bahan cukup kering, tambahkan sedikit air saat dimasukkan ke batch."
                : "Kelembapan bahan sudah cukup untuk proses pengomposan.";
        }
    }
}

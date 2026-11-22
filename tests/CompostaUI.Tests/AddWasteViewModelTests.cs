using Composta.ViewModels;

namespace Composta.UITests
{
    public class AddWasteViewModelTests : DatabaseTest
    {
        public AddWasteViewModelTests(DatabaseFixture db) : base(db)
        {
        }

        private static AddWasteViewModel NewForm()
        {
            AppData.Load();
            return new AddWasteViewModel(null);
        }

        [SkippableFact]
        public void Form_OnlyOffersRunningBatches()
        {
            RequireDatabase();

            AddWasteViewModel form = NewForm();

            Assert.Single(form.Batches);
            Assert.All(form.Batches, b => Assert.True(b.IsActive));
            Assert.True(form.SaveCommand.CanExecute(null));
            Assert.False(form.HasError);
        }

        [SkippableFact]
        public void EmptyName_ShowsErrorAndDoesNotClose()
        {
            RequireDatabase();

            AddWasteViewModel form = NewForm();
            bool closed = false;
            form.CloseRequested += _ => closed = true;

            form.SaveCommand.Execute(null);

            Assert.Equal("Nama sampah wajib diisi.", form.ErrorMessage);
            Assert.False(closed);

            form.WasteName = "Kulit pisang";
            Assert.False(form.HasError);
        }

        [SkippableTheory]
        [InlineData(null)]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(100.5)]
        public void InvalidWeight_IsRejected(double? weight)
        {
            RequireDatabase();

            AddWasteViewModel form = NewForm();
            form.WasteName = "Kulit pisang";
            form.Weight = (decimal?)weight;

            form.SaveCommand.Execute(null);

            Assert.StartsWith("Berat harus lebih dari 0 kg", form.ErrorMessage);
            Assert.Equal(11, Db.Count("SELECT COUNT(*) FROM waste_items"));
        }

        [SkippableFact]
        public void TooLongName_IsRejected()
        {
            RequireDatabase();

            AddWasteViewModel form = NewForm();
            form.WasteName = new string('a', 101);

            form.SaveCommand.Execute(null);

            Assert.Equal("Nama sampah maksimal 100 karakter.", form.ErrorMessage);
            Assert.Equal(11, Db.Count("SELECT COUNT(*) FROM waste_items"));
        }

        [SkippableFact]
        public void NonCompostableCategory_SwitchesFormAndIsRejected()
        {
            RequireDatabase();

            AddWasteViewModel form = NewForm();
            form.WasteName = "Botol";
            form.SelectedCategory = form.Categories.First(c => !c.IsCompostable);

            Assert.True(form.IsNonCompostable);
            Assert.True(form.IsPreviewWarning);
            Assert.Equal("Tidak dapat dikomposkan", form.PreviewTitle);
            Assert.Contains("bank sampah", form.PreviewText);
            Assert.False(form.SaveCommand.CanExecute(null));

            form.SaveCommand.Execute(null);
            Assert.Contains("anorganik", form.ErrorMessage);
            Assert.Equal(11, Db.Count("SELECT COUNT(*) FROM waste_items"));
        }

        [SkippableFact]
        public void SwitchingBackToCompostableCategory_EnablesSave()
        {
            RequireDatabase();

            AddWasteViewModel form = NewForm();
            bool saveStateChanged = false;
            form.SaveCommand.CanExecuteChanged += (_, _) => saveStateChanged = true;

            form.SelectedCategory = form.Categories.First(c => !c.IsCompostable);
            Assert.False(form.SaveCommand.CanExecute(null));
            Assert.True(saveStateChanged);

            form.SelectedCategory = form.Categories.First(c => c.IsCompostable);
            Assert.True(form.SaveCommand.CanExecute(null));
        }

        [SkippableFact]
        public void Preview_FollowsWeightAndMoisture()
        {
            RequireDatabase();

            AddWasteViewModel form = NewForm();
            form.SelectedCategory = form.Categories.First(c => c.Name.StartsWith("Sisa Sayur"));
            form.Weight = 4;

            Assert.Equal("Estimasi emisi dicegah: 2 kg CO2e", form.PreviewTitle);

            form.Moisture = 20;
            Assert.Equal("20%", form.MoistureText);
            Assert.Contains("cukup kering", form.PreviewText);
        }

        [SkippableFact]
        public void RadioButtons_SwitchMaterialType()
        {
            RequireDatabase();

            AddWasteViewModel form = NewForm();
            Assert.True(form.IsGreen);

            form.IsBrown = true;
            Assert.False(form.IsGreen);

            form.IsBrown = false;
            Assert.True(form.IsBrown);
        }

        [SkippableFact]
        public void Cancel_ClosesWithoutSaving()
        {
            RequireDatabase();

            AddWasteViewModel form = NewForm();
            bool? result = null;
            form.CloseRequested += saved => result = saved;

            form.CancelCommand.Execute(null);

            Assert.False(result);
            Assert.Equal(11, Db.Count("SELECT COUNT(*) FROM waste_items"));
        }

        [SkippableFact]
        public void UserWithoutRunningBatch_CannotSave()
        {
            RequireDatabase();

            AppData.Login(DatabaseFixture.DemoUserId);
            AppData.CurrentUser.Batches.ForEach(b => b.Status = Enums.BatchStatus.Ready);
            AddWasteViewModel form = new AddWasteViewModel(null);

            Assert.Empty(form.Batches);
            Assert.False(form.SaveCommand.CanExecute(null));
            Assert.Contains("Belum ada batch", form.ErrorMessage);
        }
    }
}

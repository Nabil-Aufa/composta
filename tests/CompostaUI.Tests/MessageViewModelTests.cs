using Composta.ViewModels;

namespace Composta.UITests
{
    public class MessageViewModelTests
    {
        [Theory]
        [InlineData("Pengingat Perawatan", MessageTone.Warning)]
        [InlineData("Database Gagal", MessageTone.Error)]
        [InlineData("Delete Gagal", MessageTone.Error)]
        [InlineData("Berhasil", MessageTone.Success)]
        [InlineData("Panen", MessageTone.Info)]
        public void ToneFollowsTitle(string title, MessageTone expected)
        {
            MessageViewModel viewModel = new MessageViewModel(title, "isi");

            Assert.Equal(expected, viewModel.Tone);
            Assert.False(viewModel.IsConfirmation);
            Assert.Equal("Mengerti", viewModel.PrimaryText);
        }

        [Fact]
        public void ConfirmationUsesConfirmToneAndButtons()
        {
            MessageViewModel viewModel = new MessageViewModel("Konfirmasi", "Hapus?", isConfirmation: true);

            Assert.True(viewModel.IsConfirm);
            Assert.True(viewModel.IsConfirmation);
            Assert.Equal("Ya, lanjutkan", viewModel.PrimaryText);
            Assert.Equal("Composta - Konfirmasi", viewModel.WindowTitle);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CommandsRequestCloseWithResult(bool accept)
        {
            MessageViewModel viewModel = new MessageViewModel("Konfirmasi", "Hapus?", isConfirmation: true);
            bool? result = null;
            viewModel.CloseRequested += value => result = value;

            if (accept)
            {
                viewModel.AcceptCommand.Execute(null);
            }
            else
            {
                viewModel.CancelCommand.Execute(null);
            }

            Assert.Equal(accept, result);
        }
    }
}

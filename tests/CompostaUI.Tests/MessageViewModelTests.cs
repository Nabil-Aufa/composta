using Composta.ViewModels;

namespace Composta.UITests
{
    public class MessageViewModelTests
    {
        [Theory]
        [InlineData(MessageTone.Info)]
        [InlineData(MessageTone.Success)]
        [InlineData(MessageTone.Warning)]
        [InlineData(MessageTone.Error)]
        public void ToneComesFromParameter(MessageTone tone)
        {
            MessageViewModel viewModel = new MessageViewModel("Judul Bebas", "isi", tone);

            Assert.Equal(tone, viewModel.Tone);
            Assert.False(viewModel.IsConfirmation);
            Assert.Equal("Mengerti", viewModel.PrimaryText);
        }

        [Fact]
        public void ToneDoesNotDependOnTitle()
        {
            MessageViewModel viewModel = new MessageViewModel("Update Gagal", "isi", MessageTone.Info);

            Assert.True(viewModel.IsInfo);
            Assert.False(viewModel.IsError);
        }

        [Fact]
        public void ConfirmationUsesConfirmToneAndButtons()
        {
            MessageViewModel viewModel = new MessageViewModel("Konfirmasi", "Hapus?", MessageTone.Confirm);

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
            MessageViewModel viewModel = new MessageViewModel("Konfirmasi", "Hapus?", MessageTone.Confirm);
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

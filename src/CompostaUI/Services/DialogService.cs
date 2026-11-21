using Avalonia.Controls;
using Composta.ViewModels;
using Composta.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MessageBoxIcon = MsBox.Avalonia.Enums.Icon;

namespace Composta.Services
{
    public class DialogService : IDialogService
    {
        private readonly Window _owner;

        public DialogService(Window owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public async Task ShowMessageAsync(string title, string message)
        {
            await MessageBoxManager.GetMessageBoxStandard(title, message).ShowWindowDialogAsync(_owner);
        }

        public async Task<bool> ConfirmAsync(string title, string message)
        {
            ButtonResult answer = await MessageBoxManager
                .GetMessageBoxStandard(title, message, ButtonEnum.YesNo, MessageBoxIcon.Question)
                .ShowWindowDialogAsync(_owner);
            return answer == ButtonResult.Yes;
        }

        public Task ShowProfileAsync(ProfileViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ProfileWindow window = new ProfileWindow { DataContext = viewModel };
            return window.ShowDialog(_owner);
        }

        public async Task<bool> ShowAddWasteAsync(AddWasteViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            AddWasteWindow window = new AddWasteWindow { DataContext = viewModel };

            void CloseWindow(bool saved)
            {
                window.Close(saved);
            }

            viewModel.CloseRequested += CloseWindow;
            try
            {
                return await window.ShowDialog<bool>(_owner);
            }
            finally
            {
                viewModel.CloseRequested -= CloseWindow;
            }
        }
    }
}

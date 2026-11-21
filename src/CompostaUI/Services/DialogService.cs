using Avalonia.Controls;
using Composta.ViewModels;
using Composta.Views;

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
            await ShowMessageWindowAsync(new MessageViewModel(title, message));
        }

        public Task<bool> ConfirmAsync(string title, string message)
        {
            return ShowMessageWindowAsync(new MessageViewModel(title, message, isConfirmation: true));
        }

        private async Task<bool> ShowMessageWindowAsync(MessageViewModel viewModel)
        {
            MessageWindow window = new MessageWindow { DataContext = viewModel };

            void CloseWindow(bool accepted)
            {
                window.Close(accepted);
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

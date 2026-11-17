using Composta.Services;
using Composta.ViewModels;

namespace Composta.UITests
{
    public class FakeDialogService : IDialogService
    {
        public List<string> Messages { get; } = new List<string>();

        public bool ConfirmResult { get; set; } = true;

        public ProfileViewModel? LastProfile { get; private set; }

        public Task ShowProfileAsync(ProfileViewModel viewModel)
        {
            LastProfile = viewModel;
            return Task.CompletedTask;
        }

        public Func<AddWasteViewModel, bool>? OnAddWaste { get; set; }

        public Task ShowMessageAsync(string title, string message)
        {
            Messages.Add(title + ": " + message);
            return Task.CompletedTask;
        }

        public Task<bool> ConfirmAsync(string title, string message)
        {
            Messages.Add(title + ": " + message);
            return Task.FromResult(ConfirmResult);
        }

        public Task<bool> ShowAddWasteAsync(AddWasteViewModel viewModel)
        {
            bool saved = OnAddWaste?.Invoke(viewModel) ?? false;
            return Task.FromResult(saved);
        }
    }
}

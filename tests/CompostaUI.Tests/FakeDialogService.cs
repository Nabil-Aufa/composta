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

        public Dictionary<string, MessageTone> TonesByTitle { get; } = new Dictionary<string, MessageTone>();

        public Task ShowMessageAsync(string title, string message, MessageTone tone)
        {
            Messages.Add(title + ": " + message);
            TonesByTitle[title] = tone;
            return Task.CompletedTask;
        }

        public Dictionary<string, bool> DestructiveByTitle { get; } = new Dictionary<string, bool>();

        public Task<bool> ConfirmAsync(string title, string message, bool isDestructive)
        {
            Messages.Add(title + ": " + message);
            DestructiveByTitle[title] = isDestructive;
            return Task.FromResult(ConfirmResult);
        }

        public Task<bool> ShowAddWasteAsync(AddWasteViewModel viewModel)
        {
            bool saved = OnAddWaste?.Invoke(viewModel) ?? false;
            return Task.FromResult(saved);
        }
    }
}

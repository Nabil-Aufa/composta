using Composta.ViewModels;

namespace Composta.Services
{
    public interface IDialogService
    {
        Task ShowMessageAsync(string title, string message, MessageTone tone);

        Task<bool> ConfirmAsync(string title, string message);

        Task<bool> ShowAddWasteAsync(AddWasteViewModel viewModel);

        Task ShowProfileAsync(ProfileViewModel viewModel);
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Composta.ViewModels
{
    public enum MessageTone
    {
        Info,
        Success,
        Warning,
        Error,
        Confirm
    }

    public partial class MessageViewModel : ObservableObject
    {
        public MessageViewModel(string title, string message, MessageTone tone, bool isDestructive = false)
        {
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            Tone = tone;
            IsDestructive = isDestructive && tone == MessageTone.Confirm;
        }

        public bool IsDestructive { get; }

        public event Action<bool>? CloseRequested;

        public string Title { get; }

        public string Message { get; }

        public string WindowTitle => "Composta - " + Title;

        public bool IsConfirmation => Tone == MessageTone.Confirm;

        public MessageTone Tone { get; }

        public bool IsInfo => Tone == MessageTone.Info;

        public bool IsSuccess => Tone == MessageTone.Success;

        public bool IsWarning => Tone == MessageTone.Warning;

        public bool IsError => Tone == MessageTone.Error;

        public bool IsConfirm => Tone == MessageTone.Confirm;

        public string PrimaryText => IsConfirmation ? "Ya, lanjutkan" : "Mengerti";

        [RelayCommand]
        private void Accept()
        {
            CloseRequested?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseRequested?.Invoke(false);
        }
    }
}

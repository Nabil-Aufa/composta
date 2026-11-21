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
        public MessageViewModel(string title, string message, bool isConfirmation = false)
        {
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            IsConfirmation = isConfirmation;
            Tone = ToneFromTitle(Title, isConfirmation);
        }

        public event Action<bool>? CloseRequested;

        public string Title { get; }

        public string Message { get; }

        public string WindowTitle => "Composta - " + Title;

        public bool IsConfirmation { get; }

        public MessageTone Tone { get; }

        public bool IsInfo => Tone == MessageTone.Info;

        public bool IsSuccess => Tone == MessageTone.Success;

        public bool IsWarning => Tone == MessageTone.Warning;

        public bool IsError => Tone == MessageTone.Error;

        public bool IsConfirm => Tone == MessageTone.Confirm;

        public string PrimaryText => IsConfirmation ? "Ya, lanjutkan" : "Mengerti";

        public static MessageTone ToneFromTitle(string title, bool isConfirmation)
        {
            if (isConfirmation)
            {
                return MessageTone.Confirm;
            }

            if (title.Contains("Gagal", StringComparison.OrdinalIgnoreCase))
            {
                return MessageTone.Error;
            }

            if (title.Contains("Berhasil", StringComparison.OrdinalIgnoreCase))
            {
                return MessageTone.Success;
            }

            if (title.Contains("Pengingat", StringComparison.OrdinalIgnoreCase))
            {
                return MessageTone.Warning;
            }

            return MessageTone.Info;
        }

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

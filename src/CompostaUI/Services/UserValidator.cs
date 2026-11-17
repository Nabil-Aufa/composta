using System.Text.RegularExpressions;

namespace Composta.Services
{
    public static class UserValidator
    {
        public const int MaxNameLength = 100;
        public const int MaxEmailLength = 255;

        private static readonly Regex EmailPattern = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public static string? Validate(string? name, string? email)
        {
            string trimmedName = name?.Trim() ?? string.Empty;
            string trimmedEmail = email?.Trim() ?? string.Empty;

            if (trimmedName.Length == 0)
            {
                return "Nama wajib diisi.";
            }

            if (trimmedName.Length > MaxNameLength)
            {
                return "Nama maksimal " + MaxNameLength + " karakter.";
            }

            if (trimmedEmail.Length == 0)
            {
                return "Email wajib diisi.";
            }

            if (trimmedEmail.Length > MaxEmailLength)
            {
                return "Email maksimal " + MaxEmailLength + " karakter.";
            }

            if (!EmailPattern.IsMatch(trimmedEmail))
            {
                return "Format email tidak valid, contoh: nama@email.com.";
            }

            return null;
        }

        public static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}

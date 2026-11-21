using System.Globalization;
using Avalonia.Data.Converters;

namespace Composta.Converters
{
    /// <summary>
    /// Mengubah nama lengkap menjadi inisial untuk avatar, misalnya "Nayla Thalita" menjadi "NT".
    /// </summary>
    public class InitialsConverter : IValueConverter
    {
        public static readonly InitialsConverter Instance = new InitialsConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string name = value as string ?? string.Empty;
            string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return "?";
            }

            string initials = parts[0][..1];
            if (parts.Length > 1)
            {
                initials += parts[^1][..1];
            }

            return initials.ToUpperInvariant();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

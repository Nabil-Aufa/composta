using System.Net.Sockets;
using Npgsql;

namespace Composta.Services
{
    public static class DatabaseErrorMessage
    {
        private const string UndefinedFunction = "42883";
        private const string UndefinedTable = "42P01";
        private const string InvalidPassword = "28P01";
        private const string InvalidDatabase = "3D000";
        private const string UniqueViolation = "23505";
        private const string ForeignKeyViolation = "23503";
        private const string CheckViolation = "23514";
        private const string NotNullViolation = "23502";
        private const string StringTooLong = "22001";
        private const string NumericOutOfRange = "22003";
        private const string UserEmailConstraint = "users_email_key";

        public static string From(Exception ex, string databaseInfo)
        {
            ArgumentNullException.ThrowIfNull(ex);

            PostgresException? postgres = Find<PostgresException>(ex);
            if (postgres != null)
            {
                switch (postgres.SqlState)
                {
                    case UndefinedFunction:
                    case UndefinedTable:
                        return "Skema database " + databaseInfo + " belum sesuai dengan versi aplikasi ini.\n\n"
                            + "Jalankan ulang src/CompostaUI/Database/composta_postgres.sql pada database tersebut "
                            + "(data lama akan diganti dengan data awal).";
                    case InvalidPassword:
                        return "Username atau password PostgreSQL salah.\n\n"
                            + "Periksa connection string di AppData.cs atau environment variable COMPOSTA_DB.";
                    case InvalidDatabase:
                        return "Database " + databaseInfo + " belum dibuat.\n\n"
                            + "Buat database tersebut lalu jalankan src/CompostaUI/Database/composta_postgres.sql.";
                    case UniqueViolation:
                        return postgres.ConstraintName == UserEmailConstraint
                            ? "Email tersebut sudah terdaftar. Gunakan email lain atau pilih profil yang sudah ada."
                            : "Data yang sama sudah tersimpan di database.";
                    case ForeignKeyViolation:
                        return "Data terkait tidak ditemukan di database, mungkin sudah dihapus. Muat ulang aplikasi lalu coba lagi.";
                    case CheckViolation:
                    case NotNullViolation:
                        return "Data tidak memenuhi aturan database, periksa kembali isian yang dimasukkan.";
                    case StringTooLong:
                        return "Teks yang dimasukkan terlalu panjang untuk disimpan.";
                    case NumericOutOfRange:
                        return "Angka yang dimasukkan terlalu besar untuk disimpan.";
                }
            }

            if (Find<SocketException>(ex) != null || Find<TimeoutException>(ex) != null)
            {
                return "Tidak dapat terhubung ke server PostgreSQL (" + databaseInfo + ").\n\n"
                    + "Pastikan PostgreSQL sudah berjalan dan host/port pada connection string sudah benar.";
            }

            if (ex is InvalidOperationException && Find<NpgsqlException>(ex) == null)
            {
                return ex.Message;
            }

            return "Terjadi kesalahan pada database " + databaseInfo + ".\n\n" + ex.Message;
        }

        private static T? Find<T>(Exception ex) where T : Exception
        {
            for (Exception? current = ex; current != null; current = current.InnerException)
            {
                if (current is T match)
                {
                    return match;
                }
            }

            return null;
        }
    }
}

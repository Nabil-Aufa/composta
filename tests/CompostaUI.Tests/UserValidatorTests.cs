using Composta.Services;

namespace Composta.UITests
{
    public class UserValidatorTests
    {
        [Theory]
        [InlineData("Nabil", "nabil@mail.ugm.ac.id")]
        [InlineData("  Nayla  ", "  nayla@composta.id ")]
        public void ValidInput_HasNoError(string name, string email)
        {
            Assert.Null(UserValidator.Validate(name, email));
        }

        [Theory]
        [InlineData("", "a@b.com", "Nama wajib diisi.")]
        [InlineData("   ", "a@b.com", "Nama wajib diisi.")]
        [InlineData(null, "a@b.com", "Nama wajib diisi.")]
        [InlineData("Nabil", "", "Email wajib diisi.")]
        [InlineData("Nabil", null, "Email wajib diisi.")]
        public void EmptyInput_IsRejected(string? name, string? email, string expected)
        {
            Assert.Equal(expected, UserValidator.Validate(name, email));
        }

        [Theory]
        [InlineData("nabil")]
        [InlineData("nabil@")]
        [InlineData("nabil@mail")]
        [InlineData("nabil @mail.com")]
        [InlineData("@mail.com")]
        public void InvalidEmail_IsRejected(string email)
        {
            Assert.StartsWith("Format email tidak valid", UserValidator.Validate("Nabil", email));
        }

        [Fact]
        public void TooLongInput_IsRejected()
        {
            Assert.Equal("Nama maksimal 100 karakter.", UserValidator.Validate(new string('a', 101), "a@b.com"));
            Assert.Equal("Email maksimal 255 karakter.", UserValidator.Validate("Nabil", new string('a', 250) + "@b.com"));
        }

        [Fact]
        public void NormalizeEmail_TrimsAndLowercases()
        {
            Assert.Equal("nabil@composta.id", UserValidator.NormalizeEmail("  Nabil@Composta.ID "));
        }
    }
}

using Composta.Models;

namespace Composta.UITests
{
    public class UserSessionTests : DatabaseTest
    {
        public UserSessionTests(DatabaseFixture db) : base(db)
        {
        }

        [SkippableFact]
        public void TwoUsers_OnlySeeTheirOwnBatches()
        {
            RequireDatabase();

            AppData.Login(DatabaseFixture.NabilId);
            List<CompostBatch> nabilBatches = AppData.CurrentUser.Batches;

            AppData.Login(DatabaseFixture.DemoUserId);
            List<CompostBatch> demoBatches = AppData.CurrentUser.Batches;

            Assert.Equal(3, nabilBatches.Count);
            Assert.Single(demoBatches);
            Assert.All(nabilBatches, b => Assert.Equal(DatabaseFixture.NabilId, b.UserId));
            Assert.All(demoBatches, b => Assert.Equal(DatabaseFixture.DemoUserId, b.UserId));
            Assert.Empty(nabilBatches.Select(b => b.Id).Intersect(demoBatches.Select(b => b.Id)));
        }

        [SkippableFact]
        public void Load_WithoutLogin_UsesFirstUser_AndReloadKeepsActiveUser()
        {
            RequireDatabase();

            AppData.Load();
            Assert.True(AppData.IsLoggedIn);
            Assert.Equal(DatabaseFixture.NabilId, AppData.CurrentUser.Id);

            AppData.Login(DatabaseFixture.DemoUserId);
            AppData.Load();
            Assert.Equal(DatabaseFixture.DemoUserId, AppData.CurrentUser.Id);
        }

        [SkippableFact]
        public void Logout_ClearsActiveUser()
        {
            RequireDatabase();

            AppData.Login(DatabaseFixture.NabilId);
            AppData.Logout();

            Assert.False(AppData.IsLoggedIn);
            Assert.Empty(AppData.CurrentUser.Batches);
        }

        [SkippableFact]
        public void RegisterUser_SavesNormalizedUser_ThatCanLogin()
        {
            RequireDatabase();

            User user = AppData.RegisterUser("  Gilbert  ", "  Gilbert@Composta.ID ");
            AppData.Login(user.Id);

            Assert.Equal("Gilbert", AppData.CurrentUser.Name);
            Assert.Equal("gilbert@composta.id", AppData.CurrentUser.Email);
            Assert.Empty(AppData.CurrentUser.Batches);
            Assert.Equal(3, AppData.GetUsers().Count);
        }

        [SkippableFact]
        public void RegisterUser_RejectsDuplicateEmail_IgnoringCase()
        {
            RequireDatabase();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => AppData.RegisterUser("Nabil Lagi", "NABIL@composta.id"));

            Assert.Contains("sudah terdaftar", ex.Message);
            Assert.Equal(2, AppData.GetUsers().Count);
        }

        [SkippableFact]
        public void RegisterUser_RejectsInvalidInput()
        {
            RequireDatabase();

            Assert.Throws<ArgumentException>(() => AppData.RegisterUser("", "a@b.com"));
            Assert.Throws<ArgumentException>(() => AppData.RegisterUser("Nama", "bukan-email"));
            Assert.Equal(2, AppData.GetUsers().Count);
        }

        [SkippableFact]
        public void Login_UnknownUser_IsRejected()
        {
            RequireDatabase();

            Assert.Throws<InvalidOperationException>(() => AppData.Login("tidak-ada"));
            Assert.False(AppData.IsLoggedIn);
        }
    }
}

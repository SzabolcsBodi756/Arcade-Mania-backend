using Arcade_mania_backend_webAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NETCore.Encrypt;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_AdminGetAllTests
    {
        [TestMethod]
        public async Task GetAllUsersAdmin_Returns200_DecryptsPassword_AndDefaultsRole()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var games = TestHelpers.SeedGamesReturnEntities(ctx, "Snake", "Tetris");
            var cfg = TestHelpers.CreateTestConfig();
            var pwKey = cfg["Crypto:PasswordKey"]!;

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            // user1: role üres -> "User" default
            ctx.Users.Add(new User
            {
                Id = id1,
                UserName = "Elek",
                PasswordHash = EncryptProvider.AESEncrypt("pass1", pwKey),
                Role = ""
            });

            // user2: admin
            ctx.Users.Add(new User
            {
                Id = id2,
                UserName = "Admin",
                PasswordHash = EncryptProvider.AESEncrypt("pass2", pwKey),
                Role = "Admin"
            });

            ctx.UserHighScores.Add(new UserHighScore { UserId = id1, GameId = games[0].Id, HighScore = 10u });
            ctx.UserHighScores.Add(new UserHighScore { UserId = id2, GameId = games[1].Id, HighScore = 20u });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            var action = await controller.GetAllUsersAdmin();

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(200, obj!.StatusCode);

            // { message, result } -> result: List<UserDataAdminDto>
            var resultObj = TestHelpers.GetAnonymousProp<object>(obj.Value!, "result");
            Assert.IsNotNull(resultObj);

            // resultObj tipikusan List<UserDataAdminDto>
            // Dinamikus introspekció: nézzük, hogy van-e benne "Password" mező és "Role"
            // (MSTestben nem akarunk túl sok reflectiont, de itt hasznos)
            var enumerable = resultObj as System.Collections.IEnumerable;
            Assert.IsNotNull(enumerable);

            var list = enumerable!.Cast<object>().ToList();
            Assert.AreEqual(2, list.Count);

            // Keressük ki Eleket
            var elek = list.Single(x => (string?)x.GetType().GetProperty("Name")?.GetValue(x) == "Elek");
            var elekPassword = (string?)elek.GetType().GetProperty("Password")?.GetValue(elek);
            var elekRole = (string?)elek.GetType().GetProperty("Role")?.GetValue(elek);
            var elekScores = elek.GetType().GetProperty("Scores")?.GetValue(elek) as System.Collections.IEnumerable;

            Assert.AreEqual("pass1", elekPassword, "Admin GET ALL-nál a jelszó decryptelve jön vissza.");
            Assert.AreEqual("User", elekRole, "Üres role esetén default User kell legyen.");
            Assert.IsNotNull(elekScores, "Scores nem lehet null.");

            // Admin user ellenőrzés
            var admin = list.Single(x => (string?)x.GetType().GetProperty("Name")?.GetValue(x) == "Admin");
            var adminRole = (string?)admin.GetType().GetProperty("Role")?.GetValue(admin);
            Assert.AreEqual("Admin", adminRole);
        }

        [TestMethod]
        public async Task GetAllUsersAdmin_WhenDecryptFails_Returns400()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");
            var cfg = TestHelpers.CreateTestConfig();

            // Nem decryptelhető hash -> AESDecrypt exception -> catch -> 400
            ctx.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                UserName = "Bad",
                PasswordHash = "NOT_AES",
                Role = "User"
            });
            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            var action = await controller.GetAllUsersAdmin();

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(400, obj!.StatusCode);
        }
    }
}

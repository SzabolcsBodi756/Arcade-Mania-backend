using Arcade_mania_backend_webAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NETCore.Encrypt;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_AdminGetByIdTests
    {

        [TestMethod]
        public async Task GetUserAdminById_NotFound_Returns404()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.GetUserAdminById(Guid.NewGuid());

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(404, obj!.StatusCode);
        }


        [TestMethod]
        public async Task GetUserAdminById_Success_Returns200_DecryptsPassword_AndHasScores()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var games = TestHelpers.SeedGamesReturnEntities(ctx, "Snake", "Tetris");

            var cfg = TestHelpers.CreateTestConfig();

            var pwKey = cfg["Crypto:PasswordKey"]!;

            var userId = Guid.NewGuid();

            ctx.Users.Add(new User
            {
                Id = userId,
                UserName = "Elek",
                PasswordHash = EncryptProvider.AESEncrypt("pass123", pwKey),
                Role = "" // default User
            });

            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[0].Id, HighScore = 10u });

            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[1].Id, HighScore = 20u });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            var action = await controller.GetUserAdminById(userId);

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(200, obj!.StatusCode);

            var result = TestHelpers.GetAnonymousProp<object>(obj.Value!, "result");

            Assert.IsNotNull(result);

            var pass = (string?)result!.GetType().GetProperty("Password")?.GetValue(result);

            var role = (string?)result.GetType().GetProperty("Role")?.GetValue(result);

            var scores = result.GetType().GetProperty("Scores")?.GetValue(result) as System.Collections.IEnumerable;

            Assert.AreEqual("pass123", pass);

            Assert.AreEqual("User", role);

            Assert.IsNotNull(scores);
        }


        [TestMethod]
        public async Task GetUserAdminById_DecryptFails_Returns400()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var cfg = TestHelpers.CreateTestConfig();

            var userId = Guid.NewGuid();

            ctx.Users.Add(new User
            {
                Id = userId,
                UserName = "Bad",
                PasswordHash = "NOT_AES",
                Role = "User"
            });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            var action = await controller.GetUserAdminById(userId);

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(400, obj!.StatusCode);
        }
    }
}

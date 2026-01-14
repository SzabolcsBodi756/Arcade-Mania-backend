using Arcade_mania_backend_webAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_PublicTests
    {
        [TestMethod]
        public async Task GetAllUsersPublic_EmptyDb_Returns200_EmptyList()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);
            TestHelpers.SeedGames(ctx, "Snake");

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.GetAllUsersPublic();

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(200, obj!.StatusCode);

            var result = TestHelpers.GetAnonymousProp<object>(obj.Value!, "result");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GetUserPublicById_NotFound_Returns404()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);
            TestHelpers.SeedGames(ctx, "Snake");

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.GetUserPublicById(Guid.NewGuid());

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(404, obj!.StatusCode);
        }

        [TestMethod]
        public async Task GetUserPublicById_Success_Returns200_WithScores()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);
            var games = TestHelpers.SeedGamesReturnEntities(ctx, "Snake", "Tetris");

            var userId = Guid.NewGuid();
            ctx.Users.Add(new User { Id = userId, UserName = "Elek", PasswordHash = "x", Role = "User" });
            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[0].Id, HighScore = 10u });
            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[1].Id, HighScore = 20u });
            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.GetUserPublicById(userId);

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(200, obj!.StatusCode);

            var result = TestHelpers.GetAnonymousProp<object>(obj.Value!, "result")!;
            var scores = TestHelpers.GetAnonymousProp<object>(result, "Scores");
            Assert.IsNotNull(scores);
        }
    }
}

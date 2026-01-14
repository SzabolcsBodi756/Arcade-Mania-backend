using Arcade_mania_backend_webAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_GetMyScoresHappyTests
    {
        [TestMethod]
        public async Task GetMyScores_Returns200_WithScoresList()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var games = TestHelpers.SeedGamesReturnEntities(ctx, "Snake", "Tetris");
            var userId = Guid.NewGuid();

            ctx.Users.Add(new User { Id = userId, UserName = "Elek", PasswordHash = "x", Role = "User" });
            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[0].Id, HighScore = 10u });
            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[1].Id, HighScore = 20u });
            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig(), authenticatedUserId: userId);

            var action = await controller.GetMyScores();

            var ok = action as OkObjectResult;
            Assert.IsNotNull(ok);

            var result = TestHelpers.GetAnonymousProp<object>(ok.Value!, "result");
            Assert.IsNotNull(result, "result-nek listának kell lennie (scores).");
        }
    }
}

using Arcade_mania_backend_webAPI.Models;
using Arcade_mania_backend_webAPI.Models.Dtos.Scores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_MeScoresTests
    {

        [TestMethod]
        public async Task GetMyScores_InvalidToken_Returns401()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            // authenticatedUserId = null -> nincs claim -> Guid.Empty -> 401
            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig(), authenticatedUserId: null);

            var action = await controller.GetMyScores();

            // GetMyScores: StatusCode(401, ...)
            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(401, obj!.StatusCode);
        }


        [TestMethod]
        public async Task UpdateMyScores_NullBody_Returns400()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            var userId = Guid.NewGuid();

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig(), userId);

            var action = await controller.UpdateMyScores(null!);

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(400, obj!.StatusCode);
        }

        [TestMethod]
        public async Task UpdateMyScores_DeleteAddUpdate_Works()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var games = TestHelpers.SeedGamesReturnEntities(ctx, "Snake", "Tetris", "Pacman");

            var userId = Guid.NewGuid();

            ctx.Users.Add(new User { Id = userId, UserName = "Elek", PasswordHash = "x", Role = "User" });

            // existing: Snake=10, Tetris=20 (Pacman nincs)
            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[0].Id, HighScore = 10u });

            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[1].Id, HighScore = 20u });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig(), userId);

            // incoming: Snake=111 (update), Pacman=333 (add), Tetris kimarad (delete)
            var incoming = new List<GameScoreDto>
            {

                new GameScoreDto { GameId = games[0].Id, GameName = games[0].Name, HighScore = 111 },

                new GameScoreDto { GameId = games[2].Id, GameName = games[2].Name, HighScore = 333 }

            };

            var action = await controller.UpdateMyScores(incoming);

            var ok = action as OkObjectResult;

            Assert.IsNotNull(ok, "Siker esetén Ok(...) várható.");

            var after = await ctx.UserHighScores.Where(s => s.UserId == userId).ToListAsync();

            Assert.AreEqual(2, after.Count, "Tetris törlődik, Pacman hozzáadódik -> 2 rekord marad.");

            var snake = after.Single(s => s.GameId == games[0].Id);

            Assert.AreEqual(111u, snake.HighScore);

            Assert.IsFalse(after.Any(s => s.GameId == games[1].Id), "Tetris törlődött.");

            Assert.IsTrue(after.Any(s => s.GameId == games[2].Id), "Pacman hozzáadódott.");
        }


        [TestMethod]
        public async Task UpdateMyScores_NegativeHighScore_BecomesHugeUint_BUGCATCH()
        {

            // Ez a teszt direkt "bug catcher": (uint)(-1) -> 4294967295
            // Ha ezt később javítod validációval, akkor a tesztet át kell írni (400-ra).
            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var games = TestHelpers.SeedGamesReturnEntities(ctx, "Snake");

            var userId = Guid.NewGuid();

            ctx.Users.Add(new User { Id = userId, UserName = "Elek", PasswordHash = "x", Role = "User" });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig(), userId);

            var incoming = new List<GameScoreDto>
            {

                new GameScoreDto { GameId = games[0].Id, GameName = "Snake", HighScore = -1 }

            };

            var action = await controller.UpdateMyScores(incoming);

            var ok = action as OkObjectResult;

            Assert.IsNotNull(ok);

            var saved = await ctx.UserHighScores.SingleAsync(s => s.UserId == userId && s.GameId == games[0].Id);

            Assert.IsTrue(saved.HighScore > 1000000u, "Negatívból uint overflow lett (ez jelenleg bug/edge).");
        }
    }
}
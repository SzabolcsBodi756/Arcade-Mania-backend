using Arcade_mania_backend_webAPI.Models;
using Arcade_mania_backend_webAPI.Models.Dtos.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_AdminCreateMoreTests
    {

        [TestMethod]
        public async Task CreateUserAdmin_DuplicateName_Returns409()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            ctx.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                UserName = "Elek",
                PasswordHash = "x",
                Role = "User"
            });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.CreateUserAdmin(new UserCreateAdminDto
            {
                Name = "Elek", // ugyanaz
                Password = "pass",
                Role = "User"
            });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(409, obj!.StatusCode);
        }


        [TestMethod]
        public async Task CreateUserAdmin_CreatesHighScoresForAllGames()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var games = TestHelpers.SeedGamesReturnEntities(ctx, "Snake", "Tetris", "Pacman");

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.CreateUserAdmin(new UserCreateAdminDto
            {
                Name = "  NewGuy  ",
                Password = "  pass123  ",
                Role = "User"
            });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(201, obj!.StatusCode);

            var user = await ctx.Users.SingleAsync(u => u.UserName == "NewGuy");

            var scoreCount = await ctx.UserHighScores.CountAsync(s => s.UserId == user.Id);

            Assert.AreEqual(games.Count, scoreCount, "CreateUserAdmin-nál is kell score rekord minden játékhoz.");

            Assert.IsTrue(await ctx.UserHighScores.AllAsync(s => s.HighScore == 0u), "Kezdő highscore mindenhol 0.");
        }
    }
}

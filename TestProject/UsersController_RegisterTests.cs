using Arcade_mania_backend_webAPI.Models;
using Arcade_mania_backend_webAPI.Models.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_RegisterTests
    {

        [TestMethod]
        public async Task Register_HappyPath_CreatesUser_AndCreatesScoresForAllGames()
        {

            // Arrange
            var dbName = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(dbName);

            TestHelpers.SeedGames(ctx, "Snake", "Tetris", "Pacman");

            var config = TestHelpers.CreateTestConfig();

            var controller = TestHelpers.CreateUsersController(ctx, config);

            var dto = new UserRegisterDto
            {
                Name = "TesztElek",
                Password = "pass123"
            };

            // Act
            var action = await controller.Register(dto);

            // Assert (HTTP)
            var obj = action as ObjectResult;

            Assert.IsNotNull(obj, "Register-nek ObjectResult-ot kell visszaadnia.");

            Assert.AreEqual(201, obj!.StatusCode, "Sikeres regisztrációnál 201-et várunk.");

            // Assert (DB)
            var user = await ctx.Users.SingleOrDefaultAsync(u => u.UserName == "TesztElek");

            Assert.IsNotNull(user, "A usernek létre kellett jönnie.");

            Assert.AreEqual("User", user!.Role, "Register után default role 'User' kell legyen.");

            Assert.IsFalse(string.IsNullOrWhiteSpace(user.PasswordHash), "PasswordHash nem lehet üres.");

            var gamesCount = await ctx.Games.CountAsync();

            var scoresCount = await ctx.UserHighScores.CountAsync(s => s.UserId == user.Id);

            Assert.AreEqual(gamesCount, scoresCount, "Minden Game-hez kell egy UserHighScore rekord 0-val.");

            Assert.IsTrue(await ctx.UserHighScores.AllAsync(s => s.HighScore == 0), "Kezdő highscore mindenhol 0 kell legyen.");
        }


        [TestMethod]
        public async Task Register_DuplicateName_Returns409()
        {

            // Arrange
            var dbName = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(dbName);

            TestHelpers.SeedGames(ctx, "Snake");

            // Létező user
            ctx.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                UserName = "DuplikaltNev",
                PasswordHash = "whatever",
                Role = "User"
            });

            await ctx.SaveChangesAsync();

            var config = TestHelpers.CreateTestConfig();

            var controller = TestHelpers.CreateUsersController(ctx, config);

            var dto = new UserRegisterDto
            {
                Name = "DuplikaltNev",
                Password = "pass123"
            };

            // Act
            var action = await controller.Register(dto);

            // Assert
            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(409, obj!.StatusCode, "Duplikált névnél 409-et várunk.");
        }
    }
}

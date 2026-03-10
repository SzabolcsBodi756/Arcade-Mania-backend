using Arcade_mania_backend_webAPI.Models;
using Arcade_mania_backend_webAPI.Models.Dtos.Scores;
using Arcade_mania_backend_webAPI.Models.Dtos.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_AdminCrudTests
    {

        [TestMethod]
        public async Task CreateUserAdmin_MissingNameOrPassword_Returns400()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.CreateUserAdmin(new UserCreateAdminDto { Name = "", Password = "", Role = "Admin" });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(400, obj!.StatusCode);
        }


        [TestMethod]
        public async Task CreateUserAdmin_RoleNormalize_AdminMixedCase_BecomesAdmin()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.CreateUserAdmin(new UserCreateAdminDto
            {
                Name = "  Elek  ",
                Password = "  pass  ",
                Role = "aDmIn"
            });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(201, obj!.StatusCode);

            var user = await ctx.Users.SingleAsync(u => u.UserName == "Elek");

            Assert.AreEqual("Admin", user.Role);
        }


        [TestMethod]
        public async Task UpdateUserAdmin_NotFound_Returns404()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.UpdateUserAdmin(Guid.NewGuid(), new UserUpdateAdminDto { Name = "X" });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(404, obj!.StatusCode);
        }


        [TestMethod]
        public async Task UpdateUserAdmin_NameConflict_Returns409()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var id1 = Guid.NewGuid();

            var id2 = Guid.NewGuid();

            ctx.Users.Add(new User { Id = id1, UserName = "Elek", PasswordHash = "x", Role = "User" });

            ctx.Users.Add(new User { Id = id2, UserName = "Bela", PasswordHash = "x", Role = "User" });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            // id2-t átneveznénk "Elek"-re -> 409
            var action = await controller.UpdateUserAdmin(id2, new UserUpdateAdminDto { Name = "Elek" });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(409, obj!.StatusCode);
        }


        [TestMethod]
        public async Task UpdateUserAdmin_ScoreSync_DeleteAddUpdate_Works()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var games = TestHelpers.SeedGamesReturnEntities(ctx, "Snake", "Tetris", "Pacman");

            var userId = Guid.NewGuid();

            ctx.Users.Add(new User { Id = userId, UserName = "Elek", PasswordHash = "x", Role = "User" });

            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[0].Id, HighScore = 10u });

            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[1].Id, HighScore = 20u });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var dto = new UserUpdateAdminDto
            {

                Scores = new List<UserUpdateScoreAdminDto>
                {
                    new UserUpdateScoreAdminDto { GameId = games[0].Id, HighScore = 111 }, // update

                    new UserUpdateScoreAdminDto { GameId = games[2].Id, HighScore = 333 }  // add
                    // Tetris hiányzik -> delete
                }
            };

            var action = await controller.UpdateUserAdmin(userId, dto);

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(200, obj!.StatusCode);

            var after = await ctx.UserHighScores.Where(s => s.UserId == userId).ToListAsync();

            Assert.AreEqual(2, after.Count);

            Assert.AreEqual(111u, after.Single(s => s.GameId == games[0].Id).HighScore);

            Assert.IsFalse(after.Any(s => s.GameId == games[1].Id));

            Assert.IsTrue(after.Any(s => s.GameId == games[2].Id));
        }


        [TestMethod]
        public async Task DeleteUserAdmin_RemovesUserAndScores_Returns200()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var games = TestHelpers.SeedGamesReturnEntities(ctx, "Snake");

            var userId = Guid.NewGuid();

            ctx.Users.Add(new User { Id = userId, UserName = "Elek", PasswordHash = "x", Role = "User" });

            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[0].Id, HighScore = 10u });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.DeleteUserAdmin(userId);

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(200, obj!.StatusCode);

            Assert.IsFalse(await ctx.Users.AnyAsync(u => u.Id == userId));

            Assert.IsFalse(await ctx.UserHighScores.AnyAsync(s => s.UserId == userId));
        }
    }
}

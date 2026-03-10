using Arcade_mania_backend_webAPI.Models;
using Arcade_mania_backend_webAPI.Models.Dtos.Scores;
using Arcade_mania_backend_webAPI.Models.Dtos.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NETCore.Encrypt;
using System.Security.Claims;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_AdminUpdateExtraEdgeTests
    {

        [TestMethod]
        public async Task UpdateUserAdmin_UpdatesPassword_AndAdminGetByIdReturnsDecryptedNewPassword()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            var cfg = TestHelpers.CreateTestConfig();

            var pwKey = cfg["Crypto:PasswordKey"]!;

            var userId = Guid.NewGuid();

            ctx.Users.Add(new User
            {
                Id = userId,
                UserName = "Elek",
                PasswordHash = EncryptProvider.AESEncrypt("oldpass", pwKey),
                Role = "User"
            });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            // Act: admin update password
            var update = await controller.UpdateUserAdmin(userId, new UserUpdateAdminDto { Password = "  newpass  " });

            var updObj = update as ObjectResult;

            Assert.IsNotNull(updObj);

            Assert.AreEqual(200, updObj!.StatusCode);

            // Assert: admin get by id -> decryptelt jelszó már newpass
            var get = await controller.GetUserAdminById(userId);

            var getObj = get as ObjectResult;

            Assert.IsNotNull(getObj);

            Assert.AreEqual(200, getObj!.StatusCode);

            var result = TestHelpers.GetAnonymousProp<object>(getObj.Value!, "result")!;

            var pass = (string?)result.GetType().GetProperty("Password")?.GetValue(result);

            Assert.AreEqual("newpass", pass);
        }


        [TestMethod]
        public async Task UpdateUserAdmin_RoleNormalize_AdminMixedCase_BecomesAdmin()
        {
            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            var cfg = TestHelpers.CreateTestConfig();

            var userId = Guid.NewGuid();

            ctx.Users.Add(new User { Id = userId, UserName = "Elek", PasswordHash = "x", Role = "User" });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            var action = await controller.UpdateUserAdmin(userId, new UserUpdateAdminDto { Role = "  aDmIn  " });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(200, obj!.StatusCode);

            var user = await ctx.Users.SingleAsync(u => u.Id == userId);

            Assert.AreEqual("Admin", user.Role);
        }


        [TestMethod]
        public async Task UpdateUserAdmin_RoleNormalize_UnknownRole_BecomesUser()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            var cfg = TestHelpers.CreateTestConfig();

            var userId = Guid.NewGuid();

            ctx.Users.Add(new User { Id = userId, UserName = "Elek", PasswordHash = "x", Role = "Admin" });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            var action = await controller.UpdateUserAdmin(userId, new UserUpdateAdminDto { Role = "Root" });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(200, obj!.StatusCode);

            var user = await ctx.Users.SingleAsync(u => u.Id == userId);

            Assert.AreEqual("User", user.Role, "Ismeretlen role esetén NormalizeRole -> User.");
        }


        [TestMethod]
        public async Task UpdateUserAdmin_NegativeHighScore_BecomesHugeUint_BUGCATCH()
        {

            // Bug catcher ugyanúgy, mint /me/scores-nál:
            // (uint)(-1) -> 4294967295
            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var games = TestHelpers.SeedGamesReturnEntities(ctx, "Snake");

            var userId = Guid.NewGuid();

            ctx.Users.Add(new User { Id = userId, UserName = "Elek", PasswordHash = "x", Role = "User" });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var dto = new UserUpdateAdminDto
            {

                Scores = new List<UserUpdateScoreAdminDto>
                {

                    new UserUpdateScoreAdminDto { GameId = games[0].Id, HighScore = -1 }

                }
            };

            var action = await controller.UpdateUserAdmin(userId, dto);

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(200, obj!.StatusCode);

            var saved = await ctx.UserHighScores.SingleAsync(s => s.UserId == userId && s.GameId == games[0].Id);

            Assert.IsTrue(saved.HighScore > 1_000_000u, "Negatív -> uint overflow (jelenleg edge/bug).");
        }
        

        [TestMethod]
        public async Task GetMyScores_NonGuidClaim_Returns401()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            var cfg = TestHelpers.CreateTestConfig();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            // kézzel felülírjuk a User claimet: NameIdentifier = "NOT-A-GUID"
            var httpContext = new DefaultHttpContext();

            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "NOT-A-GUID") }, "TestAuth"));

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var action = await controller.GetMyScores();

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(401, obj!.StatusCode);
        }
    }
}
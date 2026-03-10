using Arcade_mania_backend_webAPI.Models;
using Arcade_mania_backend_webAPI.Models.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NETCore.Encrypt;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_LoginTests
    {

        [TestMethod]
        public async Task Login_UserNotFound_Returns401()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.Login(new UserLoginDto { Name = "Nope", Password = "123" });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(401, obj!.StatusCode);
        }


        [TestMethod]
        public async Task Login_DecryptFails_Returns401()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            // Olyan "hash", amit az AESDecrypt nem tud visszafejteni -> catch -> 401
            ctx.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                UserName = "Elek",
                PasswordHash = "THIS_IS_NOT_AES",
                Role = "User"
            });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.Login(new UserLoginDto { Name = "Elek", Password = "123" });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(401, obj!.StatusCode);
        }


        [TestMethod]
        public async Task Login_WrongPassword_Returns401()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var games = TestHelpers.SeedGamesReturnEntities(ctx, "Snake", "Tetris");

            var cfg = TestHelpers.CreateTestConfig();

            var pwKey = cfg["Crypto:PasswordKey"]!;

            ctx.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                UserName = "Elek",
                PasswordHash = EncryptProvider.AESEncrypt("goodpass", pwKey),
                Role = "User"
            });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            var action = await controller.Login(new UserLoginDto { Name = "Elek", Password = "badpass" });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(401, obj!.StatusCode);
        }


        [TestMethod]
        public async Task Login_Success_Returns200_WithToken_AndScores()
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
                Role = "User"
            });

            // score rekordok (login visszaadja Join-olva)
            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[0].Id, HighScore = 11u });

            ctx.UserHighScores.Add(new UserHighScore { UserId = userId, GameId = games[1].Id, HighScore = 22u });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            var action = await controller.Login(new UserLoginDto { Name = "Elek", Password = "pass123" });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(200, obj!.StatusCode);

            // response anon object: { message, token, result }
            var token = TestHelpers.GetAnonymousProp<string>(obj.Value!, "token");

            Assert.IsFalse(string.IsNullOrWhiteSpace(token), "Sikeres login esetén token kötelező.");


            var result = TestHelpers.GetAnonymousProp<object>(obj.Value!, "result");

            Assert.IsNotNull(result);

            var scores = TestHelpers.GetAnonymousProp<object>(result!, "Scores");

            Assert.IsNotNull(scores, "Login resultnek tartalmaznia kell Scores-t.");
        }


        [TestMethod]
        public async Task Login_EmptyRole_DefaultsToUserTokenRole()
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
                PasswordHash = EncryptProvider.AESEncrypt("pass123", pwKey),
                Role = "" // -> controller: default "User"
            });

            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            var action = await controller.Login(new UserLoginDto { Name = "Elek", Password = "pass123" });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(200, obj!.StatusCode);

            var token = TestHelpers.GetAnonymousProp<string>(obj.Value!, "token")!;

            Assert.IsFalse(string.IsNullOrWhiteSpace(token));

            // JWT-ben legyen Role=User
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

            var jwt = handler.ReadJwtToken(token);

            var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            Assert.AreEqual("User", roleClaim);
        }
    }
}

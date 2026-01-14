using Arcade_mania_backend_webAPI.Models;
using Arcade_mania_backend_webAPI.Models.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NETCore.Encrypt;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_AdminLoginTests
    {
        [TestMethod]
        public async Task AdminLogin_NotAdmin_Returns403()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);
            TestHelpers.SeedGames(ctx, "Snake");

            var cfg = TestHelpers.CreateTestConfig();
            var pwKey = cfg["Crypto:PasswordKey"]!;

            ctx.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                UserName = "Elek",
                PasswordHash = EncryptProvider.AESEncrypt("pass123", pwKey),
                Role = "User"
            });
            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            var action = await controller.AdminLogin(new UserLoginDto { Name = "Elek", Password = "pass123" });

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(403, obj!.StatusCode);
        }

        [TestMethod]
        public async Task AdminLogin_Admin_Success_Returns200_WithAdminRoleToken()
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
                UserName = "Admin",
                PasswordHash = EncryptProvider.AESEncrypt("pass123", pwKey),
                Role = "Admin"
            });
            await ctx.SaveChangesAsync();

            var controller = TestHelpers.CreateUsersController(ctx, cfg);

            var action = await controller.AdminLogin(new UserLoginDto { Name = "Admin", Password = "pass123" });

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(200, obj!.StatusCode);

            var token = TestHelpers.GetAnonymousProp<string>(obj.Value!, "token")!;
            Assert.IsFalse(string.IsNullOrWhiteSpace(token));

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            Assert.AreEqual("Admin", roleClaim);
        }
    }
}

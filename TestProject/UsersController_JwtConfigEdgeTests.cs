using Arcade_mania_backend_webAPI.Models;
using Arcade_mania_backend_webAPI.Models.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NETCore.Encrypt;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_JwtConfigEdgeTests
    {
        private static IConfiguration CreateConfigMissing(string missingKey)
        {
            var baseCfg = TestHelpers.CreateTestConfig();

            // Új configot építünk, amiből kihagyunk 1 JWT kulcsot
            var dict = new Dictionary<string, string?>
            {
                ["Crypto:PasswordKey"] = baseCfg["Crypto:PasswordKey"],
                ["Jwt:Key"] = baseCfg["Jwt:Key"],
                ["Jwt:Issuer"] = baseCfg["Jwt:Issuer"],
                ["Jwt:Audience"] = baseCfg["Jwt:Audience"],
                ["Jwt:ExpireMinutes"] = baseCfg["Jwt:ExpireMinutes"],
                ["Admin:ServiceKey"] = baseCfg["Admin:ServiceKey"]
            };

            dict.Remove(missingKey);

            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        [TestMethod]
        public async Task Login_MissingJwtKey_Returns400()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);
            TestHelpers.SeedGames(ctx, "Snake");

            var cfg = CreateConfigMissing("Jwt:Key");
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

            var action = await controller.Login(new UserLoginDto { Name = "Elek", Password = "pass123" });

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(400, obj!.StatusCode, "Jwt:Key hiányában a GenerateJwtToken dob -> outer try/catch -> 400.");
        }

        [TestMethod]
        public async Task Login_MissingJwtIssuer_Returns400()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);
            TestHelpers.SeedGames(ctx, "Snake");

            var cfg = CreateConfigMissing("Jwt:Issuer");
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

            var action = await controller.Login(new UserLoginDto { Name = "Elek", Password = "pass123" });

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(400, obj!.StatusCode);
        }

        [TestMethod]
        public async Task Login_MissingJwtAudience_Returns400()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);
            TestHelpers.SeedGames(ctx, "Snake");

            var cfg = CreateConfigMissing("Jwt:Audience");
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

            var action = await controller.Login(new UserLoginDto { Name = "Elek", Password = "pass123" });

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(400, obj!.StatusCode);
        }

        [TestMethod]
        public async Task Login_InvalidExpireMinutes_FallsBackTo60_AndTokenHasExp()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);
            TestHelpers.SeedGames(ctx, "Snake");

            var baseCfg = TestHelpers.CreateTestConfig();
            var dict = new Dictionary<string, string?>
            {
                ["Crypto:PasswordKey"] = baseCfg["Crypto:PasswordKey"],
                ["Jwt:Key"] = baseCfg["Jwt:Key"],
                ["Jwt:Issuer"] = baseCfg["Jwt:Issuer"],
                ["Jwt:Audience"] = baseCfg["Jwt:Audience"],
                ["Jwt:ExpireMinutes"] = "not-a-number", // -> int.TryParse false -> 60
            };
            var cfg = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

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

            var action = await controller.Login(new UserLoginDto { Name = "Elek", Password = "pass123" });

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(200, obj!.StatusCode);

            var token = TestHelpers.GetAnonymousProp<string>(obj.Value!, "token")!;
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // exp claimnek léteznie kell
            var exp = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            Assert.IsFalse(string.IsNullOrWhiteSpace(exp), "Tokenben exp claimnek lennie kell.");
        }
    }
}

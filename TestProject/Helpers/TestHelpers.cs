using Arcade_mania_backend_webAPI.Controllers;
using Arcade_mania_backend_webAPI.Models;
using Arcade_mania_backend_webAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace TestProject.Helpers
{
    internal static class TestHelpers
    {

        public static ArcadeManiaDatasContext CreateInMemoryContext(string dbName)
        {

            var options = new DbContextOptionsBuilder<ArcadeManiaDatasContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ArcadeManiaDatasContext(options);
        }


        public static void SeedGames(ArcadeManiaDatasContext ctx, params string[] gameNames)
        {

            // Ha nem seedelünk Game-eket, a Register/CreateAdmin nem tud HighScore rekordokat létrehozni.
            foreach (var name in gameNames)
            {

                ctx.Games.Add(new Game
                {
                    Id = Guid.NewGuid(),
                    Name = name
                });

            }

            ctx.SaveChanges();
        }


        public static IConfiguration CreateTestConfig()
        {

            // Ezeket a UsersController használja:
            var dict = new Dictionary<string, string?>
            {
                ["Crypto:PasswordKey"] = "0123456789ABCDEF0123456789ABCDEF", // 32 chars, stabil teszthez
                ["Jwt:Key"] = "TEST_JWT_KEY_0123456789_TEST_JWT_KEY_0123456789",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpireMinutes"] = "60",
                ["Admin:ServiceKey"] = "TEST_SERVICE_KEY"
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();
        }


        public static UsersController CreateUsersController(

            ArcadeManiaDatasContext ctx,
            IConfiguration config,

            Guid? authenticatedUserId = null)
        {
            var jwtService = new JwtService(config);

            var controller = new UsersController(ctx, config, jwtService);

            // Ha kell "bejelentkezett user" (pl. /me/scores tesztekhez), beállítjuk a HttpContext.User claim-et.
            var httpContext = new DefaultHttpContext();

            if (authenticatedUserId.HasValue)
            {
                var claims = new[]

                {
                    new Claim(ClaimTypes.NameIdentifier, authenticatedUserId.Value.ToString())

                };

                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            }

            controller.ControllerContext = new ControllerContext
            {

                HttpContext = httpContext

            };

            return controller;
        }


        public static T? GetAnonymousProp<T>(object? obj, string propName)
        {
            if (obj == null)
            {
                return default;
            }

            var prop = obj.GetType().GetProperty(propName);

            if (prop == null)
            {
                return default;
            }

            var val = prop.GetValue(obj);

            if (val is T t)
            {
                return t;
            }

            return default;
        }


        public static List<Game> SeedGamesReturnEntities(ArcadeManiaDatasContext ctx, params string[] gameNames)
        {

            var games = new List<Game>();

            foreach (var name in gameNames)
            {

                var g = new Game { Id = Guid.NewGuid(), Name = name };

                games.Add(g);

                ctx.Games.Add(g);
            }

            ctx.SaveChanges();

            return games;
        }
    }
}

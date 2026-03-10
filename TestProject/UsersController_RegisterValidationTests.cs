using Arcade_mania_backend_webAPI.Models.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_RegisterValidationTests
    {

        [TestMethod]
        public async Task Register_EmptyName_Returns400()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.Register(new UserRegisterDto { Name = "", Password = "pass" });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(400, obj!.StatusCode);
        }


        [TestMethod]
        public async Task Register_EmptyPassword_Returns400()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.Register(new UserRegisterDto { Name = "Elek", Password = "" });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(400, obj!.StatusCode);
        }


        [TestMethod]
        public async Task Register_WhitespaceNameOrPassword_Returns400()
        {

            var db = Guid.NewGuid().ToString();

            using var ctx = TestHelpers.CreateInMemoryContext(db);

            TestHelpers.SeedGames(ctx, "Snake");

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.Register(new UserRegisterDto { Name = "   ", Password = "   " });

            var obj = action as ObjectResult;

            Assert.IsNotNull(obj);

            Assert.AreEqual(400, obj!.StatusCode);
        }
    }
}
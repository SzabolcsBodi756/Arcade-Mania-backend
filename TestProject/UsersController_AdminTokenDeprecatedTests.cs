using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_AdminTokenDeprecatedTests
    {
        [TestMethod]
        public void GetAdminToken_Returns410_Gone()
        {
            var db = System.Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var json = JsonDocument.Parse("{\"serviceKey\":\"whatever\"}").RootElement;

            var action = controller.GetAdminToken(json);

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(410, obj!.StatusCode);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProject.Helpers;

namespace TestProject
{
    [TestClass]
    public class UsersController_AdminDeleteNotFoundTests
    {
        [TestMethod]
        public async Task DeleteUserAdmin_NotFound_Returns404()
        {
            var db = Guid.NewGuid().ToString();
            using var ctx = TestHelpers.CreateInMemoryContext(db);

            var controller = TestHelpers.CreateUsersController(ctx, TestHelpers.CreateTestConfig());

            var action = await controller.DeleteUserAdmin(Guid.NewGuid());

            var obj = action as ObjectResult;
            Assert.IsNotNull(obj);
            Assert.AreEqual(404, obj!.StatusCode);
        }
    }
}

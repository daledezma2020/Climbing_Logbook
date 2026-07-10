using System.Security.Claims;
using api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api.Tests.Unit;

public class AuthControllerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void MeReturnsTheAuthenticatedUsersProfileClaims()
    {
        var controller = new AuthController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "auth0|123"),
                        new Claim("name", "Alex Climber"),
                        new Claim(ClaimTypes.Email, "alex@example.test")
                    ], "test"))
                }
            }
        };

        var response = Assert.IsType<OkObjectResult>(controller.Me());
        var values = response.Value!.GetType().GetProperties()
            .ToDictionary(property => property.Name, property => property.GetValue(response.Value));

        Assert.Equal("auth0|123", values["subject"]);
        Assert.Equal("Alex Climber", values["name"]);
        Assert.Equal("alex@example.test", values["email"]);
    }
}

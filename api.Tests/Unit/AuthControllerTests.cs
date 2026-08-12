using System.Security.Claims;
using api.Controllers;
using api.DTO;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api.Tests.Unit;

public class AuthControllerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task MeReturnsThePersistedUserProvisionedFromTheToken()
    {
        var userService = new UserServiceStub
        {
            User = new AppUser
            {
                Id = 7,
                Auth0Subject = "auth0|123",
                Username = "alex",
                DisplayName = "Alex Climber",
                Email = "alex@example.test",
                PictureUrl = "https://example.test/alex.png"
            }
        };
        var controller = BuildController(userService);

        var response = Assert.IsType<OkObjectResult>((await controller.Me(CancellationToken.None)).Result);
        var profile = Assert.IsType<CurrentUserDto>(response.Value);

        Assert.Equal(7, profile.Id);
        Assert.Equal("alex", profile.Username);
        Assert.Equal("Alex Climber", profile.DisplayName);
        Assert.Equal("alex@example.test", profile.Email);
        Assert.Equal("https://example.test/alex.png", profile.PictureUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MeProvisionsTheCallerFromTheirOwnClaimsPrincipal()
    {
        var userService = new UserServiceStub { User = new AppUser { Id = 1, Username = "alex", DisplayName = "Alex" } };
        var controller = BuildController(userService);

        await controller.Me(CancellationToken.None);

        Assert.Equal("auth0|123", userService.LastPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MeDoesNotExposeTheAuth0Subject()
    {
        Assert.Null(typeof(CurrentUserDto).GetProperty("Auth0Subject"));
    }

    private static AuthController BuildController(IUserService userService) => new(userService)
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
}

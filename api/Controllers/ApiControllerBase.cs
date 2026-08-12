using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected const int DefaultPageSize = 25;

    // These endpoints serve visitors and signed-in users alike. EnsureUserAsync throws
    // without a subject claim, so it is only reached once the request is authenticated.
    protected async Task<AppUser?> TryGetCallerAsync(
        IUserService userService,
        CancellationToken cancellationToken)
    {
        return User.Identity?.IsAuthenticated == true
            ? await userService.EnsureUserAsync(User, cancellationToken)
            : null;
    }
}

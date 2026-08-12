using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly ISocialService _socialService;

    public CommentsController(IUserService userService, ISocialService socialService)
    {
        _userService = userService;
        _socialService = socialService;
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken cancellationToken)
    {
        var caller = await _userService.EnsureUserAsync(User, cancellationToken);
        var result = await _socialService.DeleteCommentAsync(caller.Id, id, cancellationToken);

        return result switch
        {
            CommentDeletion.Deleted => NoContent(),
            CommentDeletion.NotFound => NotFound(),
            _ => Forbid()
        };
    }
}

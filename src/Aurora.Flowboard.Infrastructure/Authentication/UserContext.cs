using Microsoft.AspNetCore.Http;

namespace Aurora.Flowboard.Infrastructure.Authentication;

internal sealed class UserContext(IHttpContextAccessor contextAccessor) : IUserContext
{
    public Guid UserId => contextAccessor.HttpContext?.User.GetUserId() ?? throw new AuroraFlowboardException("User context is not available.");
}

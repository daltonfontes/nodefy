using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Nodefy.Api.Auth;

public class CurrentUserAccessor
{
    private readonly IHttpContextAccessor _http;
    public CurrentUserAccessor(IHttpContextAccessor http) { _http = http; }

    public Guid? UserId
    {
        get
        {
            var v = _http.HttpContext?.User?.FindFirst("sub")?.Value
                    ?? _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(v, out var id) ? id : null;
        }
    }

    public string? Email => _http.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
}

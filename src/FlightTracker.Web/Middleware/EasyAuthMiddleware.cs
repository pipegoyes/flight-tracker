using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace FlightTracker.Web.Middleware;

/// <summary>
/// Middleware to integrate with Azure App Service Easy Auth.
/// Reads the X-MS-CLIENT-PRINCIPAL header and populates HttpContext.User.
/// </summary>
public class EasyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EasyAuthMiddleware> _logger;

    public EasyAuthMiddleware(RequestDelegate next, ILogger<EasyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if Easy Auth header is present
        if (context.Request.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var principalHeader))
        {
            try
            {
                // Decode the base64-encoded JSON
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(principalHeader!));
                var principal = JsonSerializer.Deserialize<EasyAuthPrincipal>(decoded, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (principal != null)
                {
                    // Create claims from the Easy Auth principal
                    var claims = new List<Claim>();

                    // Add user ID and name
                    if (!string.IsNullOrEmpty(principal.UserId))
                    {
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, principal.UserId));
                    }

                    if (!string.IsNullOrEmpty(principal.UserDetails))
                    {
                        claims.Add(new Claim(ClaimTypes.Name, principal.UserDetails));
                    }

                    // Add identity provider
                    if (!string.IsNullOrEmpty(principal.IdentityProvider))
                    {
                        claims.Add(new Claim("identityProvider", principal.IdentityProvider));
                    }

                    // Add all user claims from Easy Auth
                    if (principal.Claims != null)
                    {
                        foreach (var claim in principal.Claims)
                        {
                            claims.Add(new Claim(claim.Type, claim.Value));
                        }
                    }

                    // Create the identity and principal
                    var identity = new ClaimsIdentity(claims, principal.IdentityProvider ?? "EasyAuth");
                    var claimsPrincipal = new ClaimsPrincipal(identity);

                    // Set the user
                    context.User = claimsPrincipal;

                    _logger.LogDebug("Easy Auth user authenticated: {UserId} via {Provider}",
                        principal.UserId, principal.IdentityProvider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Easy Auth principal header");
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Model for Easy Auth principal data
    /// </summary>
    private class EasyAuthPrincipal
    {
        public string? AuthenticationType { get; set; }
        public string? IdentityProvider { get; set; }
        public string? UserId { get; set; }
        public string? UserDetails { get; set; }
        public List<EasyAuthClaim>? Claims { get; set; }
    }

    /// <summary>
    /// Model for Easy Auth claim
    /// </summary>
    private class EasyAuthClaim
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}

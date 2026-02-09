using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[Route("[controller]/[action]")]
public class CultureController : Controller
{
    public IActionResult SetCulture(string culture, string redirectUri)
    {
        if (culture != null)
        {
            // Persistir la selección de cultura en una cookie
            HttpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(culture, culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                }
            );
        }

        // Prevenir open redirect attacks
        if (!Url.IsLocalUrl(redirectUri))
        {
            redirectUri = "/";
        }

        return LocalRedirect(redirectUri);
    }
}
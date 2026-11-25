using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace RailwayTickets.Controllers;

public class LanguageController : Controller
{
    [HttpPost]
    public IActionResult Set(string language, string? returnUrl)
    {
        string culture = language == "en" ? "en-US" : "uk-UA";
        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Path = "/", HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true,
                Secure = Request.IsHttps, MaxAge = TimeSpan.FromDays(365) });
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}

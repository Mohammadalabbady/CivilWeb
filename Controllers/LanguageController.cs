using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CivilWeb.Controllers;

[AllowAnonymous] // Language switching should work for everyone
public class LanguageController : Controller
{
    [HttpGet]
    [Route("Language/Change")]
    public IActionResult Change(string lang, string returnUrl = "/")
    {
        return SetLanguage(lang, returnUrl);
    }

    [HttpGet]
    public IActionResult SetLanguage(string lang, string returnUrl = "/")
    {
        // Set language cookie (expires in 1 year)
        Response.Cookies.Append("Language", lang, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            Path = "/"
        });

        // Redirect back to the previous page
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Login", "Account");
    }
}

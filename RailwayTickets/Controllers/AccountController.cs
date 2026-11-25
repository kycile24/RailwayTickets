using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RailwayTickets.Data;
using RailwayTickets.Models;
using RailwayTickets.Services;
using System.Security.Claims;

namespace RailwayTickets.Controllers;

public class AccountController(RailwayDbContext db) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login() => View();

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(string login, string password, bool staff)
    {
        if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
        {
            if (staff)
            {
                var employee = db.Employees.SingleOrDefault(e => e.Login == login);
                if ((employee is Cashier || employee is Administrator) && Access.Matches(employee.Password, password))
                    return await Start(employee.Id, employee.FullName, employee is Cashier ? "Cashier" : "Administrator");
            }
            else
            {
                var passenger = db.Passengers.SingleOrDefault(p => p.AccountLogin == login);
                if (passenger?.PasswordHash != null && Access.Matches(passenger.PasswordHash, password))
                    return await Start(passenger.Id, passenger.FullName, "Client");
            }
        }
        ModelState.AddModelError("", "Перевірте логін, пароль і тип облікового запису.");
        return View();
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View();

    [AllowAnonymous, HttpPost]
    public IActionResult Register(string login, string password, string fullName, string phone, string document)
    {
        if (string.IsNullOrWhiteSpace(login) || login.Length > 100 || string.IsNullOrWhiteSpace(password)
            || password.Length < 8 || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone)
            || string.IsNullOrWhiteSpace(document) || db.Passengers.Any(p => p.AccountLogin == login.Trim()))
        {
            ModelState.AddModelError("", "Заповніть усі поля, оберіть вільний логін та пароль від 8 символів.");
            return View();
        }
        db.Passengers.Add(new Passenger { AccountLogin = login.Trim(), PasswordHash = Access.Hash(password),
            FullName = fullName.Trim(), Phone = phone.Trim(), Document = document.Trim() });
        db.SaveChanges();
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult Denied() => StatusCode(403, "Доступ для цієї ролі заборонено.");

    private async Task<IActionResult> Start(int id, string name, string role)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, name), new Claim(ClaimTypes.Role, role) }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
        return RedirectToAction("Index", role switch { "Administrator" => "Admin", "Cashier" => "Cashier", _ => "Trips" });
    }
}

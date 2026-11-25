using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailwayTickets.Data;
using RailwayTickets.Models;

var builder = WebApplication.CreateBuilder(args);
bool demo = builder.Configuration["Database:Mode"] == "Demo";
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp =>
{
    var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=/tmp/railwaytickets-demo.db;Foreign Keys=True");
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    connection.CreateFunction("railway_actor", () =>
    {
        var user = accessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true ? $"{user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value} #{user.AccountId()}" : "Система";
    });
    return connection;
});
builder.Services.AddDbContext<RailwayDbContext>((services, options) =>
{
    if (demo) options.UseSqlite(services.GetRequiredService<Microsoft.Data.Sqlite.SqliteConnection>());
    else options.UseSqlServer(builder.Configuration.GetConnectionString("RailwayConnection"));
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Denied";
    options.Events.OnValidatePrincipal = async context =>
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<RailwayDbContext>();
        var user = context.Principal!;
        var id = user.AccountId();
        bool exists = user.IsInRole("Client")
            ? await db.Passengers.AnyAsync(p => p.Id == id && p.AccountLogin != null)
            : user.IsInRole("Cashier") ? await db.Cashiers.AnyAsync(c => c.Id == id)
            : await db.Administrators.AnyAsync(a => a.Id == id);
        if (!exists) context.RejectPrincipal();
    };
});
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<AtomicWrite>();
});
var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("uk-UA").AddSupportedCultures("uk-UA", "en-US")
    .AddSupportedUICultures("uk-UA", "en-US"));
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RailwayDbContext>();
    if (demo) db.Database.EnsureCreated();
    else db.Database.Migrate();
    using var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
    if (demo)
    {
        DemoAudit.Initialize(db);
        DbSeeder.Seed(db, Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
    }
    else if (app.Environment.IsDevelopment() && builder.Configuration["Demo:Password"] is { Length: >= 8 } demoPassword)
        DbSeeder.Seed(db, demoPassword);
    foreach (var employee in db.Employees.Where(e => !e.Password.StartsWith("v1:")))
        employee.Password = Access.Hash(employee.Password);
    if (!db.Administrators.Any())
    {
        var password = builder.Configuration["Bootstrap:AdminPassword"]
            ?? throw new InvalidOperationException("Встановіть Bootstrap__AdminPassword.");
        db.Administrators.Add(new Administrator { FullName = "Адміністратор", Login = "admin", Password = Access.Hash(password) });
    }
    db.SaveChanges();
    transaction.Commit();
}
app.Run();

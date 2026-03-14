using AudioDBByBlazor.Components;
using AudioDBByBlazor.Data;
using AudioDBByBlazor.Models;
using AudioDBByBlazor.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=audiodb.db"));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
});

builder.Services.AddScoped<FavorisService>();
builder.Services.AddHttpClient<AudioDbService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ── Endpoint Login (POST) ──────────────
app.MapPost("/account/login", async (
    HttpContext context,
    SignInManager<AppUser> signInManager,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string? returnUrl) =>
{
    var result = await signInManager.PasswordSignInAsync(email, password, false, false);
    if (result.Succeeded){
    return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
    return Results.Redirect("/login?error=1");
}).DisableAntiforgery();;

// ── Endpoint Register (POST) ────────────────────────────────
app.MapPost("/account/register", async (
    HttpContext context,
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string? displayName) =>
{
    var user = new AppUser
    {
        UserName = email,
        Email = email,
        DisplayName = displayName
    };

    var result = await userManager.CreateAsync(user, password);
    if (result.Succeeded)
    {
         await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/");
    }

    var errors = string.Join(",", result.Errors.Select(e => e.Code));
    return Results.Redirect($"/register?error={Uri.EscapeDataString(errors)}");
}).DisableAntiforgery();;

// ── Endpoint Logout (POST) ────────────────────────────────────────────────
app.MapPost("/account/logout", async (SignInManager<AppUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
}).DisableAntiforgery();;

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

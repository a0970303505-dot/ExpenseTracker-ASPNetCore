using expense_tracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// 資料庫連線
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DevConnection")));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false; 
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4; // 密碼只要4碼
});



var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseAuthentication();


app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
   
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

using _Zannat_Mirza__Lab3;
using _Zannat_Mirza__Lab3.Models;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register DynamoDBHelper
builder.Services.AddSingleton<DynamoDBHelper>();

// Configure AWS service with credentials from appsettings.json
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions("AWS"));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// default route Login page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Application}/{action=Login}/{id?}");
// route Home Page
app.MapControllerRoute(
    name: "home",
    pattern: "Home",
    defaults: new { controller = "Home", action = "Index" });

//route Register Page
app.MapControllerRoute(
    name: "register",
    pattern: "Register",
    defaults: new { controller = "Application", action = "Register" });

app.Run();

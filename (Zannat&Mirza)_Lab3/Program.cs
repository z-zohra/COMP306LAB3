using _Zannat_Mirza__Lab3;
using _Zannat_Mirza__Lab3.Models;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the Helper class for DynamoDB and S3 connections
builder.Services.AddSingleton<Helper>();

// Register DynamoDBService with dependency on the Helper class
builder.Services.AddScoped<DynamoDBService>(); // Scoped makes it per-request, change to Singleton if needed

// Register S3Service
builder.Services.AddSingleton<S3Service>();


// Configure AWS service with credentials from appsettings.json
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions("AWS"));


// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout as needed
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Enable session middleware
app.UseSession();

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

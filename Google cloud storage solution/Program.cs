using Google_cloud_storage_solution.Databases;
using Google_cloud_storage_solution.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<GoogleStorageDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("GoogleStorageDatabaseConnection")));
builder.Services.AddSingleton<ICloudStorageService>(new GoogleCloudStorageService("kms_cloud_storage"));
builder.Services.AddSingleton<GoogleDriveService>();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

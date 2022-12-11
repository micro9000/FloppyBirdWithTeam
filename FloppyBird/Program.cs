using FloppyBird;
using FloppyBird.Cache;
using FloppyBird.Data;
using FloppyBird.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(config =>
{
    config.EnableDetailedErrors = true;
});

builder.Services.Configure<RedisConfigOptions>(builder.Configuration.GetSection(nameof(RedisConfigOptions)));
builder.Services.AddSingleton(async x => await RedisConnection.InitializeAsync(connectionString: builder.Configuration.GetConnectionString("RedisCacheConnString")));

builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ISessionUsersRepository, SessionUsersRepository>();

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

app.MapHub<ChatHub>("/chathub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

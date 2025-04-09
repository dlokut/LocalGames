using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContextFactory<ServerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("sqlite")));

builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ServerDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//Get the role manager to use in create roles method
var roleManager = app.Services.CreateScope()
    .ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

await CreateAllRolesAsync(roleManager);

app.Run();


async Task CreateAllRolesAsync(RoleManager<IdentityRole> roleManager)
{
    await CreateRoleAsync(roleManager, "Admin");
}

async Task CreateRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
{
    if (await roleManager.RoleExistsAsync(roleName)) return;

    var newRole = new IdentityRole(roleName);
    var createResult = await roleManager.CreateAsync(newRole);

    if (!createResult.Succeeded)
    {
        foreach (var error in createResult.Errors)
        {
            app.Logger.LogError($"Error when creating role {roleName} - {error.Description}");
        }
    }
}

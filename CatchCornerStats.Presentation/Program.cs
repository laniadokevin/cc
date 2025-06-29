using CatchCornerStats.Core.Interfaces;
using CatchCornerStats.Data.Repositories;
using CatchCornerStats.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(180)
    ));
builder.Services.AddRazorPages();

builder.Services.AddScoped<IStatsRepository, StatsRepository>();
builder.Services.AddScoped<IArenaRepository, ArenaRepository>();
builder.Services.AddScoped<IArenaLinkRepository, ArenaLinkRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IListingRepository, ListingRepository>();
builder.Services.AddScoped<INeighborhoodRepository, NeighborhoodRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();

builder.Services.AddSwaggerGen();

// Configure CORS to allow requests from the Web project
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                // Web project ports
                "http://localhost:5069",
                "https://localhost:7032",
                "http://localhost:45332",
                "https://localhost:44347",
                // Development ports
                "http://localhost:3000",
                "http://localhost:5000",
                "https://localhost:5001",
                // Additional ports for development
                "http://localhost:5044",
                "https://localhost:7254"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS - must be called before UseRouting
app.UseCors();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapRazorPages();

app.Run();

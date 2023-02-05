using Microsoft.AspNetCore.Hosting;
using Sample.WebApp;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<LogMiddleware>();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();

using Merjane.Application;
using Merjane.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IProductNotificationPort, ProductNotificationAdapter>();
builder.Services.AddScoped<IOrderProcessingStore, EfOrderProcessingStore>();
builder.Services.AddScoped<ProductDecisionExecutor>();
builder.Services.AddScoped<OrderProcessingService>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    _ = options.UseInMemoryDatabase($"InMemoryDb");
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
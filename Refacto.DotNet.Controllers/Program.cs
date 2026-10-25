using Merjane.Application;
using Merjane.Application.Ports;
using Merjane.Infrastructure;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IProductNotificationPort, ProductNotificationAdapter>();
builder.Services.AddScoped<ProductDecisionExecutor>();
builder.Services.AddScoped<OrderProcessingService>();
builder.Services.AddInfrastructure();

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
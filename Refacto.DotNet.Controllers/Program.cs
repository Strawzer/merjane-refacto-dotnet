using Merjane.Application;
using Merjane.Application.Ports;
using Merjane.Infrastructure;
using Merjane.Infrastructure.Persistence;
using Microsoft.OpenApi;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Merjane Order Processing API",
        Version = "v1",
        Description = "Processes customer orders: applies the availability rule of each ordered product, updates stock and sends customer notifications.",
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Refacto.DotNet.Controllers.xml"));
});
// The protected notification service is exposed to the application layer through its port adapter.
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

    // Opt-in demo data for manual testing; the in-memory database starts empty otherwise.
    if (app.Configuration.GetValue<bool>("DemoData:Seed"))
    {
        using IServiceScope scope = app.Services.CreateScope();
        _ = DemoDataSeeder.SeedDemoOrder(
            scope.ServiceProvider.GetRequiredService<AppDbContext>(),
            scope.ServiceProvider.GetRequiredService<IClock>().Now);
    }
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
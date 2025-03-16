using Microsoft.EntityFrameworkCore;
using Polly;
using Wpm.Clinic.Application;
using Wpm.Clinic.DataAccess;
using Wpm.Clinic.ExternalServices;
using Wpm.Clinic.IntegrationsEvents;
using Wpm.Clinic.SAGA;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ClinicDBContext>(options =>
{
    options.UseInMemoryDatabase("WpmClinic");
});

builder.Services.AddScoped<ManagementService>();

builder.Services.AddScoped<ClinicApplicationService>();
builder.Services.AddScoped<ConsulationSagaOrchestrator>();
builder.Services.AddHostedService<ConsulationCompensationHandler>();
builder.Services.AddHttpClient<ManagementService>(client =>
{
    var url = builder.Configuration.GetValue<string>("Wpm:ManagementUri");
    client.BaseAddress = new Uri(url);
}).AddResilienceHandler("management-pipeline", builder =>
{
    builder.AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>()
    {
        BackoffType = DelayBackoffType.Exponential,
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(10)
    });
});


var app = builder.Build();
app.EnsureDbIsCreated();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

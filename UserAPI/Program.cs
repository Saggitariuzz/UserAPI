using Prometheus;
using UserAPI.Services;
using UserAPI.Services.Impl;
using UserAPI.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<KafkaUserProcessingService>();

builder.Services.Configure<UsersDBSettings>(
    builder.Configuration.GetSection("UsersDB"));
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection("KafkaConfig"));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserAPI V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseRouting();
app.UseHttpMetrics();
app.MapMetrics();

app.MapControllers();

app.Run();

public partial class Program { }
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Prometheus;
using UserAPI.Services;
using UserAPI.Services.Impl;
using UserAPI.Settings;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.JsonWebTokens;

var builder = WebApplication.CreateBuilder(args);

var kafkaSettingsSection = builder.Configuration.GetSection("KafkaConfig");
builder.Services.Configure<KafkaSettings>(kafkaSettingsSection);
var kafkaSettings = kafkaSettingsSection.Get<KafkaSettings>();

builder.Services.AddSingleton<IProducer<Null, string>>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;
    var config = new ProducerConfig
    {
        BootstrapServers = settings.BootstrapServers,
        Acks = Acks.All,
        EnableIdempotence = true,
        LingerMs = 5
    };
    return new ProducerBuilder<Null, string>(config).Build();
});

// Add services to the container.
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<ITokenBlackListService, TokenBlackListService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Please enter token",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});
builder.Services.AddHostedService<KafkaUserProcessingService>();

builder.Services.Configure<UsersDBSettings>(
    builder.Configuration.GetSection("UsersDB"));
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection("KafkaConfig"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = AuthOptions.ISSUER,
            ValidateAudience = true,
            ValidAudience = AuthOptions.AUDIENCE,
            ValidateLifetime = true,
            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
            ValidateIssuerSigningKey = true
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var blacklistService = context.HttpContext.RequestServices.GetRequiredService<ITokenBlackListService>();
                var token = ((JsonWebToken)context.SecurityToken).EncodedToken;
                if (await blacklistService.IsTokenBlackListedAsync(token))
                {
                    context.Fail("Token is blacklisted: user logged out");
                }
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "";
});

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

//app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


app.UseHttpMetrics();
app.MapMetrics();

app.MapControllers();

app.Run();

public partial class Program { }
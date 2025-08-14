using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Text;
using TurfAuthAPI.Config;
using TurfAuthAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// MongoDB configuration
builder.Services.Configure<MongoDbSettings>(options =>
{
    var mongoConn = Environment.GetEnvironmentVariable("MONGO_CONNECTION");
    options.ConnectionString = string.IsNullOrEmpty(mongoConn)
                               ? builder.Configuration.GetSection("MongoDbSettings:ConnectionString").Value
                               : mongoConn;
    options.DatabaseName = builder.Configuration.GetSection("MongoDbSettings:DatabaseName").Value;
});

// Read MongoDB settings and test connection immediately
var mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION") ??
                            builder.Configuration.GetSection("MongoDbSettings:ConnectionString").Value;
var mongoDbName = builder.Configuration.GetSection("MongoDbSettings:DatabaseName").Value;

Console.WriteLine($"📦 Using MongoDB connection string: {mongoConnectionString}");
Console.WriteLine($"📂 Target Database: {mongoDbName}");

try
{
    var client = new MongoClient(mongoConnectionString);
    client.ListDatabaseNames(); // test query
    Console.WriteLine("✅ MongoDB connection successful.");
}
catch (Exception ex)
{
    Console.WriteLine("❌ MongoDB connection failed: " + ex.Message);
}

// Add services
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<TokenService>();

builder.Services.AddControllers();

// Configure JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Turf Booking Auth API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    };

    var securityReq = new OpenApiSecurityRequirement
    {
        { securityScheme, new[] { "Bearer" } }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(securityReq);
});

builder.WebHost.UseUrls("http://0.0.0.0:5000"); // matches Docker EXPOSE
var app = builder.Build();

// Redirect root to Swagger UI
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Turf Booking Auth API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

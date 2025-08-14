using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TurfAuthAPI.Config;
using TurfAuthAPI.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Read Mongo settings early and test connection
var mongoConn = Environment.GetEnvironmentVariable("MONGO_CONNECTION");
var mongoConnectionString = string.IsNullOrEmpty(mongoConn)
    ? builder.Configuration.GetSection("MongoDbSettings:ConnectionString").Value
    : mongoConn;

var mongoDbName = builder.Configuration.GetSection("MongoDbSettings:DatabaseName").Value;

try
{
    Console.WriteLine($"[Startup] Testing MongoDB connection to: {mongoConnectionString}, DB: {mongoDbName}");
    var client = new MongoClient(mongoConnectionString);
    client.ListDatabaseNames(); // test connection
    Console.WriteLine("✅ MongoDB connection test successful.");
}
catch (Exception ex)
{
    Console.WriteLine("❌ MongoDB connection test failed: " + ex);
    throw; // fail fast so we see the error in logs
}

// Configure MongoDbSettings for DI
builder.Services.Configure<MongoDbSettings>(options =>
{
    options.ConnectionString = mongoConnectionString;
    options.DatabaseName = mongoDbName;
});

// Add services
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<TokenService>();

builder.Services.AddControllers();

// JWT setup
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

builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

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

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OnlineBookingSystem.Api.Configuration;
using OnlineBookingSystem.Api.Security;
using OnlineBookingSystem.Shared.Configuration;
using OnlineBookingSystem.Shared.Data;
using OnlineBookingSystem.Shared.Repositories;
using OnlineBookingSystem.Shared.Security;
using OnlineBookingSystem.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Forward headers (important for hosting)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
  options.KnownNetworks.Clear();
  options.KnownProxies.Clear();
});

// ✅ Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
      options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
      options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
      options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
      options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// ✅ Swagger (ADDED)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
  options.SwaggerDoc("v1", new OpenApiInfo
  {
    Title = "Online Booking API",
    Version = "v1"
  });

  // Optional: JWT support in Swagger
  options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Enter 'Bearer YOUR_TOKEN'"
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
      new string[] {}
    }
  });
});

// ✅ JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Configure Jwt:Key in appsettings.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
      o.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
        ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(JwtKeyMaterial.GetSigningKeyBytes(jwtKey)),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = System.Security.Claims.ClaimTypes.Name,
      };
    });

// ✅ DATABASE (migrations live in API assembly; DbContext in Shared)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("OnlineBookingSystem.Api")));

// ✅ SERVICES
builder.Services.Configure<SmsSettings>(builder.Configuration.GetSection("SmsSettings"));
builder.Services.AddHttpClient("SmsGateway", client =>
{
  client.Timeout = TimeSpan.FromSeconds(60);
  client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "CommunityHallBooking/1.0");
});

builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IBookingSystemRepository, BookingSystemRepository>();
builder.Services.Configure<ProvisioningOptions>(builder.Configuration.GetSection(ProvisioningOptions.SectionName));
// Local dev: if appsettings.Development.json clears MintKey, provisioning UI mint still works.
builder.Services.PostConfigure<ProvisioningOptions>(opts =>
{
  if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(opts.MintKey))
  {
    opts.MintKey = "Test@123";
  }
});
builder.Services.AddSingleton<ProvisioningRateLimiter>();
builder.Services.AddSingleton<ProvisioningMintRateLimiter>();
builder.Services.AddScoped<IVisitorService, VisitorService>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<OfficeAuthService>();

// ✅ CORS — explicit origins when Cors:AllowedOrigins is non-empty (production); empty = AllowAnyOrigin (same-origin monolith OK)
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var corsOriginsTrimmed = corsOrigins
    .Select(o => o?.Trim())
    .Where(o => !string.IsNullOrEmpty(o))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
  options.AddPolicy("AppCors", policy =>
  {
    if (corsOriginsTrimmed.Length > 0)
    {
      policy.WithOrigins(corsOriginsTrimmed)
          .AllowAnyMethod()
          .AllowAnyHeader();
    }
    else
    {
      policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
    }
  });
});

var app = builder.Build();

if (app.Environment.IsProduction())
{
  var prodJwt = app.Configuration["Jwt:Key"] ?? "";
  if (prodJwt.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) || prodJwt.Trim().Length < 32)
  {
    throw new InvalidOperationException(
      "Production requires a strong Jwt:Key (32+ characters, not the placeholder). Set the Jwt__Key environment variable or appsettings.Production.json.");
  }
}

// EF Core migrations: Development only. Production/staging use SQL-scripted schema; Migrate() there causes "object already exists" and startup failures.
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  var startupLog = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");
  try
  {
    if (app.Environment.IsDevelopment())
    {
      var pending = db.Database.GetPendingMigrations().ToList();
      if (pending.Count > 0)
      {
        startupLog.LogInformation("Applying {Count} pending EF Core migration(s)…", pending.Count);
        try
        {
          db.Database.Migrate();
          startupLog.LogInformation("EF Core migrations applied.");
        }
        catch (Exception ex) when (IsSqlDuplicateObjectError(ex))
        {
          startupLog.LogWarning(ex,
            "EF migrations were not applied: objects already exist (typical for databases created from SQL scripts). Using existing schema. For an empty new database run: dotnet ef database update --project backend/OnlineBookingSystem.Api");
        }
      }
      else
      {
        startupLog.LogInformation("No pending EF Core migrations.");
      }
    }
    else
    {
      startupLog.LogInformation("Skipping EF Core Migrate(); not Development — using existing database schema.");
    }

    ProvisioningSchemaGuard.EnsureSuperAdminProvisioningToken(db);
    startupLog.LogInformation("SuperAdminProvisioningToken table verified.");

    // Script-built DBs often predate email/password registration; add missing columns + filtered unique index on Email.
    RegisteredUserSchemaGuard.EnsureAccountColumns(db);
    startupLog.LogInformation("RegisteredUser account columns verified (Email, PasswordHash, …).");

    VenueMasterWriteGuard.EnsureSqlServerVenueWritesWork(db, startupLog);
    startupLog.LogInformation("VenueMaster / VenueType write prerequisites verified.");
  }
  catch (Exception ex)
  {
    startupLog.LogCritical(ex,
      "Database startup failed. Check ConnectionStrings:DefaultConnection and SQL Server availability.");
    throw;
  }
}

static bool IsSqlDuplicateObjectError(Exception ex)
{
  for (Exception? e = ex; e != null; e = e.InnerException)
  {
    if (e is SqlException sqlEx && sqlEx.Number == 2714)
    {
      return true;
    }
  }

  return false;
}

// ✅ Middleware (forwarded headers + HTTPS redirect only outside local dev — avoids breaking http://localhost)
if (!app.Environment.IsDevelopment())
{
  app.UseForwardedHeaders();
}

app.UseCors("AppCors");

if (!app.Environment.IsDevelopment())
{
  app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

var enableSwagger = app.Environment.IsDevelopment()
    || app.Configuration.GetValue("Hosting:EnableSwagger", false);
if (enableSwagger)
{
  app.UseSwagger();
  app.UseSwaggerUI(c =>
  {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Online Booking API V1");
    c.RoutePrefix = "swagger";
  });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
using RetirementPlanner.Auth;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

// Throws here, before anything else starts, if Jwt:SigningKey is missing or too short.
var jwtOptions = JwtOptions.Load(builder.Configuration);

// CORS for the Angular dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Credentials are allowed so the browser sends the refresh-token cookie to /api/auth.
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Data access, repositories and services
builder.Services.AddRetirementPlanner(connectionString);

// JWT authentication (required on every endpoint unless [AllowAnonymous]) and the auth rate limit
builder.Services.AddJwtAuth(jwtOptions, builder.Configuration);

// Unhandled exceptions become RFC 7807 ProblemDetails
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Controllers: every request DTO is validated by FluentValidation before the action runs.
// Implicit [Required] for non-nullable strings is off, so FluentValidation owns all request rules.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationFilter>();
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Services.GetRequiredService<IDatabaseMigrator>().Migrate();

if (app.Environment.IsDevelopment())
{
    await using (var scope = app.Services.CreateAsyncScope())
    {
        await scope.ServiceProvider.GetRequiredService<IDataSeeder>().SeedAsync();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS runs first so its headers are also added to error responses from the exception handler.
app.UseCors("AllowFrontend");
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();

// Lets the integration tests host the API with WebApplicationFactory<Program>.
public partial class Program;

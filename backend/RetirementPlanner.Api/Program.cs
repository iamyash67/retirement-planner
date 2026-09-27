using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

// CORS for the Angular dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Data access, repositories and services
builder.Services.AddRetirementPlanner(connectionString);

// Unhandled exceptions become RFC 7807 ProblemDetails
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Swagger & Controllers
builder.Services.AddControllers();
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
app.UseAuthorization();
app.MapControllers();

app.Run();

using RetirementPlanner.Services;
using RetirementPlanner.Services.Interfaces;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Repositories;

var builder = WebApplication.CreateBuilder(args);

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

// Repositories
builder.Services.AddScoped<IGoalRepository, GoalRepository>();
builder.Services.AddScoped<IFinancialYearDataRepo, FinancialYearDataRepo>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IFinancialYearDataService, FinancialYearDataService>();

// Swagger & Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();

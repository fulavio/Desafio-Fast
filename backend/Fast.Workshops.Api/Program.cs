using Fast.Workshops.Api.Controllers;
using Fast.Workshops.Api.Repositories;
using Fast.Workshops.Api.Repositories.InMemory;
using Fast.Workshops.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>())
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = BindingProblem.Create);
builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddSingleton<InMemoryDatabase>();
builder.Services.AddScoped<IWorkshopRepository, InMemoryWorkshopRepository>();
builder.Services.AddScoped<ICollaboratorRepository, InMemoryCollaboratorRepository>();
builder.Services.AddScoped<IAttendanceRecordRepository, InMemoryAttendanceRecordRepository>();
builder.Services.AddScoped<WorkshopService>();
builder.Services.AddScoped<CollaboratorService>();
builder.Services.AddScoped<AttendanceRecordService>();
builder.Services.AddScoped<DevelopmentSeed>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<DevelopmentSeed>().Populate();
}
app.Run();

/// <summary>Entry point exposed to the integration test host; e.g. WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;

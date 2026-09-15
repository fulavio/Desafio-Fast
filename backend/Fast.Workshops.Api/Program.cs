using Fast.Workshops.Api.Controllers;
using Fast.Workshops.Api.Repositories;
using Fast.Workshops.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>())
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = BindingProblem.Create);
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "FAST Workshops API",
        Version = "v1",
        Description = "Cadastro de workshops, colaboradores e atas de presença. Armazenamento configurável em memória ou MySQL."
    });
    options.EnableAnnotations();
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
var usesMySql = builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddScoped<WorkshopService>();
builder.Services.AddScoped<CollaboratorService>();
builder.Services.AddScoped<AttendanceRecordService>();
builder.Services.AddScoped<DevelopmentSeed>();

var app = builder.Build();
if (usesMySql) PersistenceRegistration.VerifyMySql(app.Services);
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "FAST Workshops API v1"));
    if (!usesMySql)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<DevelopmentSeed>().Populate();
    }
}
app.Run();

/// <summary>Entry point exposed to the integration test host; e.g. WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;

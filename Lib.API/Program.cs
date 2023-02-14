using FluentValidation;
using Lib.API.Data;
using Lib.API.Endpoints.Internal;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "./wwwroot"
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IDbConnectionFactory>(
    _ => new SqliteConnectionFactory(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddEndpoints<Program>(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseEndpoints<Program>();

var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();
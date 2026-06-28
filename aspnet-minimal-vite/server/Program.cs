using System.Text.Json.Serialization;
using Server;
using Server.Features.Todos;
using Server.Shared.Codegen;

// Modo codegen: `dotnet run -- generate` emite o generated.ts e sai (sem servidor).
if (args.Contains("generate"))
{
    TsGenerator.Run(ApiEndpoints.All);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Serializa enums como string (batem com as unions geradas no TS).
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Libera o dev server do Vite a chamar a API.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.MapTodos();

app.Run();

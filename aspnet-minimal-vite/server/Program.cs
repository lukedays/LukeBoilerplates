using System.Text.Json.Serialization;
using Server.Features.Codegen;
using Server.Features.Todos;

// Modo codegen: `dotnet run -- generate` emite o generated.ts e sai (sem servidor).
if (args.Contains("generate"))
{
    TsGenerator.Run(ApiEndpoints.All);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Armazenamento em memória: uma instância por processo.
builder.Services.AddSingleton<TodoStore>();

// Serializa enums como string (batem com as unions geradas no TS).
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
);

// Libera o dev server do Vite a chamar a API.
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:5100").AllowAnyHeader().AllowAnyMethod()
    )
);

var app = builder.Build();

app.UseCors();
app.MapTodos();

app.Run();

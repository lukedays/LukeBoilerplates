using Server.Shared.Codegen;

namespace Server.Features.Todos;

// DTOs do slice. records -> interfaces no generated.ts.
public record TodoDto(int Id, string Title, bool Done);

public record CreateTodoRequest(string Title);

// Feature de exemplo: store em memória só para demonstrar o fluxo de codegen.
// Numa app real, troque por EF Core / repositório.
public static class TodosApi
{
    // Manifesto consumido pelo codegen. Mantenha em sincronia com MapTodos.
    public static IReadOnlyList<EndpointDef> Endpoints =>
    [
        new("getTodos", "GET", "/api/todos", null, typeof(TodoDto[])),
        new("getTodo", "GET", "/api/todos/{id}", null, typeof(TodoDto)),
        new("createTodo", "POST", "/api/todos", typeof(CreateTodoRequest), typeof(TodoDto)),
    ];

    private static readonly List<TodoDto> Store =
    [
        new(1, "Estudar TanStack Query", true),
        new(2, "Conectar front ao back", false),
    ];

    public static void MapTodos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/todos", () => Results.Ok(Store));

        app.MapGet("/api/todos/{id}", (int id) =>
            Store.FirstOrDefault(t => t.Id == id) is { } todo
                ? Results.Ok(todo)
                : Results.NotFound());

        app.MapPost("/api/todos", (CreateTodoRequest req) =>
        {
            var nextId = Store.Count == 0 ? 1 : Store.Max(t => t.Id) + 1;
            var todo = new TodoDto(nextId, req.Title, false);
            Store.Add(todo);
            return Results.Created($"/api/todos/{todo.Id}", todo);
        });
    }
}

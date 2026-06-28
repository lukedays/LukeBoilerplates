using Microsoft.EntityFrameworkCore;
using Server.Shared.Codegen;
using Server.Shared.Data;

namespace Server.Features.Todos;

// DTOs do slice (contrato da API). records -> interfaces no generated.ts.
public record TodoDto(int Id, string Title, bool Done);

public record CreateTodoRequest(string Title);

public static class TodosApi
{
    // Manifesto consumido pelo codegen. Mantenha em sincronia com MapTodos.
    public static IReadOnlyList<EndpointDef> Endpoints =>
    [
        new("getTodos", "GET", "/api/todos", null, typeof(TodoDto[])),
        new("getTodo", "GET", "/api/todos/{id}", null, typeof(TodoDto)),
        new("createTodo", "POST", "/api/todos", typeof(CreateTodoRequest), typeof(TodoDto)),
    ];

    public static void MapTodos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/todos", async (AppDbContext db) =>
            await db.Todos
                .Select(t => new TodoDto(t.Id, t.Title, t.Done))
                .ToListAsync());

        app.MapGet("/api/todos/{id}", async (int id, AppDbContext db) =>
            await db.Todos.FindAsync(id) is { } t
                ? Results.Ok(new TodoDto(t.Id, t.Title, t.Done))
                : Results.NotFound());

        app.MapPost("/api/todos", async (CreateTodoRequest req, AppDbContext db) =>
        {
            var todo = new Todo { Title = req.Title };
            db.Todos.Add(todo);
            await db.SaveChangesAsync();
            return Results.Created($"/api/todos/{todo.Id}", new TodoDto(todo.Id, todo.Title, todo.Done));
        });
    }
}

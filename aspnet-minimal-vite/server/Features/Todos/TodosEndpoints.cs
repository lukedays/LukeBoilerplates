using Server.Features.Codegen;

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
        app.MapGet(
            "/api/todos",
            (TodoStore store) => store.All().Select(t => new TodoDto(t.Id, t.Title, t.Done))
        );

        app.MapGet(
            "/api/todos/{id}",
            (int id, TodoStore store) =>
                store.Find(id) is { } t
                    ? Results.Ok(new TodoDto(t.Id, t.Title, t.Done))
                    : Results.NotFound()
        );

        app.MapPost(
            "/api/todos",
            (CreateTodoRequest req, TodoStore store) =>
            {
                var todo = store.Add(req.Title);
                return Results.Created(
                    $"/api/todos/{todo.Id}",
                    new TodoDto(todo.Id, todo.Title, todo.Done)
                );
            }
        );
    }
}

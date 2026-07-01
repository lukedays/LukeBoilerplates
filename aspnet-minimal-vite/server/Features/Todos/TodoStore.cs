using System.Collections.Concurrent;

namespace Server.Features.Todos;

// Armazenamento em memória (sem banco). Thread-safe para requests concorrentes.
// O estado vive enquanto o processo roda; reiniciar zera e re-semeia.
public sealed class TodoStore
{
    private readonly ConcurrentDictionary<int, Todo> _todos = new();
    private int _lastId;

    public TodoStore()
    {
        Add("Estudar TanStack Query", done: true);
        Add("Conectar front ao back", done: false);
    }

    public IReadOnlyList<Todo> All() => _todos.Values.OrderBy(t => t.Id).ToList();

    public Todo? Find(int id) => _todos.GetValueOrDefault(id);

    public Todo Add(string title, bool done = false)
    {
        var id = Interlocked.Increment(ref _lastId);
        var todo = new Todo
        {
            Id = id,
            Title = title,
            Done = done,
        };
        _todos[id] = todo;
        return todo;
    }
}

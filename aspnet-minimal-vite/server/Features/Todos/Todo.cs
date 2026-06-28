namespace Server.Features.Todos;

// Entidade persistida (EF Core). Separada do TodoDto, que é o contrato da API.
public class Todo
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool Done { get; set; }
}

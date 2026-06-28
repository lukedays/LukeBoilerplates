using Microsoft.EntityFrameworkCore;
using Server.Features.Todos;

namespace Server.Shared.Data;

// Contexto único agregando as entidades das features.
// Adicionou uma feature com persistência? Inclua o DbSet dela aqui.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Todo> Todos => Set<Todo>();
}

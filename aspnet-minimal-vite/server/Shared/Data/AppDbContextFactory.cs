using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Server.Shared.Data;

// Usado pelo `dotnet ef` (migrations) para criar o contexto sem subir a aplicação,
// evitando que o Migrate()/seed do Program.cs rode em tempo de design.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=app.db")
            .Options;
        return new AppDbContext(options);
    }
}

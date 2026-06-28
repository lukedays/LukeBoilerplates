# aspnet-minimal-vite

ASP.NET Core minimal API (.NET 10) + EF Core/SQLite + Vite (React + TS), com **tipos e hooks
do TanStack Query gerados automaticamente do backend** — sem OpenAPI, por reflection direto nos
tipos C#.

## Estrutura

```
server/                       # backend .NET (minimal API, vertical slices)
  Program.cs                  # composition root + branch `generate` do codegen
  ApiEndpoints.cs             # agrega o manifesto de cada slice
  HotReloadCodegen.cs         # regenera o TS no hot reload (dotnet watch)
  Shared/Codegen/             # o gerador (TsGenerator + EndpointDef)
  Shared/Data/                # AppDbContext + factory de design-time (EF Core)
  Migrations/                 # migrations do EF Core (versionadas)
  Features/<Slice>/           # cada feature: entidade + DTOs + manifesto + map
client/                       # frontend Vite React+TS
  src/api/generated.ts        # AUTOGERADO — não editar à mão
dev.ps1                       # sobe API + Vite lado a lado
```

## Persistência (EF Core + SQLite)

- `Shared/Data/AppDbContext.cs` — contexto único; cada feature com persistência adiciona o seu
  `DbSet`. A entidade vive na feature (ex.: `Features/Todos/Todo.cs`), separada do DTO da API.
- Connection string em `appsettings.json` (`Data Source=app.db`). O `app.db` não é versionado.
- No startup, `db.Database.Migrate()` cria/atualiza o banco e popula se estiver vazio.
- Mudou alguma entidade? Gere a migration:

```sh
dotnet ef migrations add <Nome> --project server
```

## Como funciona o codegen

1. Cada slice expõe `static IReadOnlyList<EndpointDef> Endpoints` — o **manifesto** (nome do
   hook, método, rota, tipo do request e do response).
2. `ApiEndpoints.All` agrega os manifestos de todas as features.
3. `TsGenerator.Run` percorre os tipos por reflection e emite `client/src/api/generated.ts`:
   `interface` por DTO, `union` por enum, e um hook por endpoint
   (`useQuery` para GET, `useMutation` para o resto).
4. Dispara em três caminhos: `dotnet run -- generate` (manual / `npm run gen`),
   target MSBuild `GenerateTs` (todo build), e `HotReloadCodegen` (hot reload).

Como lê os tipos C# direto, o TS bate 100% com o JSON real (mesma política camelCase do ASP.NET).

## Adicionar uma feature

1. Crie `server/Features/<Slice>/<Slice>Endpoints.cs` com os DTOs, a lista `Endpoints` e o
   `Map<Slice>(this IEndpointRouteBuilder)`. Mantenha o manifesto em sincronia com o map.
2. Inclua `<Slice>Api.Endpoints` em `server/ApiEndpoints.cs` e `app.Map<Slice>()` em `Program.cs`.
3. Se a feature persiste dados: crie a entidade, adicione o `DbSet` em `AppDbContext` e gere a
   migration (`dotnet ef migrations add ...`).
4. Os hooks aparecem em `generated.ts` no próximo build / hot reload.

## Como rodar

```sh
./dev.ps1                 # API + Vite no Windows Terminal (hot reload + regeneração)
./dev.ps1 -Mode Background   # ambos em background; logs em .dev-logs/. Pare com -Mode Stop
```

API em `http://localhost:5070`, front em `http://localhost:5173`. Sobrescreva a base da API
no front via `VITE_API_URL`.

# aspnet-minimal-vite

ASP.NET Core minimal API (.NET 10) + armazenamento em memória + Vite (React + TS + Tailwind),
com **tipos e hooks do TanStack Query gerados automaticamente do backend** — sem OpenAPI, por
reflection direto nos tipos C#.

## Estrutura

```
server/                       # backend .NET (minimal API, vertical slices)
  Program.cs                  # composition root + branch `generate` do codegen
  Features/Codegen/           # o codegen inteiro: gerador, manifesto agregado e hot reload
    TsGenerator.cs            #   gerador (TsGenerator + EndpointDef)
    ApiEndpoints.cs           #   agrega o manifesto de cada slice
    HotReloadCodegen.cs       #   regenera o TS no hot reload (dotnet watch)
  Features/<Slice>/           # cada feature de API: modelo + store + DTOs + manifesto + map
client/                       # frontend Vite React+TS (Tailwind; lint via oxlint, não ESLint)
  src/api/generated.ts        # AUTOGERADO — não editar à mão
dev.ps1                       # sobe API + Vite lado a lado
```

## Armazenamento (em memória)

- Sem banco: cada feature com estado expõe um store em memória registrado como singleton
  (ex.: `Features/Todos/TodoStore.cs`, um `ConcurrentDictionary` thread-safe).
- O modelo de armazenamento vive na feature (ex.: `Features/Todos/Todo.cs`), separado do DTO
  da API. O seed inicial roda no construtor do store.
- O estado dura enquanto o processo roda; reiniciar zera e re-semeia.

## Como funciona o codegen

Todo o codegen vive em `server/Features/Codegen/`.

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
2. Se a feature guarda estado, crie um `<Slice>Store` em memória e registre com
   `builder.Services.AddSingleton<<Slice>Store>()` no `Program.cs`.
3. Inclua `<Slice>Api.Endpoints` em `Features/Codegen/ApiEndpoints.cs` e `app.Map<Slice>()`
   em `Program.cs`.
4. Os hooks aparecem em `generated.ts` no próximo build / hot reload.

## Como rodar

```sh
./dev.ps1                 # API + Vite no Windows Terminal (hot reload + regeneração)
./dev.ps1 -Mode Background   # ambos em background; logs em .dev-logs/. Pare com -Mode Stop
```

API em `http://localhost:5101`, front em `http://localhost:5100`. Sobrescreva a base da API
no front via `VITE_API_URL`.

## Formatação

```sh
./format.ps1              # backend com CSharpier, frontend com Prettier
```

O CSharpier é tool local (`dotnet-tools.json`); o `format.ps1` faz `dotnet tool restore` antes.

Os comandos exatos de regeneração via CLI ficam em `REGEN.md`.

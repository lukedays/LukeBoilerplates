# REGEN — aspnet-minimal-vite

Comandos exatos para regenerar a saída pura dos CLIs. Rode-os no branch `upstream`,
de dentro desta pasta, após apagar `server/` e `client/`.

## Comandos

```sh
# de dentro de aspnet-minimal-vite/

# backend — ASP.NET Core webapi minimal (sem controllers)
dotnet new webapi -o server

# frontend — Vite React + TypeScript
npx --yes create-vite@latest client --template react-ts
cd client && npm install && cd ..
```

## Versões da última regeneração

| Componente            | Versão       |
| --------------------- | ------------ |
| .NET SDK              | 10.0.109     |
| TargetFramework       | net10.0      |
| create-vite           | latest       |
| vite                  | ^8.1.0       |
| react / react-dom     | ^19.2.7      |
| typescript            | ~6.0.2       |
| @vitejs/plugin-react  | ^6.0.2       |
| oxlint                | ^1.69.0      |

Atualize esta tabela sempre que regenerar.

## Notas

- O template novo do Vite usa **oxlint** no lugar do ESLint.
- `npm install` não é feito pelo create-vite; roda manualmente após o scaffold.
- Customizações (proxy do Vite pro backend, CORS, etc.) vivem só no branch `main`.

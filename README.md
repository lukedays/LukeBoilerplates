# LukeBoilerplates

Monorepo de boilerplates de arquiteturas, cada um derivado da saída de um CLI oficial e
customizado por cima. O git é usado para manter as customizações separadas do upstream, de
forma que re-rodar o CLI (quando há atualização) reaplique só o que mudou, sem perder as
customizações.

## Boilerplates

- `aspnet-minimal-vite/` — ASP.NET Core webapi minimal (.NET) + Vite (React + TS)

## Modelo de branches

- `upstream` — saída **pura** dos CLIs. Nunca editar arquivo gerado à mão aqui.
- `main` — `upstream` + customizações. É onde você trabalha.

Arquivos de infra do monorepo (`.gitignore`, `README.md`, cada `REGEN.md`) existem nos dois
branches; são editados só na `main`.

## Atualizar um boilerplate quando o CLI evolui

Regenere **apenas a pasta que atualizou** (o merge fica restrito a ela):

```sh
# 1. vai pro upstream
git checkout upstream

# 2. apaga o conteúdo GERADO da pasta (mantém o REGEN.md)
#    ex.: rm -rf aspnet-minimal-vite/server aspnet-minimal-vite/client

# 3. re-roda os comandos exatos do REGEN.md da pasta
#    (atualize a versão registrada no REGEN.md se mudou)

# 4. commita o novo upstream
git add -A && git commit -m "vendor(<pasta>): regen via CLI <versao>"

# 5. volta pra main e mergeia
git checkout main
git merge upstream
```

O 3-way merge resolve sozinho tudo que você não tocou; conflito só aparece onde uma
customização sua colide com mudança real do upstream.

## Adicionar um novo boilerplate

1. `git checkout upstream`
2. Gera o scaffold via CLI numa nova subpasta + cria o `REGEN.md` dela (comandos + versões).
3. `git commit` no `upstream`.
4. `git checkout main && git merge upstream`.
5. Customiza na `main`.

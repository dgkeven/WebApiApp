# Deploy na Vercel

Este projeto usa `Dockerfile.vercel`, o formato que a Vercel usa para rodar
qualquer app que fale HTTP (incluindo ASP.NET Core) em containers, sobre o
Fluid Compute deles.

## 1. Criar o Blob Store

1. No dashboard da Vercel, abra o projeto → aba **Storage** → **Create Database** → **Blob**.
2. Depois de criado, a Vercel adiciona automaticamente a variável de ambiente
   `BLOB_READ_WRITE_TOKEN` ao projeto (Production e Preview).

## 2. Rodar localmente com o mesmo token

```bash
vercel env pull .env.local
```

Ou defina manualmente antes de rodar:

```bash
export BLOB_READ_WRITE_TOKEN="seu_token_aqui"
dotnet run
```

## 3. Deploy

```bash
vercel deploy
```

A Vercel detecta o `Dockerfile.vercel`, builda a imagem, sobe pro registry do
projeto e publica em Fluid Compute automaticamente.

## Observações importantes

- **Containers são stateless na Vercel**: por isso os PDFs vão para a Vercel
  Blob (armazenamento externo), não para disco local — o disco do container
  é apagado a cada novo deploy ou quando a instância escala a zero por
  inatividade.
- **Não existe SDK oficial da Vercel Blob para .NET.** O `VercelBlobService.cs`
  chama a API REST diretamente (o mesmo formato usado pelo pacote `@vercel/blob`
  e pela Vercel CLI). Se algum endpoint parar de bater, compare o comportamento
  rodando `vercel blob put/list/del` pela CLI.
- O app escuta na porta definida pela variável `PORT`, que a Vercel injeta
  automaticamente nos containers.

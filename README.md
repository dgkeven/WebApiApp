# WebApiApp

API em C# (.NET 10, Minimal API) com upload e download de arquivos PDF, usando **Vercel Blob** como armazenamento.

## Funcionalidades

- Upload de arquivos PDF
- Listagem dos arquivos enviados
- Download direto pela CDN da Vercel Blob
- Exclusão de arquivos
- Página web simples (`/`) com botão de upload e lista de download
- Documentação interativa via Swagger (`/swagger`, ambiente de desenvolvimento)

## Estrutura do projeto

```
WebApiApp/
├── Program.cs                    # Configuração e endpoints da API
├── Models/
│   ├── BlobFile.cs                # Metadados de um arquivo armazenado
│   └── WeatherForecast.cs         # Endpoint de exemplo (/weatherforecast)
├── Services/
│   └── VercelBlobService.cs       # Cliente HTTP para a API REST da Vercel Blob
├── wwwroot/
│   └── index.html                 # Página de upload/download
├── Dockerfile.vercel              # Build/execução do container na Vercel
├── DEPLOY-VERCEL.md               # Passo a passo de deploy
├── appsettings.json
└── Properties/launchSettings.json
```

## Pré-requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) 10.0 ou superior
- Uma conta na [Vercel](https://vercel.com) com um **Blob Store** criado (Storage → Create Database → Blob)

## Rodando localmente

1. Pegue o token do seu Blob Store (dashboard da Vercel → Storage → seu store → `.env.local`), ou rode:

   ```bash
   vercel env pull .env.local
   ```

2. Exporte o token e rode a aplicação:

   ```bash
   export BLOB_READ_WRITE_TOKEN="seu_token_aqui"
   dotnet run
   ```

3. Acesse `http://localhost:5117` para a página de upload/download, ou `http://localhost:5117/swagger` para a documentação da API.

## Endpoints

| Método | Rota             | Descrição                                  |
|--------|------------------|---------------------------------------------|
| POST   | `/files/upload`  | Envia um PDF (`multipart/form-data`, campo `file`) |
| GET    | `/files`         | Lista os arquivos enviados                  |
| DELETE | `/files?url=...` | Remove um arquivo pela URL retornada no upload/listagem |
| GET    | `/weatherforecast` | Endpoint de exemplo (dados aleatórios)    |

## Deploy

Veja o passo a passo completo em [`DEPLOY-VERCEL.md`](./DEPLOY-VERCEL.md).

Resumo:

```bash
vercel deploy
```

A Vercel detecta o `Dockerfile.vercel`, builda a imagem e publica automaticamente em Fluid Compute.

## Observações

- Os containers na Vercel são **stateless**: por isso os PDFs vão para a Vercel Blob (armazenamento externo) em vez do disco local do container.
- Não existe SDK oficial da Vercel Blob para .NET — `VercelBlobService.cs` chama a API REST diretamente, no mesmo formato usado pelo pacote `@vercel/blob` e pela Vercel CLI.

# ConsultaTemperaturasApi 🌡️

**ConsultaTemperaturasApi** é uma API RESTful para consultar e analisar dados de temperaturas de diversas cidades e anos, utilizando bancos de dados SQLite. A API consulta a viabilidade de plantação para uma cultura específica utilizando as informações sobre as temperaturas mínimas e máximas de uma cidade em um determinado ano obtidas de um banco de dados sqlite (dados obtidos pelo INMET).

## Sumário

- [Funcionalidades](#funcionalidades)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Estrutura de Diretórios](#estrutura-de-diretórios)
- [Como Rodar o Projeto](#como-rodar-o-projeto)
- [Endpoints](#endpoints)
- [Swagger](#swagger)
- [Melhorias Futuras](#melhorias-futuras)
- [Contribuindo](#contribuindo)
- [Licença](#licença)

## Funcionalidades

- **Consulta de Temperaturas**: Permite consultar as temperaturas mínimas e máximas de uma cidade por ano.
- **Suporte a Vários Anos**: Possibilita adicionar novas bases de dados para diferentes anos sem a necessidade de modificar o código.
- **Flexibilidade de Consulta**: Consultas dinâmicas baseadas em cidade e ano.
- **Documentação Interativa**: Utiliza o Swagger para facilitar a visualização e testes dos endpoints.

## Tecnologias Utilizadas

- **.NET 9** (C#)
- **ASP.NET Core** (para a criação da API)
- **Entity Framework Core** (acesso aos bancos de dados SQLite)
- **SQLite** (armazenamento dos dados de temperatura)
- **Swagger** (documentação e testes dos endpoints)
- **Git** (controle de versão)

## Estrutura de Diretórios

```bash
ConsultaTemperaturasApi/
│
├── Controllers/                     # Controladores da API
│   └── ConsultaController.cs        # Lógica da API de consulta de temperaturas
│
├── databases/                       # Banco de dados de temperaturas
│   └── temperaturas/                # Dados de temperatura por cidade e ano
│
├── obj/                             # Arquivos gerados pelo processo de build
│
├── bin/                             # Arquivos binários do projeto
│
├── appsettings.json                 # Configurações gerais do aplicativo
├── appsettings.Development.json     # Configurações específicas para desenvolvimento
├── Program.cs                       # Inicialização do aplicativo
├── ConsultaTemperaturasApi.csproj   # Arquivo de configuração do projeto
└── TODO.md                          # Melhorias futuras
```

### Requisitos

- **.NET 9** ou superior instalado.
- **SQLite** (o banco de dados é gerado automaticamente).

### Passos para Executar

Acesse a API em http://localhost:5000/swagger/index.html

📜 Licença
Este projeto está licenciado sob a MIT License - veja o arquivo LICENSE para mais detalhes.

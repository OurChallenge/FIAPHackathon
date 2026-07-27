# Swagger - Documentação da API

A API do Hackathon agora possui documentação interativa via Swagger/OpenAPI.

## Como Acessar

Quando você executar a API (`dotnet run` ou F5 no Visual Studio), a interface do Swagger estará disponível na raiz da aplicação:

```text
https://localhost:{porta}/
```

Por exemplo:

```text
https://localhost:5001/
https://localhost:7001/
```

## Funcionalidades

### 1. **Documentação Interativa**

- Visualize todos os endpoints disponíveis
- Veja os modelos de dados (DTOs)
- Explore parâmetros, tipos de retorno e códigos de status

### 2. **Testar Endpoints**

- Clique em qualquer endpoint
- Clique em "Try it out"
- Preencha os parâmetros necessários
- Clique em "Execute" para fazer a requisição

### 3. **Autenticação JWT**

Para testar endpoints protegidos:

1. Primeiro, faça login através do endpoint `/api/auth/login`
2. Copie o token JWT retornado
3. Clique no botão "Authorize" no topo da página
4. Cole o token no formato: `Bearer {seu-token-aqui}`
5. Clique em "Authorize"
6. Agora você pode testar os endpoints protegidos de acordo com o perfil do usuário autenticado

### Exemplo de uso

#### 1. Registrar um novo doador

```http
POST /api/auth/register
```

```json
{
  "fullName": "João Silva",
  "email": "joao@example.com",
  "cpf": "12345678909",
  "password": "SenhaSegura123!"
}
```

Possíveis respostas:

- `201 Created` - Doador cadastrado com sucesso
- `400 Bad Request` - Dados inválidos
- `409 Conflict` - Usuário já cadastrado

#### 2. Fazer login

```http
POST /api/auth/login
```

```json
{
  "email": "joao@example.com",
  "password": "SenhaSegura123!"
}
```

#### 3. Usar o token retornado

- Copie o `token` da resposta
- Clique em "Authorize"
- Cole: `Bearer {token}`
- Clique em "Authorize"

#### 4. Acessar endpoints protegidos

A API utiliza autorização baseada em perfis (roles).

**Endpoints públicos:**

- `POST /api/auth/register` - Registrar um doador
- `POST /api/auth/login` - Realizar login
- `GET /api/campaigns/active` - Listar campanhas ativas

**Endpoints do GestorONG:**

- `POST /api/campaigns` - Criar campanha
- `PUT /api/campaigns/{id}` - Atualizar campanha

**Endpoints do Doador:**

- `POST /api/donations` - Registrar uma doação

A criação de uma doação retorna `202 Accepted`, pois o processamento é realizado de forma assíncrona através do RabbitMQ e do Donation Worker.

## Configuração

A configuração do Swagger está em:

- `Hackathon.Api/Program.cs`
- Documentação XML habilitada no `Hackathon.Api/Hackathon.Api.csproj`

## Endpoints Documentados

XML comments podem ser adicionados aos controllers para documentação mais rica:

```csharp
/// <summary>
/// Registra um novo doador no sistema
/// </summary>
/// <param name="request">Dados do doador</param>
/// <returns>ID do doador criado</returns>
/// <response code="201">Doador criado com sucesso</response>
/// <response code="400">Dados inválidos</response>
/// <response code="409">Usuário já cadastrado</response>
[HttpPost("register")]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<IActionResult> Register([FromBody] RegisterDonorRequest request)
{
    // ...
}
```

## Recursos Adicionais

- **JSON OpenAPI**: Disponível em `https://localhost:{porta}/swagger/v1/swagger.json`
- **Documentação XML**: Gerada automaticamente quando são adicionados comentários XML aos controllers
- **Health Check**: Disponível em `/health`
- **Métricas Prometheus**: Disponíveis em `/metrics`

## Pacotes Instalados

- `Swashbuckle.AspNetCore` v10.2.3
- `Microsoft.AspNetCore.OpenApi` v10.0.8
- `Microsoft.OpenApi` v2.7.5

## Integração com o Projeto

Além da documentação Swagger/OpenAPI, a solução utiliza:

- JWT para autenticação e autorização
- SQL Server para persistência dos dados
- RabbitMQ para mensageria e processamento assíncrono das doações
- Donation Worker para consumo e processamento das mensagens
- Prometheus para coleta de métricas
- Grafana para visualização das métricas
- Zabbix para monitoramento da disponibilidade
- Kubernetes para orquestração dos serviços

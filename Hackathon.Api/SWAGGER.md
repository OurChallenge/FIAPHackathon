# Swagger - Documentação da API

A API do Hackathon possui documentação interativa via Swagger/OpenAPI com **suporte completo a autenticação JWT Bearer**.

✅ **Bearer Token configurado** - O botão "Authorize" 🔓 está ativo no Swagger UI

## Como Acessar

Quando você executar a API (`dotnet run` ou F5 no Visual Studio), a interface do Swagger estará disponível na raiz da aplicação:

```
https://localhost:{porta}/
```

Por exemplo:
```
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
- Click "Try it out"
- Preencha os parâmetros necessários
- Clique em "Execute" para fazer a requisição

### 3. **Autenticação JWT** 🔐

✅ **O botão "Authorize" está configurado e pronto para uso!**

Para testar endpoints protegidos:

1. Primeiro, faça login através do endpoint `/api/auth/login`
2. Copie o token JWT retornado
3. **Clique no botão "Authorize" 🔓** no topo da página Swagger
4. Cole o token no formato: `Bearer {seu-token-aqui}`
5. Clique em "Authorize" e depois em "Close"
6. Agora você pode testar os endpoints protegidos - todos incluirão automaticamente o header Authorization

### Exemplo de uso:

#### 1. Registrar um novo doador
```http
POST /api/auth/register

{
  "name": "João Silva",
  "email": "joao@example.com",
  "password": "SenhaSegura123!",
  "phoneNumber": "11999999999"
}
```

#### 2. Fazer login
```http
POST /api/auth/login
{
  "email": "joao@example.com",
  "password": "SenhaSegura123!"
}
```

#### 3. Usar o token retornado
- Copie o `token` da resposta
- Clique em "Authorize"
- Cole: `Bearer {token}`

#### 4. Acessar endpoints protegidos
Agora você pode acessar endpoints como:
- `GET /api/campaigns` - Listar campanhas
- `POST /api/donations` - Fazer doações
- etc.

## Configuração

A configuração do Swagger está em:
- `Hackathon.Api/Program.cs` (linhas 51-53 e 71-76)
- Documentação XML habilitada no `Hackathon.Api.csproj`

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
/// <response code="409">Email já cadastrado</response>
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
- **Documentação XML**: Gerada automaticamente se você adicionar comentários XML nos controllers

## Pacotes Instalados

- `Swashbuckle.AspNetCore` v6.9.0
- `Microsoft.OpenApi` v2.0.0 (via Swashbuckle dependency)

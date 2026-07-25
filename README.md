# FIAP Hackathon - Conexão Solidária

Backend desenvolvido para o Hackathon da FIAP com foco no gerenciamento de usuários, campanhas e doações para uma ONG.

A solução utiliza uma arquitetura em camadas, autenticação JWT, persistência em SQL Server e processamento assíncrono de doações através do RabbitMQ e de um Worker dedicado. A infraestrutura local é executada em containers e orquestrada com Kubernetes.

## Stack

- .NET 10
- ASP.NET Core
- Entity Framework Core
- SQL Server
- RabbitMQ
- JWT
- BCrypt
- Docker
- Kubernetes
- GitHub Actions
- xUnit

## Arquitetura

A solução está organizada nos seguintes projetos:

```text
Hackathon.Api
Hackathon.Application
Hackathon.Domain
Hackathon.Infrastructure
Hackathon.DonationWorker
Hackathon.Domain.Tests
Hackathon.Application.Tests
```

Responsabilidades principais:

| Projeto | Responsabilidade |
| --- | --- |
| `Hackathon.Api` | API REST, autenticação e endpoints |
| `Hackathon.Application` | Casos de uso, serviços, DTOs e contratos |
| `Hackathon.Domain` | Entidades, enums e regras de domínio |
| `Hackathon.Infrastructure` | Persistência, SQL Server, RabbitMQ, JWT e BCrypt |
| `Hackathon.DonationWorker` | Processamento assíncrono das doações |
| `Hackathon.Domain.Tests` | Testes do domínio |
| `Hackathon.Application.Tests` | Testes da camada de aplicação |

### Processamento de doações

O processamento das doações é assíncrono.

```text
Cliente
   |
   v
Hackathon.Api
   |
   +------> SQL Server
   |
   v
RabbitMQ
   |
   v
DonationWorker
   |
   v
SQL Server
```

Ao registrar uma doação, a API persiste a operação e publica uma mensagem no RabbitMQ. O `DonationWorker` consome a mensagem, processa a doação e atualiza o total arrecadado da campanha.

Por esse motivo, o endpoint responsável pelo registro da doação retorna `202 Accepted`.

---

## Pré-requisitos

Para executar o ambiente local completo:

- .NET 10 SDK
- Docker Desktop
- Kubernetes habilitado no Docker Desktop
- `kubectl`
- Entity Framework CLI

Validação básica do ambiente:

```bash
dotnet --version
docker --version
kubectl version --client
dotnet ef --version
```

Verifique também o contexto Kubernetes:

```bash
kubectl config current-context
```

Utilizando Docker Desktop, o contexto esperado é:

```text
docker-desktop
```

O cluster pode ser validado com:

```bash
kubectl get nodes
```

---

# Executando o projeto

Todos os comandos abaixo devem ser executados a partir da raiz do repositório, salvo quando indicado o contrário.

## 1. Restaurar, compilar e testar

```bash
dotnet restore FIAPHackathon.slnx
dotnet build FIAPHackathon.slnx
dotnet test FIAPHackathon.slnx
```

## 2. Criar as imagens Docker

### API

```bash
docker build -f Hackathon.Api/Dockerfile -t fiap-hackathon-api:local .
```

### Donation Worker

```bash
docker build -f Hackathon.DonationWorker/Dockerfile -t fiap-hackathon-donation-worker:local .
```

As imagens podem ser verificadas com:

```bash
docker images
```

Os manifests Kubernetes locais utilizam as tags:

```text
fiap-hackathon-api:local
fiap-hackathon-donation-worker:local
```

---

## 3. Configurar os Secrets

Credenciais não são versionadas no repositório.

Utilize:

```text
k8s/secret.example.yaml
```

como modelo para criar:

```text
k8s/secret.yaml
```

No Windows:

```bat
copy k8s\secret.example.yaml k8s\secret.yaml
```

Substitua os valores indicados por `CHANGE_ME` pelas credenciais utilizadas no ambiente local.

O arquivo `k8s/secret.yaml` está incluído no `.gitignore` e não deve ser commitado.

---

## 4. Criar os recursos Kubernetes

### Namespace

```bash
kubectl apply -f k8s/namespace.yaml
```

### Configuração

```bash
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml
```

### SQL Server

Primeiro crie o volume persistente:

```bash
kubectl apply -f k8s/sqlserver/pvc.yaml
```

Depois:

```bash
kubectl apply -f k8s/sqlserver/deployment.yaml
kubectl apply -f k8s/sqlserver/service.yaml
```

O PVC pode ser conferido com:

```bash
kubectl get pvc -n fiap-hackathon
```

O volume `sqlserver-data` deve estar com status `Bound`.

### RabbitMQ

```bash
kubectl apply -f k8s/rabbitmq/deployment.yaml
kubectl apply -f k8s/rabbitmq/service.yaml
```

### API

```bash
kubectl apply -f k8s/api/deployment.yaml
kubectl apply -f k8s/api/service.yaml
```

### Donation Worker

```bash
kubectl apply -f k8s/worker/deployment.yaml
```

O Worker não necessita de um Kubernetes Service, pois atua como consumidor de mensagens e não expõe endpoints HTTP.

---

## 5. Verificar o ambiente

```bash
kubectl get pods -n fiap-hackathon
```

Os seguintes componentes devem estar em execução:

```text
hackathon-api
donation-worker
rabbitmq
sqlserver
```

Para visualizar os demais recursos:

```bash
kubectl get all -n fiap-hackathon
```

---

# Banco de dados

## Aplicando as migrations

O SQL Server executado no Kubernetes não fica diretamente exposto para o host.

Abra um terminal e execute:

```bash
kubectl port-forward service/sqlserver 1433:1433 -n fiap-hackathon
```

Mantenha esse terminal aberto.

Em outro terminal, configure a connection string utilizada pelo Entity Framework.

No Windows CMD:

```bat
set ConnectionStrings__DefaultConnection=Server=127.0.0.1,1433;Database=FIAPHackathon;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True
```

Em seguida:

```bash
dotnet ef database update --project Hackathon.Infrastructure/Hackathon.Infrastructure.csproj --startup-project Hackathon.Api/Hackathon.Api.csproj
```

As migrations criam a estrutura necessária para a aplicação, incluindo as tabelas de usuários, campanhas e doações.

---

## Seed inicial

A aplicação possui um `DatabaseSeeder` para inicialização dos dados necessários ao ambiente de desenvolvimento.

O seed é controlado pela configuração:

```yaml
SeedDatabase: "true"
```

em:

```text
k8s/configmap.yaml
```

Após habilitá-lo:

```bash
kubectl apply -f k8s/configmap.yaml
kubectl rollout restart deployment/hackathon-api -n fiap-hackathon
```

Aguarde a API retornar ao estado `Running`:

```bash
kubectl get pods -n fiap-hackathon
```

Após a inicialização, retorne a configuração para:

```yaml
SeedDatabase: "false"
```

e aplique novamente:

```bash
kubectl apply -f k8s/configmap.yaml
kubectl rollout restart deployment/hackathon-api -n fiap-hackathon
```

O `DatabaseSeeder` verifica a existência do registro antes da criação, evitando duplicidade em execuções posteriores.

---

# Acessando a API

Para disponibilizar a API no host:

```bash
kubectl port-forward service/hackathon-api 8080:8080 -n fiap-hackathon
```

A aplicação ficará acessível em:

```text
http://localhost:8080
```

O terminal do `port-forward` deve permanecer aberto.

Caso o Pod da API seja recriado durante um rollout e a conexão seja perdida, execute novamente o comando de `port-forward`.

---

# RabbitMQ

Para acessar o RabbitMQ Management:

```bash
kubectl port-forward service/rabbitmq 15672:15672 -n fiap-hackathon
```

Interface local:

```text
http://localhost:15672
```

As credenciais utilizadas devem corresponder às configuradas no ambiente Kubernetes.

---

# Fluxo básico de validação

Com o ambiente em execução, o fluxo principal pode ser validado na seguinte ordem:

1. Login do gestor
2. Criação de uma campanha
3. Cadastro de um doador
4. Login do doador
5. Registro de uma doação
6. Consulta das campanhas ativas

### Autenticação

```http
POST /api/auth/login
```

O login retorna um token JWT que deve ser utilizado nos endpoints protegidos.

### Campanhas

```http
POST /api/campaigns
```

A criação de campanha requer autenticação com a role adequada.

Para consultar campanhas ativas:

```http
GET /api/campaigns/active
```

Resposta esperada:

```text
200 OK
```

### Doações

```http
POST /api/donations
```

Resposta esperada:

```text
202 Accepted
```

Após o processamento da mensagem pelo `DonationWorker`, uma nova consulta às campanhas deve refletir o valor processado no total arrecadado.

---

# Persistência no Kubernetes

O SQL Server utiliza um `PersistentVolumeClaim` chamado:

```text
sqlserver-data
```

montado em:

```text
/var/opt/mssql
```

Dessa forma, os dados não dependem do ciclo de vida do Pod.

O PVC pode ser verificado com:

```bash
kubectl get pvc -n fiap-hackathon
```

Em ambiente de desenvolvimento, a persistência pode ser validada recriando apenas o Pod:

```bash
kubectl delete pod -l app=sqlserver -n fiap-hackathon
```

O Deployment criará automaticamente outro Pod, utilizando o mesmo volume persistente.

---

# Logs e diagnóstico

### API

```bash
kubectl logs -f deployment/hackathon-api -n fiap-hackathon
```

### Donation Worker

```bash
kubectl logs -f deployment/donation-worker -n fiap-hackathon
```

### RabbitMQ

```bash
kubectl logs -f deployment/rabbitmq -n fiap-hackathon
```

### SQL Server

```bash
kubectl logs -f deployment/sqlserver -n fiap-hackathon
```

Estado dos Pods:

```bash
kubectl get pods -n fiap-hackathon
```

Detalhes de um Pod específico:

```bash
kubectl describe pod NOME_DO_POD -n fiap-hackathon
```

---

# Estrutura Kubernetes

```text
k8s/
├── api/
│   ├── deployment.yaml
│   └── service.yaml
├── rabbitmq/
│   ├── deployment.yaml
│   └── service.yaml
├── sqlserver/
│   ├── deployment.yaml
│   ├── pvc.yaml
│   └── service.yaml
├── worker/
│   └── deployment.yaml
├── configmap.yaml
├── namespace.yaml
└── secret.example.yaml
```

O arquivo `secret.yaml` existe apenas no ambiente local e não faz parte do repositório.

---

# CI

O projeto utiliza GitHub Actions para validação contínua.

O workflow está localizado em:

```text
.github/workflows/ci.yml
```

O pipeline valida o projeto através das etapas de restore, build, testes automatizados e build das imagens Docker.

Antes de abrir um Pull Request, é recomendado executar localmente:

```bash
dotnet build FIAPHackathon.slnx
dotnet test FIAPHackathon.slnx
```

---

# Comandos úteis

```bash
# Pods
kubectl get pods -n fiap-hackathon

# Services
kubectl get services -n fiap-hackathon

# PVC
kubectl get pvc -n fiap-hackathon

# Todos os principais recursos
kubectl get all -n fiap-hackathon

# Reiniciar API
kubectl rollout restart deployment/hackathon-api -n fiap-hackathon

# Reiniciar Worker
kubectl rollout restart deployment/donation-worker -n fiap-hackathon
```

## Observação sobre imagens locais

Os manifests utilizados no ambiente de desenvolvimento usam imagens locais e:

```yaml
imagePullPolicy: Never
```

Por isso, as imagens da API e do Worker precisam existir no Docker Desktop antes da criação dos respectivos Pods.

Para ambientes externos, as imagens devem ser publicadas em um registry e os manifests ajustados para utilizar as imagens publicadas.
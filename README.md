# FIAP Hackathon - Conexão Solidária

Backend desenvolvido para o Hackathon da FIAP com foco no gerenciamento de usuários, campanhas e doações para uma ONG.

A solução utiliza arquitetura em camadas, autenticação JWT, persistência em SQL Server e processamento assíncrono de doações através do RabbitMQ e de um Worker dedicado. A infraestrutura local é executada em containers e orquestrada com Kubernetes.

O projeto também possui observabilidade com Prometheus, Grafana e Zabbix, documentação interativa da API através do Swagger/OpenAPI e pipelines de CI/CD com GitHub Actions.

---

## Stack

- .NET 10
- ASP.NET Core
- Entity Framework Core
- SQL Server
- RabbitMQ
- JWT
- BCrypt
- Swagger / OpenAPI
- Docker
- Kubernetes
- Prometheus
- Grafana
- Zabbix
- GitHub Actions
- xUnit

---

# Arquitetura

A solução está organizada nos seguintes projetos:

```text
Hackathon.Api
Hackathon.Application
Hackathon.Domain
Hackathon.Infrastructure
Hackathon.DonationWorker
Hackathon.Domain.Tests
Hackathon.Application.Tests
Hackathon.Api.IntegrationTests
```

Responsabilidades principais:

| Projeto | Responsabilidade |
| --- | --- |
| `Hackathon.Api` | API REST, autenticação, autorização e endpoints |
| `Hackathon.Application` | Casos de uso, serviços, DTOs e contratos |
| `Hackathon.Domain` | Entidades, enums e regras de domínio |
| `Hackathon.Infrastructure` | Persistência, SQL Server, RabbitMQ, JWT e BCrypt |
| `Hackathon.DonationWorker` | Processamento assíncrono das doações |
| `Hackathon.Domain.Tests` | Testes das regras de domínio |
| `Hackathon.Application.Tests` | Testes da camada de aplicação |
| `Hackathon.Api.IntegrationTests` | Testes de integração da API, autenticação e autorização |

## Processamento de doações

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

Ao registrar uma doação, a API registra a intenção de doação e publica um evento no RabbitMQ. O valor arrecadado da campanha não é atualizado diretamente pela API.

O `DonationWorker` consome a mensagem de forma assíncrona, processa a doação e atualiza o valor total arrecadado da respectiva campanha no SQL Server.

Por esse motivo, o endpoint responsável pelo registro da doação retorna `202 Accepted`.

---

# Pré-requisitos

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

A solução possui testes de domínio, aplicação e integração da API, incluindo cenários de autenticação e autorização por roles.

---

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

# Kubernetes

## 4. Criar os recursos Kubernetes

### Namespace

```bash
kubectl apply -f k8s/namespace.yaml
```

### Configurações

```bash
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml
```

---

## SQL Server

Primeiro crie o volume persistente:

```bash
kubectl apply -f k8s/sqlserver/pvc.yaml
```

Depois crie o Deployment e o Service:

```bash
kubectl apply -f k8s/sqlserver/deployment.yaml
kubectl apply -f k8s/sqlserver/service.yaml
```

Verifique:

```bash
kubectl get pods -n fiap-hackathon -l app=sqlserver
```

---

## RabbitMQ

Primeiro crie o volume persistente:

```bash
kubectl apply -f k8s/rabbitmq/pvc.yaml
```

Depois:

```bash
kubectl apply -f k8s/rabbitmq/deployment.yaml
kubectl apply -f k8s/rabbitmq/service.yaml
```

Verifique:

```bash
kubectl get pods -n fiap-hackathon -l app=rabbitmq
```

Os volumes persistentes podem ser conferidos com:

```bash
kubectl get pvc -n fiap-hackathon
```

Os PVCs:

```text
sqlserver-data
rabbitmq-data
```

devem apresentar status:

```text
Bound
```

---

## API

Crie o Deployment e o Service da API:

```bash
kubectl apply -f k8s/api/deployment.yaml
kubectl apply -f k8s/api/service.yaml
```

Verifique:

```bash
kubectl get pods -n fiap-hackathon -l app=hackathon-api
```

---

## Donation Worker

Crie o Deployment:

```bash
kubectl apply -f k8s/worker/deployment.yaml
```

Verifique:

```bash
kubectl get pods -n fiap-hackathon -l app=donation-worker
```

O Worker não necessita de Kubernetes Service, pois atua como consumidor de mensagens e não expõe endpoints HTTP.

---

# Observabilidade

A solução utiliza Prometheus, Grafana e Zabbix para observabilidade e monitoramento.

A API disponibiliza:

```text
GET /health
GET /metrics
```

O endpoint `/health` permite verificar a disponibilidade da aplicação.

O endpoint `/metrics` disponibiliza métricas HTTP no formato consumido pelo Prometheus.

---

## Prometheus

Crie os recursos do Prometheus:

```bash
kubectl apply -f k8s/prometheus/configmap.yaml
kubectl apply -f k8s/prometheus/deployment.yaml
kubectl apply -f k8s/prometheus/service.yaml
```

Verifique o Pod:

```bash
kubectl get pods -n fiap-hackathon -l app=prometheus
```

Para acessar localmente:

```bash
kubectl port-forward service/prometheus 9090:9090 -n fiap-hackathon
```

A interface ficará disponível em:

```text
http://localhost:9090
```

O Prometheus coleta as métricas expostas pela API através do endpoint `/metrics`.

---

## Grafana

O Grafana utiliza Kustomize para gerar e associar os ConfigMaps responsáveis pelo datasource do Prometheus e pelo dashboard provisionado.

Aplique os recursos com:

```bash
kubectl apply -k k8s/grafana
```

> O Grafana deve ser aplicado utilizando `kubectl apply -k k8s/grafana`, pois o `kustomization.yaml` gera os ConfigMaps necessários e atualiza automaticamente suas referências no Deployment.

Verifique o Pod:

```bash
kubectl get pods -n fiap-hackathon -l app=grafana
```

Para acessar localmente:

```bash
kubectl port-forward service/grafana 3000:3000 -n fiap-hackathon
```

A interface ficará disponível em:

```text
http://localhost:3000
```

O Grafana utiliza o Prometheus como datasource e possui dashboard provisionado para visualização das métricas reais da API.

Entre as informações monitoradas estão métricas relacionadas a:

- requisições HTTP;
- taxa de requisições;
- duração das requisições;
- requisições em andamento.

As credenciais administrativas devem corresponder às configuradas no ambiente Kubernetes.

---

## Zabbix

O ambiente de monitoramento Zabbix utiliza PostgreSQL para armazenamento dos dados internos da ferramenta.

### PostgreSQL do Zabbix

Crie o PostgreSQL:

```bash
kubectl apply -f k8s/zabbix/postgres.yaml
```

### Zabbix Server

Depois:

```bash
kubectl apply -f k8s/zabbix/server.yaml
```

### Zabbix Web

Crie a interface Web e seu Service:

```bash
kubectl apply -f k8s/zabbix/web.yaml
kubectl apply -f k8s/zabbix/service.yaml
```

No Windows, os componentes podem ser verificados com:

```bat
kubectl get pods -n fiap-hackathon | findstr zabbix
```

No Linux/macOS:

```bash
kubectl get pods -n fiap-hackathon | grep zabbix
```

Para acessar a interface Web:

```bash
kubectl port-forward service/zabbix-web 8081:8080 -n fiap-hackathon
```

A interface ficará disponível em:

```text
http://localhost:8081
```

O Zabbix pode monitorar o endpoint interno da API:

```text
http://hackathon-api:8080/health
```

Esse monitoramento permite acompanhar a disponibilidade e o comportamento do endpoint de saúde da aplicação.

---

### Configuração do monitoramento da API no Zabbix

Após acessar o Zabbix Web:

1. Crie um host chamado `FIAP Hackathon API`.
2. Crie um Web Scenario chamado `API Health Check`.
3. Configure um Step com:
   - URL: `http://hackathon-api:8080/health`
   - Required status codes: `200`
   - Update interval: `30s`
4. Salve o cenário e aguarde a primeira coleta.

O Zabbix passará a registrar disponibilidade, tempo de resposta, código HTTP e falhas do endpoint.

# Verificando o ambiente

Após criar todos os recursos:

```bash
kubectl get pods -n fiap-hackathon
```

Os seguintes componentes devem estar em execução:

```text
hackathon-api
donation-worker
rabbitmq
sqlserver
prometheus
grafana
zabbix-server
zabbix-web
zabbix-postgres
```

Para visualizar os demais recursos:

```bash
kubectl get all -n fiap-hackathon
```

Para visualizar os volumes persistentes:

```bash
kubectl get pvc -n fiap-hackathon
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

# Seed inicial

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

# Swagger / OpenAPI

A API possui documentação interativa através do Swagger/OpenAPI.

Com a API disponível localmente em `8080`, acesse:

```text
http://localhost:8080/
```

O documento OpenAPI pode ser acessado em:

```text
http://localhost:8080/swagger/v1/swagger.json
```

O Swagger permite visualizar os endpoints e executar requisições diretamente pela interface.

Para endpoints protegidos, realize o login em:

```http
POST /api/auth/login
```

e utilize o token JWT através da opção `Authorize`.

A documentação detalhada está disponível em:

```text
Hackathon.Api/SWAGGER.md
```

---

# RabbitMQ Management

Para acessar a interface administrativa do RabbitMQ:

```bash
kubectl port-forward service/rabbitmq 15672:15672 -n fiap-hackathon
```

A interface ficará disponível em:

```text
http://localhost:15672
```

As credenciais utilizadas devem corresponder às configuradas no ambiente Kubernetes.

O RabbitMQ é responsável por desacoplar o recebimento da doação de seu processamento.

A API publica a mensagem e o `DonationWorker` realiza o consumo de forma assíncrona.

---

# Fluxo básico de validação

Com o ambiente em execução, o fluxo principal pode ser validado na seguinte ordem:

1. Login do gestor
2. Criação de uma campanha
3. Cadastro de um doador
4. Login do doador
5. Registro de uma doação
6. Processamento da mensagem pelo Donation Worker
7. Consulta das campanhas ativas
8. Validação do valor arrecadado após o processamento

---

## Autenticação

### Cadastro do doador

```http
POST /api/auth/register
```

### Login

```http
POST /api/auth/login
```

O login retorna um token JWT utilizado nos endpoints protegidos.

---

## Campanhas

### Criar campanha

```http
POST /api/campaigns
```

A criação requer autenticação com a role:

```text
GestorONG
```

### Atualizar campanha

```http
PUT /api/campaigns/{id}
```

Também requer a role:

```text
GestorONG
```

### Consultar campanhas ativas

```http
GET /api/campaigns/active
```

Esse endpoint é público.

Resposta esperada:

```text
200 OK
```

---

## Doações

```http
POST /api/donations
```

A criação da doação requer autenticação com a role:

```text
Doador
```

Resposta esperada:

```text
202 Accepted
```

A API publica a mensagem no RabbitMQ.

O `DonationWorker` realiza o processamento assíncrono e atualiza o total arrecadado da campanha.

Após o processamento, uma nova consulta a:

```http
GET /api/campaigns/active
```

deve refletir o novo valor arrecadado.

---

# Persistência no Kubernetes

## SQL Server

O SQL Server utiliza um `PersistentVolumeClaim` chamado:

```text
sqlserver-data
```

montado em:

```text
/var/opt/mssql
```

Dessa forma, os dados não dependem do ciclo de vida do Pod.

## RabbitMQ

O RabbitMQ utiliza o `PersistentVolumeClaim`:

```text
rabbitmq-data
```

para manter os dados do broker mesmo quando o Pod é recriado.

Os PVCs podem ser verificados com:

```bash
kubectl get pvc -n fiap-hackathon
```

Em ambiente de desenvolvimento, a persistência do SQL Server pode ser validada recriando apenas o Pod:

```bash
kubectl delete pod -l app=sqlserver -n fiap-hackathon
```

O Deployment criará automaticamente outro Pod utilizando o mesmo volume persistente.

---

# Logs e diagnóstico

## API

```bash
kubectl logs -f deployment/hackathon-api -n fiap-hackathon
```

## Donation Worker

```bash
kubectl logs -f deployment/donation-worker -n fiap-hackathon
```

## RabbitMQ

```bash
kubectl logs -f deployment/rabbitmq -n fiap-hackathon
```

## SQL Server

```bash
kubectl logs -f deployment/sqlserver -n fiap-hackathon
```

## Prometheus

```bash
kubectl logs -f deployment/prometheus -n fiap-hackathon
```

## Grafana

```bash
kubectl logs -f deployment/grafana -n fiap-hackathon
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

A infraestrutura Kubernetes está organizada da seguinte forma:

```text
k8s/
├── api/
│   ├── deployment.yaml
│   └── service.yaml
├── grafana/
│   ├── deployment.yaml
│   ├── service.yaml
│   ├── kustomization.yaml
│   ├── dashboards/
│   └── provisioning/
├── prometheus/
│   ├── configmap.yaml
│   ├── deployment.yaml
│   └── service.yaml
├── rabbitmq/
│   ├── deployment.yaml
│   ├── pvc.yaml
│   └── service.yaml
├── sqlserver/
│   ├── deployment.yaml
│   ├── pvc.yaml
│   └── service.yaml
├── worker/
│   └── deployment.yaml
├── zabbix/
│   ├── postgres.yaml
│   ├── server.yaml
│   ├── web.yaml
│   └── service.yaml
├── configmap.yaml
├── namespace.yaml
└── secret.example.yaml
```

Entre os principais recursos estão:

- Deployment e Service da API
- Deployment do Donation Worker
- Deployment, Service e PVC do RabbitMQ
- Deployment, Service e PVC do SQL Server
- Prometheus para coleta das métricas
- Grafana com datasource e dashboard provisionados
- Zabbix Server, Web e PostgreSQL
- ConfigMap para configurações não sensíveis
- Secret para configurações sensíveis

O arquivo:

```text
k8s/secret.yaml
```

existe apenas no ambiente local e não faz parte do repositório.

---

# CI/CD

O projeto utiliza GitHub Actions para integração e entrega contínuas.

Os workflows estão localizados em:

```text
.github/workflows/ci.yml
.github/workflows/cd.yml
```

## CI

O pipeline de CI executa:

1. checkout do código;
2. instalação do .NET 10;
3. restore das dependências;
4. build da solução;
5. execução dos testes automatizados;
6. build da imagem Docker da API;
7. build da imagem Docker do Donation Worker.

O workflow é executado nas branches configuradas para integração do projeto.

Antes de abrir um Pull Request, é recomendado executar localmente:

```bash
dotnet build FIAPHackathon.slnx
dotnet test FIAPHackathon.slnx
```

---

## CD

O projeto também possui workflow de Continuous Deployment para o ambiente de desenvolvimento na Azure.

O processo utiliza autenticação OIDC, Azure Container Registry e Azure Container Apps.

Em alto nível, o workflow:

1. autentica no Azure;
2. autentica no Azure Container Registry;
3. gera a imagem Docker da API;
4. publica a imagem no registry;
5. atualiza o Azure Container App para utilizar a nova imagem.

O ambiente local Kubernetes é independente desse processo e pode ser executado integralmente através dos manifests do diretório `k8s/`.

---

# Testes automatizados

O projeto possui testes automatizados distribuídos em:

```text
Hackathon.Domain.Tests
Hackathon.Application.Tests
Hackathon.Api.IntegrationTests
```

Os testes cobrem regras de domínio, serviços da camada Application e cenários de integração relacionados à autenticação e autorização.

Para executar todos os testes:

```bash
dotnet test FIAPHackathon.slnx
```

Os testes também são executados automaticamente pelo pipeline de CI.

---

# Comandos úteis

```bash
# Pods
kubectl get pods -n fiap-hackathon

# Services
kubectl get services -n fiap-hackathon

# PVCs
kubectl get pvc -n fiap-hackathon

# Todos os principais recursos
kubectl get all -n fiap-hackathon

# Reiniciar API
kubectl rollout restart deployment/hackathon-api -n fiap-hackathon

# Reiniciar Worker
kubectl rollout restart deployment/donation-worker -n fiap-hackathon

# Logs da API
kubectl logs -f deployment/hackathon-api -n fiap-hackathon

# Logs do Worker
kubectl logs -f deployment/donation-worker -n fiap-hackathon
```

---

# Observação sobre imagens locais

Os manifests utilizados no ambiente local usam imagens:

```text
fiap-hackathon-api:local
fiap-hackathon-donation-worker:local
```

e estão configurados com:

```yaml
imagePullPolicy: Never
```

Por isso, as imagens da API e do Worker precisam existir no Docker Desktop antes da criação dos respectivos Pods.

Para ambientes externos, as imagens devem ser publicadas em um registry e os manifests ajustados para utilizar as imagens publicadas.

---

# Documentação adicional

Além deste README, a documentação da API está disponível em:

```text
Hackathon.Api/SWAGGER.md
```

O projeto também deve ser acompanhado dos demais artefatos de documentação exigidos para a entrega do Hackathon, incluindo o diagrama da arquitetura e a justificativa das tecnologias de persistência utilizadas.
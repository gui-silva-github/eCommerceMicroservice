# Roteiro: Azure Container Apps e depois AKS

Ordem prática dos comandos para publicar **a mesma stack** primeiro no **Azure Container Apps (ACA)** e em seguida no **AKS**. Troque só as variáveis do bloco 0. Os nomes das APIs no ACA **têm de coincidir** com o `ocelot.docker.json` (`users-api-app`, `products-api-app`, `orders-api-app`).

No ACA cada processo é um recurso Azure (`az containerapp create`). No AKS o recurso Azure é **um** cluster; os processos entram com `kubectl apply -f k8s/*.yaml`.

---

## 0. Variáveis e login (vale para os dois labs)

```bash
LOCATION=brazilsouth          # ou a região que a sua subscrição aceitar
RG_ACA=rg-ecommerce-aca
RG_AKS=rg-ecommerce-aks
ACR=ecommercelabacr           # 5–50 caracteres, único no mundo, só letras/números
ACA_ENV=ecommerce-aca-env
AKS_CLUSTER=aks-ecommerce

az login
az account show
# az account set --subscription "<SUBSCRIPTION_ID>"
```

O ACR é compartilhado: o ACA e o AKS puxam as mesmas imagens.

```bash
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.ContainerRegistry --wait
az provider register --namespace Microsoft.OperationalInsights --wait
az provider register --namespace Microsoft.ContainerService --wait   # só precisa no AKS; pode registrar já
```

---

## Parte A — Azure Container Apps

Siga esta ordem. Não crie o gateway antes das APIs, nem as APIs antes dos bancos.

### A1. Resource group + ACR

```bash
az group create -n "$RG_ACA" -l "$LOCATION"

az acr create -g "$RG_ACA" -n "$ACR" --sku Basic
az acr update -n "$ACR" --admin-enabled true
az acr login -n "$ACR"

ACR_LOGIN=$(az acr show -n "$ACR" --query loginServer -o tsv)
ACR_USER=$(az acr credential show -n "$ACR" --query username -o tsv)
ACR_PASS=$(az acr credential show -n "$ACR" --query passwords[0].value -o tsv)
```

### A2. Build e push das imagens

Na raiz do repositório. As tags `:aca` e `:init` são as que o Container App vai puxar.

```bash
docker build -t "$ACR_LOGIN/ecommerce-users-api:aca" \
  -f User/eCommerceSolution.UsersService/eCommerce.API/Dockerfile \
  User/eCommerceSolution.UsersService

docker build -t "$ACR_LOGIN/ecommerce-products-api:aca" \
  -f Product/eCommerceSolution.ProductsService/ProductsMicroService.API/Dockerfile \
  Product/eCommerceSolution.ProductsService

docker build -t "$ACR_LOGIN/ecommerce-orders-api:aca" \
  -f Order/eCommerceSolution.OrdersService/API/Dockerfile \
  Order/eCommerceSolution.OrdersService

docker build -t "$ACR_LOGIN/ecommerce-api-gateway:aca" \
  -f Gateway/eCommerceSolution.ApiGateway/Dockerfile \
  Gateway/eCommerceSolution.ApiGateway

docker build -t "$ACR_LOGIN/ecommerce-postgres:init" docker/postgres
docker build -t "$ACR_LOGIN/ecommerce-mysql:init" docker/mysql

docker push "$ACR_LOGIN/ecommerce-users-api:aca"
docker push "$ACR_LOGIN/ecommerce-products-api:aca"
docker push "$ACR_LOGIN/ecommerce-orders-api:aca"
docker push "$ACR_LOGIN/ecommerce-api-gateway:aca"
docker push "$ACR_LOGIN/ecommerce-postgres:init"
docker push "$ACR_LOGIN/ecommerce-mysql:init"
```

Importe as imagens públicas para o ACR (evita rate limit do Docker Hub no Environment):

```bash
az acr import -n "$ACR" --source docker.io/library/mongo:7 --image mongo:7
az acr import -n "$ACR" --source docker.io/library/redis:7-alpine --image redis:7-alpine
az acr import -n "$ACR" --source docker.io/library/rabbitmq:3-management-alpine --image rabbitmq:3-management-alpine
```

Neste lab o Angular **não** vai para o ACA: roda na máquina com `npm run start:azure` e chama o FQDN do gateway.

### A3. Environment (a rede compartilhada)

```bash
az containerapp env create \
  -n "$ACA_ENV" \
  -g "$RG_ACA" \
  -l "$LOCATION"

az containerapp env list -g "$RG_ACA" -o table
```

Use o nome completo da listagem. Apps no mesmo Environment resolvem uns aos outros pelo **nome do Container App**. HTTP interno entra na porta **80** e o ingress encaminha ao `targetPort` 8080.

### A4. Infra primeiro (TCP interno)

Bancos, Redis e Rabbit **antes** das APIs. Ingress TCP interno. Rabbit precisa da porta **5672 (AMQP)**; a UI 15672 não substitui o AMQP.

**Postgres (Users) — imagen `:init` para o seed `admin@gmail.com` / `admin`:**

```bash
az containerapp create \
  -n postgres-app -g "$RG_ACA" --environment "$ACA_ENV" \
  --image "$ACR_LOGIN/ecommerce-postgres:init" \
  --registry-server "$ACR_LOGIN" --registry-username "$ACR_USER" --registry-password "$ACR_PASS" \
  --cpu 0.5 --memory 1Gi --min-replicas 1 --max-replicas 1 \
  --ingress internal --transport tcp --target-port 5432 --exposed-port 5432 \
  --env-vars POSTGRES_USER=postgres POSTGRES_PASSWORD=123 POSTGRES_DB=eCommerceUsers
```

**MySQL (Products) — aspas em `--args` para o Azure CLI não engolir os hífens:**

```bash
az containerapp create \
  -n mysql-app -g "$RG_ACA" --environment "$ACA_ENV" \
  --image "$ACR_LOGIN/ecommerce-mysql:init" \
  --registry-server "$ACR_LOGIN" --registry-username "$ACR_USER" --registry-password "$ACR_PASS" \
  --cpu 0.5 --memory 1Gi --min-replicas 1 --max-replicas 1 \
  --ingress internal --transport tcp --target-port 3306 --exposed-port 3306 \
  --env-vars MYSQL_ROOT_PASSWORD=admin MYSQL_DATABASE=ecommerceproducts MYSQL_USER=ecommerce MYSQL_PASSWORD=admin \
  --args "--character-set-server=utf8mb4 --collation-server=utf8mb4_unicode_ci --default-authentication-plugin=mysql_native_password"
```

**MongoDB, Redis, RabbitMQ:**

```bash
az containerapp create \
  -n mongodb-app -g "$RG_ACA" --environment "$ACA_ENV" \
  --image "$ACR_LOGIN/mongo:7" \
  --registry-server "$ACR_LOGIN" --registry-username "$ACR_USER" --registry-password "$ACR_PASS" \
  --cpu 0.5 --memory 1Gi --min-replicas 1 --max-replicas 1 \
  --ingress internal --transport tcp --target-port 27017 --exposed-port 27017

az containerapp create \
  -n redis-app -g "$RG_ACA" --environment "$ACA_ENV" \
  --image "$ACR_LOGIN/redis:7-alpine" \
  --registry-server "$ACR_LOGIN" --registry-username "$ACR_USER" --registry-password "$ACR_PASS" \
  --cpu 0.25 --memory 0.5Gi --min-replicas 1 --max-replicas 1 \
  --ingress internal --transport tcp --target-port 6379 --exposed-port 6379 \
  --args "redis-server --appendonly yes"

az containerapp create \
  -n rabbitmq-app -g "$RG_ACA" --environment "$ACA_ENV" \
  --image "$ACR_LOGIN/rabbitmq:3-management-alpine" \
  --registry-server "$ACR_LOGIN" --registry-username "$ACR_USER" --registry-password "$ACR_PASS" \
  --cpu 0.5 --memory 1Gi --min-replicas 1 --max-replicas 1 \
  --ingress internal --transport tcp --target-port 5672 --exposed-port 5672 \
  --env-vars RABBITMQ_DEFAULT_USER=guest RABBITMQ_DEFAULT_PASS=guest
```

Espere `Running` em cada um:

```bash
for APP in postgres-app mysql-app mongodb-app redis-app rabbitmq-app; do
  az containerapp show -g "$RG_ACA" -n "$APP" --query "{name:name,status:properties.runningStatus}" -o table
done
```

### A5. APIs (HTTP), na ordem Users → Products → Orders

**Users** (depende só do Postgres):

```bash
az containerapp create \
  -n users-api-app -g "$RG_ACA" --environment "$ACA_ENV" \
  --image "$ACR_LOGIN/ecommerce-users-api:aca" \
  --registry-server "$ACR_LOGIN" --registry-username "$ACR_USER" --registry-password "$ACR_PASS" \
  --cpu 0.5 --memory 1Gi --min-replicas 1 --max-replicas 1 \
  --ingress external --target-port 8080 \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Development \
    "ConnectionStrings__PostgresConnection=Host=postgres-app;Port=5432;Database=eCommerceUsers;Username=postgres;Password=123"
```

**Products** (MySQL + Redis + Rabbit):

```bash
az containerapp create \
  -n products-api-app -g "$RG_ACA" --environment "$ACA_ENV" \
  --image "$ACR_LOGIN/ecommerce-products-api:aca" \
  --registry-server "$ACR_LOGIN" --registry-username "$ACR_USER" --registry-password "$ACR_PASS" \
  --cpu 0.5 --memory 1Gi --min-replicas 1 --max-replicas 1 \
  --ingress external --target-port 8080 \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Development \
    MYSQL_HOST=mysql-app MYSQL_USER=ecommerce MYSQL_PASSWORD=admin \
    ConnectionStrings__Redis=redis-app:6379 \
    RabbitMQ__HostName=rabbitmq-app RabbitMQ__Port=5672 \
    RabbitMQ__UserName=guest RabbitMQ__Password=guest
```

**Orders** (Mongo + Redis + Rabbit + HTTP para Users e Products, **sem** passar pelo Ocelot):

```bash
az containerapp create \
  -n orders-api-app -g "$RG_ACA" --environment "$ACA_ENV" \
  --image "$ACR_LOGIN/ecommerce-orders-api:aca" \
  --registry-server "$ACR_LOGIN" --registry-username "$ACR_USER" --registry-password "$ACR_PASS" \
  --cpu 0.5 --memory 1Gi --min-replicas 1 --max-replicas 1 \
  --ingress external --target-port 8080 \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Development \
    MONGODB_HOST=mongodb-app MONGODB_PORT=27017 \
    ConnectionStrings__Redis=redis-app:6379 \
    UsersMicroserviceBaseUrl=http://users-api-app \
    ProductsMicroserviceBaseUrl=http://products-api-app \
    RabbitMQ__HostName=rabbitmq-app RabbitMQ__Port=5672 \
    RabbitMQ__UserName=guest RabbitMQ__Password=guest
```

### A6. Gateway por último (`ASPNETCORE_ENVIRONMENT=Docker` → `ocelot.docker.json`)

```bash
az containerapp create \
  -n api-gateway-app -g "$RG_ACA" --environment "$ACA_ENV" \
  --image "$ACR_LOGIN/ecommerce-api-gateway:aca" \
  --registry-server "$ACR_LOGIN" --registry-username "$ACR_USER" --registry-password "$ACR_PASS" \
  --cpu 0.25 --memory 0.5Gi --min-replicas 1 --max-replicas 1 \
  --ingress external --target-port 8080 \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Docker \
    Cors__Origins=http://localhost:4200
```

```bash
FQDN=$(az containerapp show -g "$RG_ACA" -n api-gateway-app --query properties.configuration.ingress.fqdn -o tsv)
echo "https://$FQDN"
curl -s "https://$FQDN/health"
curl -s "https://$FQDN/api/products" | head -c 300
```

`GET /` no gateway é **404 esperado**. Use `/health` e `/api/...`.

### A7. Frontend na máquina

Edite `microservice/src/environment.azure.ts` com o FQDN do passo A6:

```ts
apiUrl: 'https://<FQDN>/api/Auth/',
productsMicroserviceUrl: 'https://<FQDN>/api/products',
ordersMicroserviceUrl: 'https://<FQDN>/api/Orders',
```

```bash
cd microservice
npm install
npm run start:azure
```

Abra `http://localhost:4200`. Login de laboratório: `admin@gmail.com` / `admin`.

Mudança no Ocelot só vale depois de `docker build` + `docker push` + `az containerapp update --image`.

### A8. Atualizar uma imagem já publicada

```bash
az containerapp update -g "$RG_ACA" -n api-gateway-app --image "$ACR_LOGIN/ecommerce-api-gateway:aca"
```

### A9. Pausar o lab ACA (custo)

Disco da réplica é **efêmero**. `min-replicas 0` nos bancos apaga Users/Products. Para uma noite, deixe a infra em 1 e pause só as APIs. Para zerar custo da stack:

```bash
for APP in api-gateway-app users-api-app products-api-app orders-api-app \
           postgres-app mysql-app mongodb-app redis-app rabbitmq-app
do
  az containerapp update -g "$RG_ACA" -n "$APP" --min-replicas 0 --max-replicas 0
done
```

Religar: `--min-replicas 1 --max-replicas 1` na **infra primeiro**, depois nas APIs, depois no gateway. Postgres/MySQL `:init` reseedam quando o data dir nasce vazio.

---

## Parte B — AKS (depois do ACA)

O Environment do ACA pode ficar no ar. São labs lado a lado. Apagar `$RG_AKS` **não** mexe em `$RG_ACA`.

`az` cria o cluster. `kubectl` cria Pods. Um serviço **não** vira um recurso Azure.

### B1. CLI e cluster

```bash
az aks install-cli
az aks get-versions -l "$LOCATION" -o table

az group create -n "$RG_AKS" -l "$LOCATION"

az aks create \
  -g "$RG_AKS" -n "$AKS_CLUSTER" \
  --node-count 2 \
  --node-vm-size Standard_B2s \
  --tier free \
  --attach-acr "$ACR" \
  --generate-ssh-keys
```

Se `Standard_B2s` for recusado, use um SKU da lista do erro (ou de `az aks get-versions`). `--tier free` não cobra o plano de controle; **o node cobra** com a loja parada. `--attach-acr` autoriza o node a puxar imagem sem senha no manifesto.

```bash
az aks get-credentials -g "$RG_AKS" -n "$AKS_CLUSTER"
kubectl get nodes
```

### B2. Imagens com tag AKS + prefixo do ACR nos manifests

No Git as imagens das APIs vão **sem** hostname (`ecommerce-postgres:init`). O node precisa de `$ACR_LOGIN/ecommerce-postgres:init`.

```bash
docker build -t "$ACR_LOGIN/ecommerce-users-api:aks-v1" \
  -f User/eCommerceSolution.UsersService/eCommerce.API/Dockerfile \
  User/eCommerceSolution.UsersService

docker build -t "$ACR_LOGIN/ecommerce-products-api:aks-v1" \
  -f Product/eCommerceSolution.ProductsService/ProductsMicroService.API/Dockerfile \
  Product/eCommerceSolution.ProductsService

docker build -t "$ACR_LOGIN/ecommerce-orders-api:aks-v1" \
  -f Order/eCommerceSolution.OrdersService/API/Dockerfile \
  Order/eCommerceSolution.OrdersService

docker build -t "$ACR_LOGIN/ecommerce-api-gateway:aks-v2" \
  -f Gateway/eCommerceSolution.ApiGateway/Dockerfile \
  Gateway/eCommerceSolution.ApiGateway

docker build -t "$ACR_LOGIN/ecommerce-postgres:init" docker/postgres
docker build -t "$ACR_LOGIN/ecommerce-mysql:init" docker/mysql

docker push "$ACR_LOGIN/ecommerce-users-api:aks-v1"
docker push "$ACR_LOGIN/ecommerce-products-api:aks-v1"
docker push "$ACR_LOGIN/ecommerce-orders-api:aks-v1"
docker push "$ACR_LOGIN/ecommerce-api-gateway:aks-v2"
docker push "$ACR_LOGIN/ecommerce-postgres:init"
docker push "$ACR_LOGIN/ecommerce-mysql:init"
```

O gateway no cluster **já** usa `ASPNETCORE_ENVIRONMENT=Aks` (`k8s/gateway.yaml` → `ocelot.docker.aks.json`, hosts `users-api` / `products-api` / `orders-api` na porta 80). Não reutilize a imagem `:aca` (environment `Docker`).

Aplique os YAML prefixando só imagens `ecommerce-*` (não altere `mongo:7`, `redis:7-alpine`, `rabbitmq:3-management-alpine`):

```bash
apply_acr() {
  sed "s|image: ecommerce-|image: ${ACR_LOGIN}/ecommerce-|g" "$1" | kubectl apply -f -
}
```

### B3. Namespaces

```bash
kubectl create namespace ecommerce
kubectl create namespace ecommerce-data
```

`ecommerce` = APIs + frontend. `ecommerce-data` = bancos, Redis, Rabbit. Cada passo abaixo espera `Ready` antes do seguinte.

### B4. Gateway (prova `/health` sem depender dos bancos)

```bash
apply_acr k8s/gateway.yaml
kubectl rollout status deploy/gateway -n ecommerce --timeout=180s
kubectl port-forward -n ecommerce svc/gateway 7010:80
```

Em outro terminal: `curl -s http://localhost:7010/health`. O `/health` é mapeado **antes** do Ocelot; 404 em `/` continua esperado.

### B5. Postgres → users-api

`PGDATA=/var/lib/postgresql/data/pgdata` já está no YAML: o disco Azure ext4 nasce com `lost+found` e o `initdb` recusa o mount point.

```bash
apply_acr k8s/postgres.yaml
kubectl wait --for=condition=ready pod -l app=postgres -n ecommerce-data --timeout=300s

apply_acr k8s/users.yaml
kubectl rollout status deploy/users-api -n ecommerce --timeout=180s
```

### B6. MySQL + Redis + RabbitMQ → products-api

```bash
apply_acr k8s/mysql.yaml
apply_acr k8s/redis.yaml
kubectl apply -f k8s/rabbitmq.yaml

kubectl wait --for=condition=ready pod -l app=mysql -n ecommerce-data --timeout=300s
kubectl wait --for=condition=ready pod -l app=redis -n ecommerce-data --timeout=180s
kubectl wait --for=condition=ready pod -l app=rabbitmq -n ecommerce-data --timeout=180s

apply_acr k8s/products.yaml
kubectl rollout status deploy/products-api -n ecommerce --timeout=180s
```

### B7. MongoDB → orders-api

```bash
kubectl apply -f k8s/mongodb.yaml
kubectl wait --for=condition=ready pod -l app=mongodb -n ecommerce-data --timeout=180s

apply_acr k8s/orders.yaml
kubectl rollout status deploy/orders-api -n ecommerce --timeout=180s
```

### B8. Application Routing + Ingress (antes do frontend)

O frontend Angular grava o IP do Ingress **dentro do JS**. Por isso o ADDRESS precisa existir **antes** do build `aks-prod`.

```bash
az aks approuting enable -g "$RG_AKS" -n "$AKS_CLUSTER"
kubectl apply -f k8s/ingress.yaml
kubectl get ingress -n ecommerce -w
```

`ADDRESS` vazio nos primeiros minutos é normal. Quando o IP aparecer:

```bash
INGRESS_IP=$(kubectl get ingress ecommerce -n ecommerce -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
echo "$INGRESS_IP"
curl -s "http://$INGRESS_IP/health"
curl -s "http://$INGRESS_IP/api/products" | head -c 300
```

502 em `/api/products` com log `Name or service not known (users-api-app)` significa gateway ainda em `Docker`. O Deployment precisa de `ASPNETCORE_ENVIRONMENT=Aks`.

### B9. Frontend (`aks-prod`) com o IP do Ingress

Edite `microservice/src/environment.aks.ts`:

```ts
apiUrl: 'http://<INGRESS_IP>/api/Auth/',
productsMicroserviceUrl: 'http://<INGRESS_IP>/api/products',
ordersMicroserviceUrl: 'http://<INGRESS_IP>/api/Orders',
```

```bash
docker build -t "$ACR_LOGIN/ecommerce-frontend:aks-v1" \
  --build-arg BUILD_CONFIGURATION=aks-prod \
  -f microservice/Dockerfile microservice
docker push "$ACR_LOGIN/ecommerce-frontend:aks-v1"

apply_acr k8s/frontend.yaml
kubectl rollout status deploy/frontend -n ecommerce --timeout=180s
```

Abra `http://<INGRESS_IP>/`. 503 no nginx do Ingress = Pod do frontend ainda não Ready (`ImagePullBackOff` se a imagem não chegou ao ACR).

Os IPs `10.244.x.x` dos Pods (`kubectl get pods -n ecommerce -o wide`) **não** abrem no browser. Só o `ADDRESS` do Ingress é público.

### B10. Conferir a stack

```bash
kubectl get pods -n ecommerce
kubectl get pods -n ecommerce-data
kubectl get svc -n ecommerce
kubectl get ingress -n ecommerce
```

### B11. Ligar e desligar (custo)

`kubectl scale --replicas=0` tira os Pods. PVC e Ingress ficam. As VMs **continuam cobrando**.

```bash
az aks stop -g "$RG_AKS" -n "$AKS_CLUSTER"
az aks start -g "$RG_AKS" -n "$AKS_CLUSTER"
az aks get-credentials -g "$RG_AKS" -n "$AKS_CLUSTER"
kubectl get ingress -n ecommerce
```

No `start`, o IP do Ingress **pode mudar**. Ajuste `environment.aks.ts`, refaça o build `aks-prod` e `kubectl set image` / `apply_acr` no frontend.

---

## Ordem compacta (checklist)

| # | ACA | AKS |
|---|-----|-----|
| 1 | `az group create` + `az acr create` | `az aks create --attach-acr` + `get-credentials` |
| 2 | `docker build/push` (`:aca`, `:init`) | `docker build/push` (`:aks-v1` / `:aks-v2`, `:init`) |
| 3 | `az containerapp env create` | `kubectl create namespace` ecommerce e ecommerce-data |
| 4 | TCP interno: postgres → mysql → mongo → redis → rabbit **5672** | gateway.yaml (prova `/health`) |
| 5 | users-api-app | postgres.yaml → users.yaml |
| 6 | products-api-app | mysql + redis + rabbitmq → products.yaml |
| 7 | orders-api-app | mongodb.yaml → orders.yaml |
| 8 | api-gateway-app (`Docker`) | `az aks approuting enable` + ingress.yaml |
| 9 | `npm run start:azure` | build frontend `aks-prod` + frontend.yaml |
| 10 | `az containerapp update --min-replicas 0` para pausar | `az aks stop` para pausar |

---

## O que muda entre os dois

| | ACA | AKS |
|-|-----|-----|
| Quem cria o processo | `az containerapp create` | `kubectl apply` |
| DNS do Users | `users-api-app` | `users-api.ecommerce.svc.cluster.local` |
| Porta HTTP interna | ingress **80** → 8080 | Service **80** → 8080 |
| Ocelot | `ocelot.docker.json` | `ocelot.docker.aks.json` |
| Disco | efêmero no lab | PVC `managed-csi` |
| O que a internet vê | FQDN de cada app HTTP externo | **um** IP de Ingress |
| Frontend | `ng serve` na máquina | nginx no cluster (`aks-prod`) |

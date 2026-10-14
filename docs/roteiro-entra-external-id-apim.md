# Roteiro: Microsoft Entra External ID + Azure API Management

Ordem prática para trocar o login de laboratório (`Token = "token"` no Users e admin fixo no Angular) por **login real** no **Microsoft Entra External ID** e colocar o **Azure API Management (APIM)** na frente do Ocelot. Vale para a stack que já está no ar pelo [`roteiro-aca-depois-aks.md`](roteiro-aca-depois-aks.md) (ACA **ou** AKS). Troque só as variáveis do bloco 0.

> **Por que não "Azure AD B2C"?** Desde **1º de maio de 2025** o Azure AD B2C não é mais vendido para novos clientes. Quem não tinha tenant B2C cria um **tenant externo do Microsoft Entra External ID** — o sucessor, com o mesmo papel (cadastro/login de clientes, user flows, branding). Tabela de equivalências na [seção final](#de-b2c-para-external-id).

---

## Visão geral

```
 Navegador (Angular + MSAL)
   │ 1. loginRedirect ─────────────▶ https://<SUBDOMINIO>.ciamlogin.com   (user flow "Inscrição e entrada")
   │ ◀──────────── 2. code + PKCE → access token (aud = ecommerce-api, scp = Ecommerce.Access, roles = Admin)
   │
   │ 3. Authorization: Bearer <token>
   ▼
 Azure API Management  https://<APIM>.azure-api.net/ecommerce/...
   • cors                         • validate-jwt (openid-config do ciamlogin.com)
   • operação Admin → roles       • createOrder → userID do corpo == oid
   • remove header ClientId       • injeta X-Apim-Secret
   │
   ▼ 4. /api/...  + X-Apim-Secret
 Ocelot (api-gateway-app no ACA | gateway no AKS)  ── recusa 403 sem o segredo
   │
   ▼
 users-api (valida o token de novo e faz upsert por oid) · products-api · orders-api
```

O que fica igual: Ocelot, Polly, Redis, RabbitMQ, bancos, `ocelot.docker*.json` (só ganhou a rota `/api/Users/me` sem cache). O Orders continua chamando o Users **direto** (`/api/Users/{userID}`), sem passar pelo APIM.

O que muda no código (já está neste branch):

| Onde | Mudança |
|------|---------|
| `User/.../Program.cs` | JWT Bearer apontando para `https://<SUBDOMINIO>.ciamlogin.com/<TENANT_ID>/v2.0` |
| `User/.../UsersController.cs` | `GET /api/Users/me`: cria/atualiza o usuário com `UserID = oid` e devolve `isAdmin` |
| `docker/postgres/init/01-users.sql` | `"Password"` aceita `NULL` (senha fica no Entra) |
| `Gateway/.../Program.cs` | Exige `X-Apim-Secret` quando `Apim__SharedSecret` está definido |
| `Gateway/.../ocelot*.json` | Rota `/api/Users/me` **sem** `FileCacheOptions` (cache por URL vazaria perfil entre usuários) |
| `microservice/src/environment.entra.ts` | URLs do APIM + config do MSAL |
| `microservice/src/app/auth/*` | `MsalInterceptor` anexa o token só nas rotas protegidas |
| `apim/` | OpenAPI para importar, policy fragments e políticas por operação |

---

## 0. Variáveis

Reaproveite as do roteiro anterior (`LOCATION`, `RG_ACA`, `RG_AKS`, `ACR`, `ACR_LOGIN`…) e acrescente:

```bash
RG_APIM=rg-ecommerce-apim
APIM=apim-ecommerce-lab          # único no mundo → https://$APIM.azure-api.net
APIM_PUBLISHER_EMAIL=voce@exemplo.com
APIM_PUBLISHER_NAME="Harbor eCommerce"

# Preenchidos ao longo do roteiro (anote!)
TENANT_SUBDOMAIN=                # A2: parte antes de .onmicrosoft.com (ex.: harborclientes)
TENANT_ID=                       # A2: Directory (tenant) ID do tenant EXTERNO
API_CLIENT_ID=                   # B1: Application (client) ID do ecommerce-api
SPA_CLIENT_ID=                   # B6: Application (client) ID do ecommerce-spa
GATEWAY_URL=                     # D1: https://<FQDN do api-gateway-app> (ACA) ou http://<INGRESS_IP> (AKS)

APIM_SHARED_SECRET=$(openssl rand -hex 32)
echo "$APIM_SHARED_SECRET"        # guarde: vai no APIM (E2) e no gateway (D2/D3)
```

Dois diretórios diferentes no Portal:

| Diretório | O que vive nele |
|-----------|-----------------|
| **Workforce** (o seu de sempre, onde está a assinatura) | ACR, ACA, AKS, **APIM** |
| **Externo** (novo, `<SUBDOMINIO>.onmicrosoft.com`) | App registrations, user flow, clientes, app roles |

A maior fonte de erro do lab é estar no diretório errado. O nome do diretório aparece no canto superior direito do Portal, embaixo do seu usuário.

---

## Parte A — Tenant externo

### A1. Criar o tenant

1. Entre em <https://entra.microsoft.com> com a conta da assinatura (precisa de pelo menos **Tenant Creator**).
2. **Entra ID** → **Overview** → **Manage tenants** → **+ Create**.
3. Escolha **External** → **Continue**.
4. Primeiro tenant externo? O Portal pode oferecer **trial de 30 dias sem assinatura**. Para o lab ficar junto dos outros recursos, prefira **Use Azure Subscription**.
5. Aba **Basics**:
   - **Tenant Name**: `Harbor Clientes`
   - **Domain Name**: `harborclientes` → vira `harborclientes.onmicrosoft.com` e `harborclientes.ciamlogin.com`
   - **Country/Region**: **Brazil**. **Não muda depois** (define onde ficam os dados dos clientes).
6. **Next: Add a subscription**:
   - **Subscription**: a mesma do ACA/AKS
   - **Resource group**: **Create new** → `rg-ecommerce-identity` → **OK**
7. **Next: Review + Create** → **Create**. Leva até **30 min**; acompanhe no sino de **Notifications**.

> Cobrança do External ID é por usuário ativo no mês (MAU) e tem faixa gratuita que cobre o lab com folga. Confira a página de preços antes de usar em produção.

### A2. Entrar no tenant e anotar os IDs

1. Ícone de **engrenagem (Settings)** → **Directories + subscriptions** → na lista, `Harbor Clientes` → **Switch**.
2. **Entra ID** → **Overview** → **Basic information** (ou **Tenant overview**):
   - **Tenant ID** → `TENANT_ID`
   - **Primary domain** `harborclientes.onmicrosoft.com` → `TENANT_SUBDOMAIN=harborclientes`

Confira se o tenant responde (deve devolver um JSON com `"issuer": "https://<TENANT_ID>.ciamlogin.com/<TENANT_ID>/v2.0"`):

```bash
curl -s "https://$TENANT_SUBDOMAIN.ciamlogin.com/$TENANT_ID/v2.0/.well-known/openid-configuration" | head -c 400
```

Esse issuer é o que o APIM e o Users vão validar. Repare: o **host** do issuer é o **Tenant ID**, não o subdomínio. Por isso nunca escreva o issuer à mão; deixe o `openid-config` / `Authority` descobrir.

---

## Parte B — App registrations (no tenant **externo**)

São **dois** registros. O Angular não pede token "para ele mesmo": pede token **para a API**.

| Registro | Representa | Tipo |
|----------|-----------|------|
| `ecommerce-api` | Users/Products/Orders atrás do APIM | Recurso: expõe scope e app role |
| `ecommerce-spa` | O Angular | Cliente público SPA (PKCE, sem secret) |

### B1. Registrar a API

1. **Entra ID** → **App registrations** → **+ New registration**.
2. **Name**: `ecommerce-api`
3. **Supported account types**: **Accounts in this organizational directory only** (a única opção que importa num tenant externo).
4. **Redirect URI**: deixe vazio (API não faz login).
5. **Register**.
6. Na **Overview**, copie **Application (client) ID** → `API_CLIENT_ID`.

### B2. Expor o scope `Ecommerce.Access`

1. `ecommerce-api` → **Expose an API**.
2. **Application ID URI** → **Add** → aceite `api://<API_CLIENT_ID>` → **Save**.
3. **+ Add a scope**:
   - **Scope name**: `Ecommerce.Access`
   - **Who can consent?**: **Admins only**
   - **Admin consent display name**: `Acessar a loja Harbor`
   - **Admin consent description**: `Permite que o site da loja chame a API em nome do cliente logado.`
   - **State**: **Enabled**
4. **Add scope**. O valor completo é `api://<API_CLIENT_ID>/Ecommerce.Access` — é o `apiScope` do `environment.entra.ts`.

No token o scope chega curto, na claim `scp`: `"scp": "Ecommerce.Access"`. O APIM (`required-claims`) e o Users (`RequiredScope`) conferem esse valor.

### B3. Criar o app role `Admin`

Substitui o `userID === '...0001'` que hoje decide quem é admin no Angular.

1. `ecommerce-api` → **App roles** → **+ Create app role**.
2. **Display name**: `Administrador`
3. **Allowed member types**: **Users/Groups**
4. **Value**: `Admin` (exatamente assim; é o texto que vem em `roles` e que as políticas comparam)
5. **Description**: `Gerencia produtos e vê todos os pedidos.`
6. **Do you want to enable this app role?** ✔ → **Apply**.

A atribuição a um usuário fica para o passo **F3** (o usuário ainda não existe).

### B4. Forçar token v2

O APIM e o Users esperam `aud = <API_CLIENT_ID>` e issuer `…/v2.0`. Token v1 tem `aud = api://…` e outro issuer → **401**.

1. `ecommerce-api` → **Manifest**.
2. Procure `requestedAccessTokenVersion` (dentro de `"api": { … }`). Se estiver `null` ou `1`, troque para:

   ```json
   "requestedAccessTokenVersion": 2,
   ```

   Se o Portal abrir o manifest antigo (AAD Graph), a propriedade se chama `accessTokenAcceptedVersion`. Mesmo valor: `2`.
3. **Save**.

### B5. Colocar o e-mail no access token

O `/api/Users/me` grava o e-mail no Postgres (`UNIQUE "Email"`). Access token não traz `email` por padrão.

1. `ecommerce-api` → **Token configuration** → **+ Add optional claim**.
2. **Token type**: **Access** → marque `email` → **Add**.
3. Se aparecer *"Turn on the Microsoft Graph email permission"*, marque e confirme.

Sem isso o `/me` responde **400** `Token sem a claim 'email'`.

### B6. Registrar o Angular (SPA)

1. **App registrations** → **+ New registration**.
2. **Name**: `ecommerce-spa`
3. **Supported account types**: **Accounts in this organizational directory only**
4. **Redirect URI**: plataforma **Single-page application (SPA)** → `http://localhost:4200/` (com a barra final, igual ao `redirectUri` do `environment.entra.ts`).
5. **Register** → copie **Application (client) ID** → `SPA_CLIENT_ID`.
6. **Authentication**:
   - Confira que a URI está no bloco **Single-page application** (não em **Web**). Em **Web** o login falha com `AADSTS9002326`.
   - **Implicit grant and hybrid flows**: deixe **tudo desmarcado** (MSAL v5 usa Authorization Code + PKCE).
   - **Front-channel logout URL**: pode ficar vazio.
   - **Save**.

> O Entra só aceita redirect `http://` para **localhost**. Para servir o Angular do AKS/ACA com Entra, o frontend precisa de **HTTPS** com domínio (TLS no Ingress, Static Web Apps, etc.). No lab, rode o Angular na máquina com `npm run start:entra`, como já era no ACA.

### B7. Dar ao SPA permissão para chamar a API

1. `ecommerce-spa` → **API permissions** → **+ Add a permission**.
2. Aba **APIs my organization uses** → `ecommerce-api` → **Delegated permissions** → ✔ `Ecommerce.Access` → **Add permissions**.
3. Mantenha `Microsoft Graph › User.Read` (já vem) e confira `openid` / `offline_access` (o MSAL pede sozinho).
4. **Grant admin consent for Harbor Clientes** → **Yes**.
5. A coluna **Status** precisa mostrar o check verde em todas as linhas.

Num tenant externo o cliente **não** vê tela de consentimento. Se o consentimento de admin faltar, o login volta com `AADSTS65001`.

---

## Parte C — User flow (a tela de cadastro/login)

### C1. Atributo customizado `Gender` (opcional)

O cadastro atual pede sexo. Para continuar coletando:

1. **Entra ID** → **External Identities** → **Custom user attributes** → **+ Add**.
2. **Name**: `Gender` · **Data Type**: **String** · **Description**: `Sexo informado no cadastro` → **Create**.

### C2. Criar o user flow

1. **Entra ID** → **External Identities** → **User flows** → **+ New user flow**.
2. **Name**: `harbor-signup-signin`
3. **Identity providers**: **Email with password** (ou **Email one-time passcode**, se quiser login sem senha).
4. **User attributes** (o que o cadastro pede): ✔ **Display Name** (vira a claim `name` e o `PersonName` no Postgres) · ✔ **Gender** (se fez o C1). **Email Address** já vem obrigatório.
5. **Create**.

Dropdown para o Gender (em vez de texto livre):

1. `harbor-signup-signin` → **Page layouts** → linha **Gender**.
2. **User input type**: **RadioSingleSelect** (ou **DropdownSingleSelect**).
3. Valores: `Masculino` / `Feminino` / `Outro` (texto exibido = valor) → **Save**.

### C3. Ligar o user flow ao SPA

1. `harbor-signup-signin` → **Applications** → **+ Add application**.
2. ✔ `ecommerce-spa` → **Select**.

Cada aplicação usa **um** user flow. Sem este passo o login cai numa tela genérica sem a opção de criar conta.

### C4. Idioma e marca (opcional)

- `harbor-signup-signin` → **Languages** → habilite **Portuguese (Brazil)** e defina como padrão.
- **Entra ID** → **Company branding** → **Default sign-in** → **Edit**: logo (`microservice/public/harbor.jpg`), cor de fundo, textos.

### C5. Gender dentro do access token (opcional)

Só faça se fez o C1. Sem isto o app funciona e o `Gender` fica `NULL`.

1. **App registrations** → `ecommerce-api` → **Overview** → link em **Managed application in local directory** (abre a Enterprise application).
2. **Single sign-on** → **Attributes & Claims** → **Edit** → **+ Add new claim**:
   - **Name**: `gender`
   - **Source**: **Directory schema extension** → **Select Application**: `b2c-extensions-app` → selecione o atributo `Gender` → **Add**
   - **Save**
3. Volte em **App registrations** → `ecommerce-api` → **Manifest** → `"acceptMappedClaims": true` → **Save**.

Confira num token real (passo F2) se a claim apareceu como `gender`. Se vier com outro nome, ajuste `EntraExternalId__GenderClaim` no Users.

---

## Parte D — Backend com as variáveis do Entra

### D1. Rebuild das imagens que mudaram

Mudaram **Users**, **Gateway** e o **seed do Postgres**. Mesmas tags do roteiro anterior (o Container App / Deployment já aponta para elas).

**ACA:**

```bash
docker build -t "$ACR_LOGIN/ecommerce-users-api:aca" \
  -f User/eCommerceSolution.UsersService/eCommerce.API/Dockerfile \
  User/eCommerceSolution.UsersService
docker build -t "$ACR_LOGIN/ecommerce-api-gateway:aca" \
  -f Gateway/eCommerceSolution.ApiGateway/Dockerfile \
  Gateway/eCommerceSolution.ApiGateway
docker build -t "$ACR_LOGIN/ecommerce-postgres:init" docker/postgres

docker push "$ACR_LOGIN/ecommerce-users-api:aca"
docker push "$ACR_LOGIN/ecommerce-api-gateway:aca"
docker push "$ACR_LOGIN/ecommerce-postgres:init"

GATEWAY_URL="https://$(az containerapp show -g "$RG_ACA" -n api-gateway-app --query properties.configuration.ingress.fqdn -o tsv)"
```

**AKS:**

```bash
docker build -t "$ACR_LOGIN/ecommerce-users-api:aks-v1" \
  -f User/eCommerceSolution.UsersService/eCommerce.API/Dockerfile \
  User/eCommerceSolution.UsersService
docker build -t "$ACR_LOGIN/ecommerce-api-gateway:aks-v2" \
  -f Gateway/eCommerceSolution.ApiGateway/Dockerfile \
  Gateway/eCommerceSolution.ApiGateway
docker build -t "$ACR_LOGIN/ecommerce-postgres:init" docker/postgres

docker push "$ACR_LOGIN/ecommerce-users-api:aks-v1"
docker push "$ACR_LOGIN/ecommerce-api-gateway:aks-v2"
docker push "$ACR_LOGIN/ecommerce-postgres:init"

GATEWAY_URL="http://$(kubectl get ingress ecommerce -n ecommerce -o jsonpath='{.status.loadBalancer.ingress[0].ip}')"
```

### D2. ACA: Postgres, Users e Gateway

Postgres: o disco do ACA é efêmero, então uma nova revisão com a imagem `:init` nova recria o schema já com `"Password" NULL` (e o seed `admin@gmail.com`). Se preferir manter os dados, rode [`docker/postgres/migrations/001-entra-external-id.sql`](../docker/postgres/migrations/001-entra-external-id.sql) em vez de atualizar a imagem.

```bash
az containerapp update -g "$RG_ACA" -n postgres-app --image "$ACR_LOGIN/ecommerce-postgres:init" \
  --revision-suffix entra1
```

Users (o `--set-env-vars` também força uma revisão nova, que puxa a imagem recém-publicada):

```bash
az containerapp update -g "$RG_ACA" -n users-api-app \
  --image "$ACR_LOGIN/ecommerce-users-api:aca" \
  --set-env-vars \
    EntraExternalId__TenantSubdomain="$TENANT_SUBDOMAIN" \
    EntraExternalId__TenantId="$TENANT_ID" \
    EntraExternalId__ApiClientId="$API_CLIENT_ID"
```

Gateway — o segredo vai como **secret** do Container App, não como texto na env:

```bash
az containerapp secret set -g "$RG_ACA" -n api-gateway-app \
  --secrets apim-shared-secret="$APIM_SHARED_SECRET"

az containerapp update -g "$RG_ACA" -n api-gateway-app \
  --image "$ACR_LOGIN/ecommerce-api-gateway:aca" \
  --set-env-vars \
    Apim__SharedSecret=secretref:apim-shared-secret \
    Cors__Origins=https://cors-handled-by-apim.invalid
```

`Cors__Origins` com uma origem que não existe faz o Ocelot **parar de emitir** cabeçalhos CORS: quem responde CORS agora é o APIM. Com os dois emitindo, o navegador recusa (`Access-Control-Allow-Origin` duplicado).

Confira a porta dos fundos fechada:

```bash
curl -s "$GATEWAY_URL/health"                       # 200 {"status":"ok"...}  (/health continua aberto)
curl -s -o /dev/null -w "%{http_code}\n" "$GATEWAY_URL/api/products"   # 403
curl -s "$GATEWAY_URL/api/products" -H "X-Apim-Secret: $APIM_SHARED_SECRET" | head -c 200   # 200
```

### D3. AKS: ConfigMap, Secret e rollout

Os YAML de `users` e `gateway` já leem estes objetos (com `optional: true`, então antes deles nada quebrava).

```bash
kubectl create configmap entra-external-id -n ecommerce \
  --from-literal=tenant-subdomain="$TENANT_SUBDOMAIN" \
  --from-literal=tenant-id="$TENANT_ID" \
  --from-literal=api-client-id="$API_CLIENT_ID"

kubectl create secret generic apim-gateway -n ecommerce \
  --from-literal=shared-secret="$APIM_SHARED_SECRET"

apply_acr k8s/users.yaml
apply_acr k8s/gateway.yaml      # já traz Cors__Origins=https://cors-handled-by-apim.invalid

# imagePullPolicy: Always + mesma tag → rollout restart puxa a imagem nova
kubectl rollout restart deploy/users-api deploy/gateway -n ecommerce
kubectl rollout status deploy/users-api -n ecommerce --timeout=180s
kubectl rollout status deploy/gateway -n ecommerce --timeout=180s
```

Postgres com PVC **guarda** os dados: a imagem nova não reexecuta o `initdb`. Rode a migração:

```bash
kubectl exec -it -n ecommerce-data postgres-0 -- \
  psql -U postgres -d eCommerceUsers -c 'ALTER TABLE public."Users" ALTER COLUMN "Password" DROP NOT NULL;'
```

Mesmos três `curl` do D2, trocando por `GATEWAY_URL=http://<INGRESS_IP>`.

> O Ingress continua expondo `/api` direto para o gateway. Com o segredo ativo isso responde **403**; o único caminho que funciona é o APIM.

---

## Parte E — Azure API Management (no diretório **workforce**)

Volte para o diretório da assinatura: **Settings** → **Directories + subscriptions** → **Switch**.

### E1. Criar o APIM

1. Portal → **+ Create a resource** → busque **API Management** → **Create**.
2. Aba **Basics**:
   - **Subscription**: a do lab
   - **Resource group**: **Create new** → `rg-ecommerce-apim`
   - **Region**: `Brazil South` (mesma do ACA/AKS)
   - **Resource name**: `apim-ecommerce-lab` → `APIM` (vira `https://apim-ecommerce-lab.azure-api.net`)
   - **Organization name**: `Harbor eCommerce`
   - **Administrator email**: o seu
   - **Pricing tier**: **Consumption**
3. **Review + create** → **Create**.

| Tier | Para quê | Observação |
|------|----------|------------|
| **Consumption** | Lab (recomendado) | Sobe em minutos, cobra por chamada com faixa gratuita mensal. **Não** tem `rate-limit-by-key` (o Ocelot segue limitando). Pode ter *cold start* na primeira chamada. |
| **Developer** | Testar tudo, inclusive developer portal e `rate-limit-by-key` | Custo fixo por hora mesmo parado; leva ~30–45 min para provisionar; sem SLA. |
| **Basic v2 / Standard v2** | Produção | Rate limit, VNet (Standard v2), SLA. |

Equivalente na CLI:

```bash
az group create -n "$RG_APIM" -l "$LOCATION"
az apim create -g "$RG_APIM" -n "$APIM" -l "$LOCATION" \
  --sku-name Consumption \
  --publisher-email "$APIM_PUBLISHER_EMAIL" --publisher-name "$APIM_PUBLISHER_NAME"
```

### E2. Named values

As políticas usam `{{nome}}` e não têm nenhum ID colado no XML.

1. `apim-ecommerce-lab` → **APIs** → **Named values** → **+ Add**, quatro vezes:

| Name | Display name | Type | Value |
|------|-------------|------|-------|
| `entra-tenant-subdomain` | igual | **Plain** | `$TENANT_SUBDOMAIN` |
| `entra-tenant-id` | igual | **Plain** | `$TENANT_ID` |
| `entra-api-client-id` | igual | **Plain** | `$API_CLIENT_ID` |
| `gateway-shared-secret` | igual | **Secret** | `$APIM_SHARED_SECRET` |

```bash
az apim nv create -g "$RG_APIM" -n "$APIM" --named-value-id entra-tenant-subdomain --display-name entra-tenant-subdomain --value "$TENANT_SUBDOMAIN"
az apim nv create -g "$RG_APIM" -n "$APIM" --named-value-id entra-tenant-id        --display-name entra-tenant-id        --value "$TENANT_ID"
az apim nv create -g "$RG_APIM" -n "$APIM" --named-value-id entra-api-client-id    --display-name entra-api-client-id    --value "$API_CLIENT_ID"
az apim nv create -g "$RG_APIM" -n "$APIM" --named-value-id gateway-shared-secret  --display-name gateway-shared-secret  --value "$APIM_SHARED_SECRET" --secret true
```

### E3. Importar a API apontando para o Ocelot

O arquivo [`apim/openapi/ecommerce-api.yaml`](../apim/openapi/ecommerce-api.yaml) já tem as 15 operações com os mesmos caminhos do Ocelot. Rotas que **não** estão nele (ex.: `/api/Users/{userID}`, `/api/Auth/login`) respondem **404** no APIM.

1. **APIs** → **+ Add API** → **Create from definition** → **OpenAPI**.
2. **Full**.
3. **OpenAPI specification**: **Select a file** → `apim/openapi/ecommerce-api.yaml`.
4. **Display name**: `ecommerce` · **Name**: `ecommerce`
5. **API URL suffix**: `ecommerce` → a URL pública fica `https://$APIM.azure-api.net/ecommerce/api/...`
6. **Create**.
7. Aba **Settings** da API:
   - **Web service URL**: `$GATEWAY_URL` (sem `/api` no fim). Ex.: `https://api-gateway-app.<hash>.brazilsouth.azurecontainerapps.io`
   - **Subscription required**: **desmarcar** (o Angular se identifica pelo token, não por subscription key)
   - **Save**

O APIM tira o sufixo e repassa o resto: `…/ecommerce/api/products` → `$GATEWAY_URL/api/products`.

```bash
az apim api import -g "$RG_APIM" -n "$APIM" \
  --api-id ecommerce --path ecommerce --display-name ecommerce \
  --specification-format OpenApi --specification-path apim/openapi/ecommerce-api.yaml \
  --service-url "$GATEWAY_URL" --subscription-required false --protocols https
```

### E4. Policy fragments

1. **APIs** → **Policy fragments** → **+ Create**.
2. **Name**: `ecommerce-validate-user` → cole [`apim/policy-fragments/ecommerce-validate-user.xml`](../apim/policy-fragments/ecommerce-validate-user.xml) → **Create**.
3. Repita com `ecommerce-require-admin` ← [`apim/policy-fragments/ecommerce-require-admin.xml`](../apim/policy-fragments/ecommerce-require-admin.xml).

O nome do fragment tem de ser **exatamente** esse: é o `fragment-id` que as políticas incluem.

### E5. Política da API (todas as operações)

1. **APIs** → `ecommerce` → **Design** → **All operations**.
2. **Inbound processing** → ícone **`</>`** (policy code editor).
3. Substitua tudo por [`apim/policies/api-ecommerce.xml`](../apim/policies/api-ecommerce.xml) → **Save**.

Ela faz: CORS para `http://localhost:4200`, remove `ClientId` (que no Ocelot fura o rate limit com `admin-client`), injeta `X-Apim-Secret`. No tier Developer+ descomente o `rate-limit-by-key`.

### E6. Políticas por operação

Mesmo caminho: **Design** → clique na operação → **Inbound processing** → **`</>`** → cole → **Save**.

| Operação (OpenAPI `operationId`) | Arquivo | Quem passa |
|----------------------------------|---------|-----------|
| `listProducts`, `getProductById`, `searchProducts` | — (nenhum) | Qualquer um (catálogo público) |
| `getCurrentUser`, `getOrderById` | [`op-user.xml`](../apim/policies/op-user.xml) | Token válido com `Ecommerce.Access` |
| `createOrder` | [`op-create-order.xml`](../apim/policies/op-create-order.xml) | Token válido **e** `userID` do corpo == `oid` |
| `getOrdersByUser` | [`op-orders-by-user.xml`](../apim/policies/op-orders-by-user.xml) | `{userId}` == `oid`, ou Admin |
| `createProduct`, `updateProduct`, `deleteProduct`, `listOrders`, `getOrdersByProduct`, `getOrdersByDate`, `updateOrder`, `deleteOrder` | [`op-admin.xml`](../apim/policies/op-admin.xml) | Token válido **e** `roles` contém `Admin` |

### E7. Testar no Portal

`ecommerce` → aba **Test**:

| Operação | Esperado |
|----------|----------|
| `listProducts` | **200** com a lista (prova backend + segredo) |
| `getCurrentUser` sem header | **401** `Token ausente ou inválido` |
| `createProduct` sem header | **401** |

**403** `Acesse a API pelo Azure API Management.` em `listProducts` = segredo diferente entre o named value e o gateway. O botão **Trace** (ao lado de **Send**) mostra cada política e a resposta do backend.

```bash
APIM_URL="https://$APIM.azure-api.net/ecommerce"
curl -s "$APIM_URL/api/products" | head -c 200                       # 200
curl -s -o /dev/null -w "%{http_code}\n" "$APIM_URL/api/Users/me"     # 401
curl -s -o /dev/null -w "%{http_code}\n" "$APIM_URL/api/Users/11111111-1111-1111-1111-111111111111"  # 404 (não exposto)
```

---

## Parte F — Frontend e primeiro login

### F1. Configurar e subir o Angular

Edite `microservice/src/environment.entra.ts`:

```ts
const apimBase = 'https://<APIM>.azure-api.net/ecommerce';
...
clientId: '<SPA_CLIENT_ID>',
authority: 'https://<TENANT_SUBDOMAIN>.ciamlogin.com/',
knownAuthority: '<TENANT_SUBDOMAIN>.ciamlogin.com',
apiScope: 'api://<API_CLIENT_ID>/Ecommerce.Access',
```

```bash
cd microservice
npm install
npm run start:entra
```

Abra `http://localhost:4200`. O catálogo carrega **sem** login (GET de produtos não leva token).

### F2. Criar a conta e conferir o token

1. **Cadastro** → abre a página hospedada `https://<SUBDOMINIO>.ciamlogin.com/...` direto na criação de conta (`prompt=create`).
2. E-mail → código de verificação → senha → **Display Name** / **Gender**.
3. Volta para `http://localhost:4200/`. O Angular chama `GET …/api/Users/me` e o Users grava a linha com `UserID = oid`.

Confira:

```bash
# ACA (abre o psql; cole o SELECT abaixo e saia com \q)
az containerapp exec -g "$RG_ACA" -n postgres-app --command "psql -U postgres -d eCommerceUsers"
# AKS
kubectl exec -it -n ecommerce-data postgres-0 -- psql -U postgres -d eCommerceUsers
```

```sql
SELECT "UserID", "Email", "PersonName", "Gender", "Password" IS NULL AS sem_senha_local FROM "Users";
```

Para ver o token: DevTools → **Network** → a chamada `me` → **Request Headers** → `Authorization: Bearer eyJ…` → cole em <https://jwt.ms>. Precisa ter:

| Claim | Valor |
|-------|-------|
| `iss` | `https://<TENANT_ID>.ciamlogin.com/<TENANT_ID>/v2.0` |
| `aud` | `<API_CLIENT_ID>` (GUID, **sem** `api://`) |
| `ver` | `2.0` |
| `scp` | `Ecommerce.Access` |
| `oid` | GUID do usuário = `UserID` no Postgres |
| `email` | e-mail do cadastro |
| `roles` | ainda **ausente** (vem no F3) |

Faça um pedido pelo carrinho: o `createOrder` passa (corpo com `userID = oid`) e o Orders valida o usuário chamando `users-api` direto.

### F3. Virar Admin

1. Diretório **externo** → **Entra ID** → **Enterprise apps** → `ecommerce-api` (é a Enterprise app da **API**, não a do SPA).
2. **Users and groups** → **+ Add user/group**.
3. **Users** → **None Selected** → escolha a sua conta → **Select**.
4. **Select a role** → `Administrador` → **Select** → **Assign**.
5. No site: **Sair** → **Login** de novo. O role só aparece em token **novo**.

Agora o token tem `"roles": ["Admin"]`, o `/me` devolve `isAdmin: true` e o menu mostra **Produtos** / **Pedidos** de administração. Em <https://jwt.ms> a claim `roles` aparece.

Prova de que o bloqueio está no servidor, não no menu: com uma conta **sem** o role, copie o token e chame

```bash
curl -s -o /dev/null -w "%{http_code}\n" -X DELETE "$APIM_URL/api/products/<PRODUCT_ID>" \
  -H "Authorization: Bearer <TOKEN_DE_CLIENTE>"     # 403
```

### F4. Usuários de teste

- Crie mais contas pelo **Cadastro** do site (é o fluxo de cliente real).
- Para listar/remover: **Entra ID** → **Users**. O usuário apagado no Entra **continua** no Postgres (o `/me` só cria/atualiza).
- O seed `admin@gmail.com` / `admin` só serve no modo antigo (Compose, `npm run start:azure`). Com Entra, não há login local. Se cadastrar `admin@gmail.com` no Entra, o `/me` responde **409** (e-mail já usado por outro `UserID`): use outro e-mail ou apague a linha do seed.

---

## Ordem compacta (checklist)

| # | Onde | O quê |
|---|------|-------|
| 1 | Entra (workforce) | **Manage tenants** → criar tenant **External** (até 30 min) |
| 2 | Tenant externo | Anotar `TENANT_ID` e `TENANT_SUBDOMAIN`; `curl` no openid-configuration |
| 3 | Tenant externo | `ecommerce-api`: scope `Ecommerce.Access`, app role `Admin`, token **v2**, optional claim `email` |
| 4 | Tenant externo | `ecommerce-spa`: plataforma **SPA** `http://localhost:4200/`, permissão `Ecommerce.Access`, **Grant admin consent** |
| 5 | Tenant externo | User flow `harbor-signup-signin` (+ `Gender`) → **Applications** → `ecommerce-spa` |
| 6 | ACA / AKS | Rebuild Users + Gateway + Postgres; env `EntraExternalId__*`, `Apim__SharedSecret`, `Cors__Origins` |
| 7 | ACA / AKS | `curl` direto no gateway → **403**; com `X-Apim-Secret` → **200** |
| 8 | Workforce | APIM **Consumption** |
| 9 | APIM | 4 named values (1 **Secret**) |
| 10 | APIM | Importar OpenAPI, sufixo `ecommerce`, Web service URL = gateway, **sem** subscription |
| 11 | APIM | 2 policy fragments → política da API → políticas por operação |
| 12 | Máquina | `environment.entra.ts` + `npm run start:entra` → cadastro → conferir token |
| 13 | Tenant externo | Enterprise apps → `ecommerce-api` → atribuir `Administrador` → logout/login |

---

## Erros comuns

| Sintoma | Causa | Correção |
|---------|-------|----------|
| `AADSTS50011` redirect URI mismatch | URI diferente da cadastrada (barra final, porta, `127.0.0.1`) | B6: cadastre **exatamente** a URL do `redirectUri` |
| `AADSTS9002326` cross-origin token redemption | URI cadastrada em **Web**, não em **SPA** | B6: apague de Web e cadastre em Single-page application |
| `AADSTS65001` sem consentimento | Faltou **Grant admin consent** | B7 passo 4 |
| `AADSTS500011` resource principal not found | `apiScope` com Client ID errado ou API em outro tenant | Confira `api://<API_CLIENT_ID>/Ecommerce.Access` e o diretório |
| Login abre, mas sem "Criar conta" | User flow não ligado ao SPA | C3 |
| APIM **401** com token aparentemente certo | Token v1 (`aud` = `api://…`, `ver` = `1.0`) | B4 e **novo login** (token antigo fica em cache no MSAL: Sair → Login) |
| APIM **401**, `aud` certo | Named value `entra-tenant-id`/`subdomain` errado (openid-config não carrega) | **Trace** na aba Test mostra o erro do `validate-jwt` |
| APIM **401** só em operações de usuário | Falta `Ecommerce.Access` em `scp` | B7: permissão + consentimento; login de novo |
| APIM **403** "Apenas administradores" | Sem `roles: Admin` | F3 na Enterprise app da **API**; Sair e Login |
| APIM **403** `Acesse a API pelo Azure API Management.` | `gateway-shared-secret` ≠ `Apim__SharedSecret` | Iguale os dois (E2 / D2 / D3) |
| APIM **404** | Rota não está no OpenAPI, ou sufixo esquecido no `environment.entra.ts` | Use `https://$APIM.azure-api.net/ecommerce/api/...` |
| Navegador: erro de CORS | Origem fora do `<allowed-origins>`, ou cabeçalho duplicado (Ocelot ainda emitindo) | E5: inclua a origem; D2/D3: `Cors__Origins=https://cors-handled-by-apim.invalid` |
| `/api/Users/me` **400** `Token sem a claim 'email'` | Optional claim faltando | B5; Sair e Login |
| `/api/Users/me` **409** | E-mail já existe numa conta local (seed) | F4 |
| `/api/Users/me` **500** `No authentication handler` | Users sem `EntraExternalId__*` | D2 / D3 |
| Checkout **400** "usuário não encontrado" | `/me` não rodou (linha não criada) | Recarregue a página logado; confira F2 |
| Checkout **403** "userID do pedido não corresponde" | `currentUserId` velho no `localStorage` (sessão do modo antigo) | Sair → Login |
| 1ª chamada lenta (segundos) | Cold start do tier Consumption | Normal no lab |

---

## O que muda

| | Antes | Depois |
|-|-------|--------|
| Quem guarda a senha | Postgres (`"Password"` em texto) | Entra External ID |
| Tela de login/cadastro | Componentes Angular | Página hospedada `ciamlogin.com` (user flow) |
| Token | `"token"` fixo | JWT assinado (RS256), validado pelo APIM e pelo Users |
| Quem é admin | `userID === '…0001'` no Angular | App role `Admin` → claim `roles` |
| Porta de entrada pública | FQDN do gateway / IP do Ingress | `https://<APIM>.azure-api.net/ecommerce` |
| Gateway direto | Aberto | **403** sem `X-Apim-Secret` |
| CORS | Ocelot | APIM |
| Rate limit | Ocelot (furável com `ClientId: admin-client`) | Ocelot (header removido no APIM) + `rate-limit-by-key` no APIM (tier Developer+) |
| UserID | `Guid.NewGuid()` no cadastro | `oid` do Entra |

Modo antigo continua funcionando sem Entra: `docker compose up` / `npm start` / `npm run start:azure` usam `environment.ts` / `environment.azure.ts` (`entra: null`) e o `AuthController` local.

---

## De B2C para External ID

Para quem estudou pelo material de B2C:

| Azure AD B2C | Entra External ID (tenant externo) |
|--------------|-------------------------------------|
| Recurso "Azure AD B2C" + diretório B2C | **Manage tenants** → **External** |
| `https://<tenant>.b2clogin.com/<tenant>.onmicrosoft.com/B2C_1_susi/v2.0` | `https://<SUBDOMINIO>.ciamlogin.com/` (o user flow vem da aplicação, não da URL) |
| `knownAuthorities: ['<tenant>.b2clogin.com']` | `knownAuthorities: ['<SUBDOMINIO>.ciamlogin.com']` |
| User flow `B2C_1_signupsignin` | User flow ligado à aplicação (C3) |
| Custom policies (IEF/XML) | Não existem; extensões via **Custom authentication extensions** |
| `validate-jwt` com `…/tfp/<tenant>/<policy>/v2.0/.well-known/openid-configuration` | `validate-jwt` com `https://<SUBDOMINIO>.ciamlogin.com/<TENANT_ID>/v2.0/.well-known/openid-configuration` |
| Claim de ID do usuário: `sub`/`oid` por policy | `oid` |
| Grupos/roles exigiam API Connector | App roles e grupos nativos (B3 / F3) |

---

## Desligar (custo)

| Recurso | Pausar | Apagar |
|---------|--------|--------|
| APIM Consumption | Não precisa: sem chamadas, sem custo | `az group delete -n "$RG_APIM"` |
| APIM Developer | Não pausa | `az group delete -n "$RG_APIM"` |
| Tenant externo | Sem custo abaixo da faixa gratuita de MAU | Apague usuários e app registrations; depois **Manage tenants** → **Delete** (pode pedir para remover a assinatura vinculada) |
| ACA / AKS | Igual ao [roteiro anterior](roteiro-aca-depois-aks.md) | Idem |

Ao religar o AKS (`az aks start`), se o IP do Ingress mudar, atualize **Web service URL** da API no APIM (E3). O Angular não muda: ele só conhece o APIM.

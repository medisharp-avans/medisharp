# Medisharp

Medisharp is made of three parts:

| Folder      | What it is   | Tech                      |
| ----------- | ------------ | ------------------------- |
| `Client/`   | Web frontend | Vue 3, Vite, TypeScript   |
| `Server/`   | REST API     | ASP.NET Core (.NET 10)    |
| `Database/` | SQL scripts  | Microsoft SQL Server 2022 |

There are two compose files:

| File                      | Used for                                                                                    |
| ------------------------- | ------------------------------------------------------------------------------------------- |
| `docker-compose.yml`      | **Development.** Starts only SQL Server. You run the client and server on your own machine. |
| `docker-compose.prod.yml` | **Staging and production (Dokploy).** Runs the database, server and client together.       |

---

## 1. Development environment

### 1.1 What you need

- [Git](https://git-scm.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine with the Compose plugin)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) `22.18+` or `24.12+`
- An editor: VS Code (with the _Vue (Official)_ and _C# Dev Kit_ extensions) or Visual Studio 2022+

Optional: SQL Server Management Studio, Azure Data Studio or the VS Code _SQL Server (mssql)_ extension to look inside the database.

### 1.2 Clone the repository

```sh
git clone https://github.com/medisharp-avans/medisharp.git
cd medisharp
git checkout development
```

Work on a branch from `development` and open pull requests into `development`. A merge from `development` into `main` is deployed to staging, and after approval to production (see section 2).

### 1.3 Create the `.env` file

Create a file called `.env` in the root of the repository (next to `docker-compose.yml`). Every value has a default, so the file can be empty, but it is good practice to set your own password:

```env
MSSQL_SA_PASSWORD=YourStrong!Passw0rd
MSSQL_PORT=1433
```

See [section 3](#3-environment-variables-env) for every variable. `.env*` files are in `.gitignore`. **Never commit them.**

### 1.4 Start the database

Start SQL Server (Compose creates the network and volumes for you):

```sh
docker compose up -d
```

Check that it is running:

```sh
docker compose ps
docker compose logs -f mssql_db
```

SQL Server is now reachable at `localhost,1433` with user `sa` and the password from `.env` (default `Medisharp@123`).

Stop the database with `docker compose down`. Your data is kept in the `mssqlsystem` and `mssqluser` volumes. Use `docker compose down -v` to wipe it.

### 1.5 Run the server (API)

```sh
cd Server
dotnet restore
dotnet run --launch-profile http
```

The API runs at <http://localhost:5174>. Try <http://localhost:5174/weatherforecast>. In Development mode the OpenAPI document is at <http://localhost:5174/openapi/v1.json>.

To point the server at the dev database, add a connection string to `Server/appsettings.Development.json` (or use `dotnet user-secrets` so the password stays out of Git):

```sh
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=Medisharp;User Id=sa;Password=Medisharp@123;TrustServerCertificate=True;"
```

You can also open `Server/Server.slnx` in Visual Studio and press F5.

### 1.6 Run the client (frontend)

```sh
cd Client
npm install
npm run dev
```

The app runs at <http://localhost:5173>.

In production nginx sends every `/api/...` request to the server (see `Client/nginx.conf`). To get the same behaviour in development, add a proxy to `Client/vite.config.ts`:

```ts
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:5174',
      rewrite: (path) => path.replace(/^\/api/, ''),
    },
  },
},
```

Other useful commands:

| Command                   | What it does                                                    |
| ------------------------- | --------------------------------------------------------------- |
| `npm run build`           | Type-check and build for production                             |
| `npm run test:unit`       | Run unit tests with Vitest (watch mode)                         |
| `npm run test:coverage`   | Run unit tests once with a coverage report in `coverage/`       |
| `npm run test:lighthouse` | Run Lighthouse CI on every route (needs `npm run build` first)  |
| `npm run test:a11y`       | Run axe accessibility checks on every route (needs a build too) |
| `npm run check`           | Lint and format check without fixing, the same as CI            |
| `npm run lint`            | Lint with oxlint and ESLint                                     |
| `npm run format`          | Format `src/` with Prettier                                     |

#### Lighthouse and accessibility checks

Both checks test the built app, so build it first. The list of pages comes from `src/router/routes.ts` (via `e2e/routes.ts`), so a new route is tested automatically.

```sh
cd Client
npm run build
npm run test:lighthouse   # reports in lighthouse-reports/
npm run test:a11y         # reports in a11y-reports/
```

Both commands start their own preview server on <http://localhost:4173>, so that port has to be free.

Both checks use your installed Google Chrome. The Lighthouse thresholds live in `lighthouserc.cjs`; the coverage settings live in `vitest.config.ts`. In CI the same checks run in the `client` and `client-quality` jobs, and the reports are uploaded as artifacts on the workflow run.

### 1.7 Optional: test the production setup locally

You can run the full production stack on your machine before you deploy. `docker-compose.build.yml` adds the `build:` settings and a local port, so the images are built from your code instead of pulled from GHCR:

```sh
# create .env.prod first, see section 3.2
docker compose -f docker-compose.prod.yml -f docker-compose.build.yml --env-file .env.prod up -d --build
```

The site is served at `http://localhost:${CLIENT_PORT}` (default port 8080).

---

## 2. Staging and production (GitHub Actions + Dokploy)

### 2.1 How a change reaches production

GitHub Actions builds the images once. Dokploy only runs them. Production never gets its own build: it gets the exact images that were tested on staging.

```text
PR into development or main ──► CI (.github/workflows/ci.yml): lint, unit tests, end-to-end tests

merge into main ──► .github/workflows/docker.yml
                    1. build-and-push     build server + client, push to GHCR as :main and :sha-<commit>
                    2. deploy-staging     tag sha-<commit> as :staging, ask Dokploy (staging) to deploy
                    3. deploy-production  waits for a reviewer, then tags the same sha-<commit> as :production
                                          and asks Dokploy (production) to deploy
```

The reviewer checks the change on staging before approving the production job. That approval is the acceptance step.

| OTAP        | Where                                              | Branch / trigger                     |
| ----------- | -------------------------------------------------- | ------------------------------------ |
| Development | your own machine (section 1)                       | feature branches                     |
| Test        | GitHub Actions CI                                  | every pull request                   |
| Acceptance  | Dokploy environment `staging`                      | every merge into `main`              |
| Production  | Dokploy environment `production`                   | same build, after a reviewer approves |

### 2.2 What runs on Dokploy

Both environments use `docker-compose.prod.yml`. It starts three services:

```text
Internet ──► Dokploy (Traefik) ──► client (nginx :80) ──/api/──► server (:8080) ──► db (SQL Server :1433)
                                   network: frontend            networks: frontend + backend   network: backend
```

- **db** runs SQL Server 2022 (Express edition by default). Data is stored in the `mssql_data` volume. It has a health check.
- **server** is the image `ghcr.io/medisharp-avans/medisharp-server:${IMAGE_TAG}`. It listens on port 8080 inside Docker and is not exposed to the internet.
- **client** is the image `ghcr.io/medisharp-avans/medisharp-client:${IMAGE_TAG}`. nginx serves the Vue app and proxies `/api/*` to the server.

The compose file has no `build:` settings, so Dokploy never builds on the server. `pull_policy: always` makes every deploy fetch the newest image for the tag. Only the client needs a public domain.

### 2.3 Requirements

- A server (VPS) with Dokploy installed. Two SQL Servers (staging and production) need about **4 GB RAM** together.
- A domain name with `A` records for the staging and production hosts pointing to the server's IP.
- The GHCR packages `medisharp-server` and `medisharp-client` must be public, or ghcr.io must be added as a registry in Dokploy.
- A Dokploy API key (Settings → Profile → API/CLI) for GitHub Actions.

### 2.4 Dokploy: one Compose service per environment

The Dokploy project `Medisharp` has two environments, `staging` and `production`, each with one Compose service. Set both the same way, with the values from the table:

1. **General** tab:
   - **Provider:** GitHub, **Repository:** `medisharp-avans/medisharp`, **Branch:** `main`
   - **Compose Path:** `./docker-compose.prod.yml`
   - **Autodeploy:** off. GitHub Actions starts every deploy, after the images are built.
2. **Environment** tab (see [section 3.2](#32-production-docker-composeprodyml)):

   ```env
   IMAGE_TAG=staging            # production: IMAGE_TAG=production
   ASPNETCORE_ENVIRONMENT=Staging   # production: Production
   MSSQL_SA_PASSWORD=<a long, strong password, different per environment>
   MSSQL_DB_NAME=Medisharp
   ```

3. **Domains** tab → **Add Domain**: **Service Name** `client`, **Host** your staging or production host, **Path** `/`, **Container Port** `80`, **HTTPS** on with Let's Encrypt. Dokploy adds the Traefik labels and the `dokploy-network` for you.
4. **Advanced** tab: leave **Run Command** empty.

### 2.5 GitHub: environments, secrets and variables

Settings → Environments. The jobs in `docker.yml` read their values from these environments:

| GitHub environment | Dokploy environment | Deployment branches | Protection          | Secret             | Variables                                                        |
| ------------------ | ------------------- | ------------------- | ------------------- | ------------------ | ---------------------------------------------------------------- |
| `acceptance`       | `staging`           | `main`              | none                | `DOKPLOY_API_KEY`  | `DOKPLOY_URL`, `DOKPLOY_COMPOSE_ID` (ID of the staging service)  |
| `production`       | `production`        | `main`              | required reviewers  | `DOKPLOY_API_KEY`  | `DOKPLOY_URL`, `DOKPLOY_COMPOSE_ID` (ID of the production service) |

- `DOKPLOY_URL` is the address of the Dokploy dashboard, without a trailing `/`.
- `DOKPLOY_COMPOSE_ID` is the last part of the URL of the Compose service in Dokploy, after `/services/compose/`.

The database password and other runtime settings live only in Dokploy, not in GitHub.

### 2.6 Rolling back

Every build stays in GHCR as `sha-<commit>`. To go back, run the **Build and deploy** workflow again for an older commit (Actions → Build and deploy → the old run → **Re-run all jobs**). It moves `:staging` and `:production` back to that build.

### 2.7 Maintenance

- **Logs:** the **Logs** tab, pick `db`, `server` or `client`.
- **Backups:** the database lives in the `mssql_data` volume. Use the **Backups** or **Volume Backups** tab in Dokploy, or make a backup by hand: open the `db` container in the **Containers** tab (or `docker exec` into it on the server) and run

  ```sh
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
    -Q "BACKUP DATABASE [Medisharp] TO DISK = N'/var/opt/mssql/data/medisharp.bak'"
  ```

- **Changing the SA password:** the password is saved inside the database the first time the volume is created. Changing `MSSQL_SA_PASSWORD` later does **not** change the real password. Change it in SQL Server first (`ALTER LOGIN sa WITH PASSWORD = '...'`), then update the variable in Dokploy and redeploy.
- **HTTPS redirect.** `Program.cs` calls `UseHttpsRedirection()`, but the server container only listens on HTTP 8080 behind nginx and Traefik. That is fine, but ASP.NET logs a warning that it cannot find the HTTPS port.

---

## 3. Environment variables (`.env`)

Compose reads variables from a `.env` file in the same folder as the compose file. Locally you create this file yourself. On Dokploy you fill in the **Environment** tab and Dokploy creates the file.

### 3.1 Development (`docker-compose.yml`)

All variables are optional.

| Variable            | Default                                 | Description                                                               |
| ------------------- | --------------------------------------- | ------------------------------------------------------------------------- |
| `MSSQL_SA_PASSWORD` | `Medisharp@123`                         | Password of the `sa` user.                                                |
| `MSSQL_PORT`        | `1433`                                  | Port on your machine for SQL Server. Change it if 1433 is already in use. |
| `MSSQL_IMAGE`       | `mcr.microsoft.com/mssql/server:latest` | SQL Server Docker image.                                                  |
| `MSSQL_PRODUCT_ID`  | `Developer`                             | SQL Server edition (`Developer`, `Express`, ...).                         |
| `MSSQL_CONFIG_DIR`  | `./config/mssql`                        | Folder with `entrypoint.sh` and the `sql/` init scripts.                  |

Example `.env`:

```env
MSSQL_SA_PASSWORD=Medisharp@123
MSSQL_PORT=1433
```

### 3.2 Production (`docker-compose.prod.yml`)

Used for staging and production on Dokploy, and locally with `docker-compose.build.yml`.

| Variable                 | Required | Default                                      | Description                                                                               |
| ------------------------ | -------- | -------------------------------------------- | ----------------------------------------------------------------------------------------- |
| `MSSQL_SA_PASSWORD`      | **Yes**  | none, the deploy stops if it is missing      | Password of the `sa` user. Also used in the server's connection string.                   |
| `IMAGE_TAG`              | No       | `production`                                 | Which images to run: `staging` or `production` on Dokploy.                                |
| `ASPNETCORE_ENVIRONMENT` | No       | `Production`                                 | `Staging` on staging, `Production` on production.                                         |
| `MSSQL_DB_NAME`          | No       | `Medisharp`                                  | Name of the database the server connects to.                                              |
| `MSSQL_IMAGE`            | No       | `mcr.microsoft.com/mssql/server:2022-latest` | SQL Server Docker image.                                                                  |
| `MSSQL_PRODUCT_ID`       | No       | `Express`                                    | SQL Server edition. Express is free for production. Other editions need a license.        |
| `CLIENT_PORT`            | No       | `8080`                                       | Only locally (`docker-compose.build.yml`): port on your machine for the website.          |

Example `.env.prod` for running locally:

```env
MSSQL_SA_PASSWORD=ChangeMe-Use-A-Long-Random-Password-123!
MSSQL_DB_NAME=Medisharp
ASPNETCORE_ENVIRONMENT=Production
CLIENT_PORT=8080
```

### 3.3 Password rules

SQL Server will not start if the `sa` password is too weak. It must:

- be at least **8 characters** long, and
- contain characters from at least **three** of these four groups: uppercase letters, lowercase letters, digits, symbols.

Avoid `;` in the password, because it breaks the connection string. Avoid `$`, because Compose reads it as the start of a variable (write `$$` if you really need one).

### 3.4 Variables the server receives

Apart from `ASPNETCORE_ENVIRONMENT`, you do not set these in `.env`. `docker-compose.prod.yml` builds them for the server container:

| Variable                               | Value                                                                                                            |
| -------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`               | `${ASPNETCORE_ENVIRONMENT}` (default `Production`)                                                               |
| `ASPNETCORE_HTTP_PORTS`                | `8080`                                                                                                           |
| `ConnectionStrings__DefaultConnection` | `Server=db,1433;Database=${MSSQL_DB_NAME};User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;` |

In code you read the connection string with `builder.Configuration.GetConnectionString("DefaultConnection")`.

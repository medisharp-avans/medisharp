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
| `docker-compose.prod.yml` | **Production (Dokploy).** Builds and runs the database, server and client together.         |

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

Work on a branch from `development` and open pull requests into `development`. `production` is what gets deployed.

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

You can run the full production stack on your machine before you deploy:

```sh
# create .env.prod first, see section 3.2
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

The site is served at `http://localhost:${CLIENT_PORT}` (default port 80).

---

## 2. Production on Dokploy

Production uses `docker-compose.prod.yml`. It starts three services:

```text
Internet ──► Dokploy (Traefik) ──► client (nginx :80) ──/api/──► server (:8080) ──► db (SQL Server :1433)
                                   network: frontend            networks: frontend + backend   network: backend
```

- **db** runs SQL Server 2022 (Express edition by default). Data is stored in the `mssql_data` volume. It has a health check.
- **server** is built from `Server/Dockerfile` (target `final`) and listens on port 8080 inside Docker. It is not exposed to the internet.
- **client** is built from `Client/Dockerfile`. nginx serves the Vue app and proxies `/api/*` to the server.

Only the client needs a public domain.

### 2.1 Requirements

- A server (VPS) with Dokploy installed: `curl -sSL https://dokploy.com/install.sh | sh`
- At least **2 GB RAM** free for SQL Server
- A domain name with an `A` record pointing to the server's IP
- Dokploy connected to GitHub (Settings → Git → GitHub) so it can read this repository

### 2.2 Create the Compose service

1. In Dokploy, open (or create) a **Project**, then click **Create Service → Compose**.
2. Give it a name, for example `medisharp`. Choose **Compose type: Docker Compose**.
3. Open the **General** tab and set the source:
   - **Provider:** GitHub
   - **Repository:** `medisharp-avans/medisharp`
   - **Branch:** `production`
   - **Compose Path:** `./docker-compose.prod.yml`
4. Click **Save**.

### 2.3 Set the environment variables

Open the **Environment** tab and paste the production variables (see [section 3](#3-environment-variables-env)):

```env
MSSQL_SA_PASSWORD=<a long, strong password>
MSSQL_DB_NAME=Medisharp
CLIENT_PORT=8085
```

Click **Save**. Dokploy writes these into a `.env` file next to the compose file, and Docker Compose reads it when it deploys.

> **Important:** Dokploy's Traefik proxy already uses ports 80 and 443 on the host. If `CLIENT_PORT` stays at its default of `80`, the deploy fails with _"port is already allocated"_. Set `CLIENT_PORT` to a free port (for example `8085`) and use a Dokploy domain for public traffic (next step).

### 2.4 Add a domain

1. Open the **Domains** tab and click **Add Domain**.
2. Fill in:
   - **Service Name:** `client`
   - **Host:** `medisharp.yourdomain.com`
   - **Path:** `/`
   - **Container Port:** `80`
   - **HTTPS:** on, **Certificate:** Let's Encrypt
3. Save.

Dokploy adds the Traefik labels and the `dokploy-network` to the `client` service for you. You do not have to change the compose file.

### 2.5 Deploy

Click **Deploy**. Follow the progress in the **Deployments** tab. The order is:

1. `db` starts and waits until its health check passes.
2. `server` starts.
3. `client` starts.

When it is done, open `https://medisharp.yourdomain.com`. The API is at `https://medisharp.yourdomain.com/api/...`.

To deploy on every push to `production`, turn on **Autodeploy** in the General tab (or copy the webhook URL from the Deployments tab into GitHub).

### 2.6 Things to check before the first deploy

- **`db-init` service is missing.** In `docker-compose.prod.yml`, `server` has `depends_on: db-init` with `condition: service_completed_successfully`, but there is no `db-init` service in the file. Compose refuses to start until you either add a `db-init` service (for example a container based on the same SQL Server image that runs `Database/*.sql` against `db` with `/opt/mssql-tools18/bin/sqlcmd` and then exits) or change the dependency to `db: condition: service_healthy`.
- **`container_name` is set** on every service. Dokploy recommends removing `container_name` because it can break logs, metrics and running more than one copy of the app on the same server. It works for a single deployment.
- **HTTPS redirect.** `Program.cs` calls `UseHttpsRedirection()`, but the server container only listens on HTTP 8080 behind nginx and Traefik. That is fine, but ASP.NET logs a warning that it cannot find the HTTPS port.

### 2.7 Maintenance

- **Logs:** the **Logs** tab, pick `db`, `server` or `client`.
- **Backups:** the database lives in the `mssql_data` volume. Make a backup on the server with:

  ```sh
  docker exec medisharp_db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
    -Q "BACKUP DATABASE [Medisharp] TO DISK = N'/var/opt/mssql/data/medisharp.bak'"
  docker cp medisharp_db:/var/opt/mssql/data/medisharp.bak ./medisharp.bak
  ```

- **Changing the SA password:** the password is saved inside the database the first time the volume is created. Changing `MSSQL_SA_PASSWORD` later does **not** change the real password. Change it in SQL Server first (`ALTER LOGIN sa WITH PASSWORD = '...'`), then update the variable in Dokploy and redeploy.

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

| Variable            | Required | Default                                      | Description                                                                                                 |
| ------------------- | -------- | -------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| `MSSQL_SA_PASSWORD` | **Yes**  | none, the deploy stops if it is missing      | Password of the `sa` user. Also used in the server's connection string.                                     |
| `JWT_KEY`           | **Yes**  | none, the deploy stops if it is missing      | Secret key that signs the login tokens (JWT). At least 32 characters, e.g. `openssl rand -base64 48`.       |
| `MSSQL_DB_NAME`     | No       | `Medisharp`                                  | Name of the database the server connects to.                                                                |
| `CLIENT_PORT`       | No       | `80`                                         | Host port for the website. **On Dokploy, set this to a free port such as `8085`**, because Traefik uses 80. |
| `MSSQL_IMAGE`       | No       | `mcr.microsoft.com/mssql/server:2022-latest` | SQL Server Docker image.                                                                                    |
| `MSSQL_PRODUCT_ID`  | No       | `Express`                                    | SQL Server edition. Express is free for production. Other editions need a license.                          |

Example:

```env
# =========================================
# DATABASE
# =========================================
MSSQL_SA_PASSWORD=ChangeMe-Use-A-Long-Random-Password-123!
MSSQL_DB_NAME=Medisharp

# =========================================
# CLIENT
# =========================================
# Host port the website is served on. Use a free port on Dokploy (Traefik owns 80/443).
CLIENT_PORT=8085
```

### 3.3 Password rules

SQL Server will not start if the `sa` password is too weak. It must:

- be at least **8 characters** long, and
- contain characters from at least **three** of these four groups: uppercase letters, lowercase letters, digits, symbols.

Avoid `;` in the password, because it breaks the connection string. Avoid `$`, because Compose reads it as the start of a variable (write `$$` if you really need one).

### 3.4 Variables the server receives

You do not set these in `.env`. `docker-compose.prod.yml` builds them for the server container:

| Variable                               | Value                                                                                                            |
| -------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`               | `Production`                                                                                                     |
| `ASPNETCORE_HTTP_PORTS`                | `8080`                                                                                                           |
| `ConnectionStrings__DefaultConnection` | `Server=db,1433;Database=${MSSQL_DB_NAME};User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;` |
| `JWT__Token`                           | `${JWT_KEY}`                                                                                                     |
| `SMTP__Username`                       | `${SMTP_USERNAME}` (the Purelymail address that sends the login codes)                                           |
| `SMTP__Password`                       | `${SMTP_PASSWORD}`                                                                                               |

Locally: `dotnet user-secrets set "SMTP:Username" "..."` and `dotnet user-secrets set "SMTP:Password" "..."` in `Server/`. Without them, Development logs the login code to the console instead of mailing it.

In code you read the connection string with `builder.Configuration.GetConnectionString("DefaultConnection")`.

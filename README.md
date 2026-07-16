# Leitor.Erp

## About this solution

This is a minimalist, non-layered startup solution with the ABP Framework. All the fundamental ABP modules are already installed. 

### Pre-requirements

* [.NET 10.0+ SDK](https://dotnet.microsoft.com/download/dotnet)
* Node is **not** required to build or run this project — `Leitor.Erp/wwwroot/libs` (the theme's client-side assets) is committed to git rather than regenerated via `abp install-libs`/Yarn at build time, so the Docker build has no npm-registry dependency at all. If you ever need to update client-side dependencies, install Node + the ABP CLI locally, run `abp install-libs` from the repo root, and commit the resulting changes under `Leitor.Erp/wwwroot/libs/`.

### Configurations

The solution comes with a default configuration that works out of the box for local development. However, you may consider changing the following before running your solution:

* Check the `ConnectionStrings` in `appsettings.json` files under the `Leitor.Erp` project and change it if you need.

### OpenIddict certificate

`openiddict.pfx` (the signing/encryption certificate ABP's OpenIddict integration needs outside the Development environment) is **not committed to git** — this repo is public, and the password would otherwise be readable by anyone. Instead:

* In `ASPNETCORE_ENVIRONMENT=Development` (the default for local Docker Compose via `docker-compose.override.yml`), ABP auto-generates an ephemeral dev certificate — nothing to configure.
* Outside Development (Coolify/production), the app requires two secret environment variables:
  * `OPENIDDICT_CERT_BASE64` — the `.pfx` file, base64-encoded. `entrypoint.sh` decodes this into `openiddict.pfx` at container startup.
  * `OPENIDDICT_CERT_PASSPHRASE` — the certificate's password (mapped to the `OpenIddict:CertificatePassPhrase` config key inside the container).

  To generate your own certificate and values:
  ```bash
  openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days 3650 -nodes -subj "/CN=Leitor.Erp OpenIddict"
  openssl pkcs12 -export -out openiddict.pfx -inkey key.pem -in cert.pem -passout pass:<your-password>
  base64 -w0 openiddict.pfx   # paste this output as OPENIDDICT_CERT_BASE64 in Coolify
  ```
  Set both as **secret** environment variables on the `app` service in Coolify (never commit them). Rotate by generating a new certificate and updating the two Coolify secrets — no code or image change needed.

### How to Run

Everything the app needs (client-side libraries, EF Core migrations) is baked into the Docker image at build time and applied automatically on container start (`entrypoint.sh` runs `--migrate-database` before launching the app every time — safe to repeat, it's a no-op once migrations are applied). No manual post-deploy step is required.

#### Option A: Docker Compose, local machine

Prerequisite: [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.

```bash
docker compose up --build
```

`docker-compose.override.yml` (auto-loaded locally, not used by Coolify) publishes the app on http://localhost:8080 and Postgres on `localhost:5432`. Default seeded admin login: `admin` / `1q2w3E*` — **change this before any real deployment.**

#### Option B: Run locally against Dockerized Postgres, outside a container

With Postgres running via `docker compose up -d postgres`:

```bash
cd Leitor.Erp
dotnet run --migrate-database   # first time only: creates and seeds the database
dotnet run                      # subsequent runs
```

#### Option C: Deploy to Coolify

1. In Coolify, add this repository as a **Docker Compose** resource pointing at `docker-compose.yml` (Coolify does not read `docker-compose.override.yml`, so host ports stay unpublished — that's expected).
2. Coolify auto-detects the `SERVICE_PASSWORD_POSTGRES` and `SERVICE_URL_APP` / `SERVICE_FQDN_APP` tokens referenced in `docker-compose.yml` and generates values for them — no manual secrets setup needed.
3. Set `OPENIDDICT_CERT_BASE64` and `OPENIDDICT_CERT_PASSPHRASE` as **secret** environment variables on the `app` service — see "OpenIddict certificate" above. Required outside Development; the app fails fast at startup if missing.
4. Assign a domain to the `app` service in the Coolify UI (Coolify's proxy routes to it directly; that's why `app` doesn't publish a host port).
5. Deploy. Postgres starts, the app image builds (`dotnet restore`/`publish` only — no Node/npm needed), and the container writes the certificate, migrates the database, and starts automatically.

### Deploying the application

Deploying an ABP application is not different than deploying any .NET or ASP.NET Core application. However, there are some topics that you should care about when you are deploying your applications. You can check ABP's [Deployment documentation](https://docs.abp.io/en/abp/latest/Deployment/Index) before deploying your application.

### Additional resources

You can see the following resources to learn more about your solution and the ABP Framework:

* [Application (Single Layer) Startup Template](https://docs.abp.io/en/abp/latest/Startup-Templates/Application-Single-Layer)
* [LeptonX Lite MVC UI](https://docs.abp.io/en/abp/latest/Themes/LeptonXLite/AspNetCore)

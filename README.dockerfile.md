# Dockerfile for Capstone .NET 8 API

This repository includes a multi-stage Dockerfile named `.dockerfile` that builds and publishes the `CapstoneProject_BE` ASP.NET Core application and produces a small runtime image.

How to build and run

- Build the image (from repository root):

```
docker build -f .dockerfile -t capstone_be:latest .
```

- Run the container, exposing port 8080:

```
docker run -p 8080:8080 capstone_be:latest
```

What the Dockerfile does (step-by-step)

- `FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build`
  - Uses the .NET SDK image for restoring and publishing the project.

- `WORKDIR /src`
  - Sets a working directory inside the build stage.

- `COPY ["capstone_be.sln", "."]` and `COPY [...]` for each `.csproj`
  - Copies the solution and individual project files first. This enables Docker layer caching so `dotnet restore` is only rerun when project files or dependencies change.

- `RUN dotnet restore`
  - Restores NuGet packages for the solution.

- `COPY . .`
  - Copies the remainder of the source tree into the container.

- `WORKDIR /src/CapstoneProject_BE` then `RUN dotnet publish -c Release -o /app/publish`
  - Publishes the web app in Release configuration to `/app/publish` (optimized output, ready for runtime).

- `FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final`
  - Uses the small runtime image for the final stage to keep the image surface smaller and more secure.

- `WORKDIR /app` and `COPY --from=build /app/publish .`
  - Copies the published output from the build stage into the final image.

- `ENV ASPNETCORE_URLS=http://+:8080` and `EXPOSE 8080`
  - Configures ASP.NET Core to listen on port 8080 inside the container and documents the exposed port.

- `ENTRYPOINT ["dotnet", "capstone_be.dll"]`
  - Starts the application. The entry DLL name matches the `capstone_be` project (default assembly name derived from the csproj file).


Healthcheck
- `HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 CMD curl -f http://localhost:8080/health || exit 1`
  - Container health is probed by calling `/health`. If `curl` is not present in the runtime image you can install it in a custom image, or handle health from the orchestrator (recommended).

Notes about fixes made

- The original Dockerfile used the `aspnet` runtime image for the build stage; that fails because `dotnet restore` and `dotnet publish` require the SDK. The build stage was changed to `mcr.microsoft.com/dotnet/sdk:8.0`.

- The original Dockerfile used the `aspnet` runtime image for the build stage; that fails because `dotnet restore` and `dotnet publish` require the SDK. The build stage was changed to `mcr.microsoft.com/dotnet/sdk:8.0`.
- The original Dockerfile copied `CapstoneProject_BE/CapstoneProject_BE.csproj`, but the actual project file is `CapstoneProject_BE/capstone_be.csproj`. The `COPY` line and the final `ENTRYPOINT` were updated accordingly.

Optional improvements

- Add a `HEALTHCHECK` to the final image to let orchestrators detect app health.
- Pin specific SDK/runtime patch versions if reproducible builds are required (e.g., `8.0.8` instead of `8.0`).
- Add labels with image metadata (maintainer, version).

If you'd like, I can add a `docker-compose.yml` for local development, a `healthcheck`, or update CI build steps to build/push this image.

Docker Compose

- A sample `docker-compose.yml` is included at the repository root. It builds the image from the `Dockerfile`, maps host port `8080` to container port `8080`, and passes common JWT env vars as placeholders.

- Usage (from repository root):

```powershell
docker compose up --build
```

- To run in background:

```powershell
docker compose up -d --build
```

- Override environment variables by creating a `.env` file with `JWT_KEY`, `JWT_ISSUER`, and `JWT_AUDIENCE`, or set them in your shell before running compose.

- If you need HTTPS inside the container, mount a `.pfx` into `/https/aspnetapp.pfx` and set `ASPNETCORE_Kestrel__Certificates__Default__Password` (do not bake secrets in the image).

- To enable Swagger UI while running in Docker/Compose, set the environment variable `ENABLE_SWAGGER=true` or add `EnableSwagger: true` to your configuration. Example:

```powershell
docker run -p 8080:8080 -e ENABLE_SWAGGER=true \
  -e "Jwt__Key=YourSuperSecretKeyForJWTTokenGenerationThatIsAtLeast32CharactersLong" \
  -e "Jwt__Issuer=FCTMSBackend" \
  -e "Jwt__Audience=FCTMSFrontend" \
  capstone_be:latest
```

Or add `ENABLE_SWAGGER=true` into your `.env` file used by `docker compose`.


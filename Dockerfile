FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Leitor.Erp/Leitor.Erp.csproj Leitor.Erp/
RUN dotnet restore Leitor.Erp/Leitor.Erp.csproj

COPY Leitor.Erp/ Leitor.Erp/

# wwwroot/libs is gitignored by ABP convention (regenerated via `abp install-libs`,
# which runs Yarn under the hood) - restore it here so the image is self-contained
# and doesn't depend on any pre-built assets from the host/git.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && corepack disable \
    && npm install -g yarn@1.22.22 \
    && yarn config set registry https://registry.npmjs.org \
    && rm -rf /var/lib/apt/lists/* \
    && node --version && yarn --version

RUN dotnet tool install --global Volo.Abp.Cli --version 10.5.0
ENV PATH="$PATH:/root/.dotnet/tools"
RUN abp install-libs

RUN dotnet publish Leitor.Erp/Leitor.Erp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["./entrypoint.sh"]

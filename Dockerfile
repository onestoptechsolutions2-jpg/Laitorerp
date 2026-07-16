FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Leitor.Erp/Leitor.Erp.csproj Leitor.Erp/
RUN dotnet restore Leitor.Erp/Leitor.Erp.csproj

COPY Leitor.Erp/ Leitor.Erp/

# wwwroot/libs is committed to git (not regenerated via `abp install-libs`/Yarn here)
# so this build has no dependency on Node/npm-registry access - see .gitignore.
RUN dotnet publish Leitor.Erp/Leitor.Erp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["./entrypoint.sh"]

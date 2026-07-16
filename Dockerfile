FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Leitor.Erp/Leitor.Erp.csproj Leitor.Erp/
RUN dotnet restore Leitor.Erp/Leitor.Erp.csproj

COPY Leitor.Erp/ Leitor.Erp/
RUN dotnet publish Leitor.Erp/Leitor.Erp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Leitor.Erp.dll"]

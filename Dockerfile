FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["ProjectB.csproj", "./"]
RUN dotnet restore "./ProjectB.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ProjectB.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProjectB.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
# RUN cp certs/customer.cert /usr/local/share/ca-certificates/customer.crt && update-ca-certificates
COPY --from=publish /app/publish .
COPY . .
ENTRYPOINT ["dotnet","ProjectB.dll"]

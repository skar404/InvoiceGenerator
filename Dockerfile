FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["InvoiceGenerator.csproj", "./"]
RUN dotnet restore "InvoiceGenerator.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "InvoiceGenerator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InvoiceGenerator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libx11-6 \
    libxext6 \
    libxrender1

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InvoiceGenerator.dll"]

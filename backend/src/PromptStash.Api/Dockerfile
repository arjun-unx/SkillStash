FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["backend/Directory.Build.props", "backend/Directory.Build.props"]
COPY ["backend/src/PromptStash.Api/PromptStash.Api.csproj", "backend/src/PromptStash.Api/"]
RUN dotnet restore "backend/src/PromptStash.Api/PromptStash.Api.csproj"
COPY ["backend/", "backend/"]
WORKDIR /src/backend/src/PromptStash.Api
RUN dotnet publish "PromptStash.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PromptStash.Api.dll"]

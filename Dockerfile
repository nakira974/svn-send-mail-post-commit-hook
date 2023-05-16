FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["post-commit/post-commit.csproj", "post-commit/"]
RUN dotnet restore "post-commit/post-commit.csproj"
COPY . .
WORKDIR "/src/post-commit"
RUN dotnet build "post-commit.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "post-commit.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "post-commit.dll"]

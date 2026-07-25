FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Hackathon.Api/Hackathon.Api.csproj", "Hackathon.Api/"]
COPY ["Hackathon.Application/Hackathon.Application.csproj", "Hackathon.Application/"]
COPY ["Hackathon.Domain/Hackathon.Domain.csproj", "Hackathon.Domain/"]
COPY ["Hackathon.Infrastructure/Hackathon.Infrastructure.csproj", "Hackathon.Infrastructure/"]

RUN dotnet restore "Hackathon.Api/Hackathon.Api.csproj"

COPY . .

WORKDIR "/src/Hackathon.Api"

RUN dotnet publish "Hackathon.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Hackathon.Api.dll"]
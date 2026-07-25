FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Hackathon.DonationWorker/Hackathon.DonationWorker.csproj", "Hackathon.DonationWorker/"]
COPY ["Hackathon.Application/Hackathon.Application.csproj", "Hackathon.Application/"]
COPY ["Hackathon.Domain/Hackathon.Domain.csproj", "Hackathon.Domain/"]
COPY ["Hackathon.Infrastructure/Hackathon.Infrastructure.csproj", "Hackathon.Infrastructure/"]

RUN dotnet restore "Hackathon.DonationWorker/Hackathon.DonationWorker.csproj"

COPY . .

WORKDIR "/src/Hackathon.DonationWorker"

RUN dotnet publish "Hackathon.DonationWorker.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore


FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Hackathon.DonationWorker.dll"]
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Interpolator.Host/Interpolator.Host.csproj"
RUN dotnet publish "Interpolator.Host/Interpolator.Host.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS run
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "Interpolator.Host.dll"]
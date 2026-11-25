FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RailwayTickets/RailwayTickets.csproj RailwayTickets/
RUN dotnet restore RailwayTickets/RailwayTickets.csproj
COPY RailwayTickets/ RailwayTickets/
RUN dotnet publish RailwayTickets/RailwayTickets.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production Database__Mode=Demo
USER app
EXPOSE 10000
CMD ["sh","-c","exec dotnet RailwayTickets.dll --urls http://0.0.0.0:${PORT:-10000}"]

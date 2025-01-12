FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /BadukServer

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -o out

# Build runtime image
FROM build AS final
WORKDIR /BadukServer
COPY --from=build /BadukServer/out .
ENTRYPOINT ["dotnet", "BadukServer.dll"]
# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy source from the repo checkout (no git clone needed)
COPY . .

# If your solution has multiple projects, set the csproj explicitly:
# docker build --build-arg PROJECT_PATH=src/MDR_Harvester/MDR_Harvester.csproj .
ARG PROJECT_PATH=MDR_Harvester.csproj

# Restore + publish
RUN dotnet restore "${PROJECT_PATH}"
RUN dotnet publish "${PROJECT_PATH}" -c Release -o /app/out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ARG PUID=1000
ARG PGID=1000

# Create user/group matching host IDs
RUN groupadd -g "${PGID}" mdr \
 && useradd  -u "${PUID}" -g "${PGID}" -m -s /usr/sbin/nologin mdr

# App binaries
COPY --from=build /app/out ./

# Create data dirs (use volumes for real data)
RUN mkdir -p /app/MDR_Data /app/MDR_Sources /app/MDR_Logs /app/test \
    /app/biolincc /app/ctg /app/euctr /app/isrctn /app/pubmed /app/who /app/yoda \
 && chown -R "${PUID}:${PGID}" /app

USER mdr

ENTRYPOINT ["dotnet", "MDR_Harvester.dll"]

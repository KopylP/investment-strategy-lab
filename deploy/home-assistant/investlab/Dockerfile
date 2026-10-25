ARG BUILD_FROM=mcr.microsoft.com/dotnet/aspnet:10.0-noble
FROM ${BUILD_FROM}

ENV DEBIAN_FRONTEND=noninteractive LANG=C.UTF-8 \
    DOTNET_gcServer=0 DOTNET_GCConserveMemory=5 ASPNETCORE_ENVIRONMENT=Production

RUN apt-get update \
 && apt-get install -y --no-install-recommends postgresql python3 python3-venv openssl ca-certificates tzdata jq \
 && rm -rf /var/lib/apt/lists/*

# The strategy engine first: its dependencies change less often than the app, so this layer is reused.
COPY rootfs/app/engine /app/engine
RUN python3 -m venv /opt/engine && /opt/engine/bin/pip install --no-cache-dir /app/engine

COPY rootfs/ /
RUN chmod +x /run.sh

CMD ["/run.sh"]

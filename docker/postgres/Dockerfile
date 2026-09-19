# Postgres com schema/seed do Users (roda em /docker-entrypoint-initdb.d
# sempre que o data dir estiver vazio — típico no Container Apps sem volume).
FROM postgres:16-alpine
COPY init/ /docker-entrypoint-initdb.d/

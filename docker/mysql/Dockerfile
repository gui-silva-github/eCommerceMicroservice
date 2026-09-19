# MySQL com schema/seed do Products (roda em /docker-entrypoint-initdb.d
# sempre que o data dir estiver vazio — típico no Container Apps sem volume).
FROM mysql:8.0
COPY init/ /docker-entrypoint-initdb.d/

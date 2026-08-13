# Database Tools

Scripts de mantenimiento y administración de la base de datos.

## Scripts disponibles

### `generate-password-hash.py`

Genera un hash de contraseña compatible con ASP.NET Core Identity v3 (PBKDF2-HMACSHA256).
Úsalo junto con `create-superadmin.sql` para crear/resetear un usuario admin directamente en BD.

```bash
python3 tools/db/generate-password-hash.py "MiContraseña@123"
```

### `create-superadmin.sql`

Crea o resetea un usuario SuperAdmin con todos los claims. Seguro de ejecutar más de una vez
(upsert). Edita `@username`/`@email`/`@fullName`/`@passwordHash` antes de correrlo. Termina con
`COMMIT` — revisa el post-check antes si prefieres hacer `ROLLBACK` en su lugar.

```bash
docker exec -i <container> mysql -uroot -p<password> App < tools/db/create-superadmin.sql
```

---

## Respaldo de base de datos de producción

Producción usa un MySQL **externo**, administrado por el DBA (no hay contenedor `db` local
en el perfil `production` — ver `docker-compose.yml`). El respaldo se hace con `mysqldump`
directo contra ese servidor, con las credenciales de aplicación que te dé el DBA (no root;
pídele al DBA un usuario con privilegios de respaldo si `mysqldump` requiere más permisos
que el usuario de la app).

### 1. Obtener los datos de conexión

Están en `DATABASE_CONNECTION_STRING` dentro de `.env.production` (host, puerto, base, usuario).

### 2. Generar el dump

```bash
mysqldump -h <host> -P <puerto> -u <usuario> -p<password> <base_de_datos> \
  > backup_$(date +%Y%m%d).sql
```

### 3. Restaurar en entorno local (para pruebas)

```bash
mysql -h 127.0.0.1 -P 3306 -u root -p<MYSQL_ROOT_PASSWORD_LOCAL> <base_de_datos> \
  < backup_<fecha>.sql
```

### 4. Respaldo automático (cron, desde donde tengas acceso de red al servidor externo)

```bash
0 3 * * * mysqldump -h <host> -P <puerto> -u <usuario> -p<password> <base_de_datos> \
  | gzip > /backups/backup_$(date +\%Y\%m\%d).sql.gz
```

> Si en cambio estás en un despliegue **self-hosted** white-label (perfiles `shared-db`/`tenant`,
> ver `docs/02-Development/white-label-deployment-guide.md`), el respaldo sí se hace vía
> `docker exec <CONTAINER_PREFIX>-mysql mysqldump -u root -p<MYSQL_ROOT_PASSWORD> <base>`,
> igual que en desarrollo — ese caso sí tiene el contenedor `db` local.

---

## Obtener credenciales Docker

Las credenciales están en el archivo de entorno del proyecto:

```bash
# Desarrollo (contenedor db local)
cat .env.development | grep -E "MYSQL_USER|MYSQL_PASSWORD|MYSQL_DATABASE|CONTAINER_PREFIX"

# Producción (BD externa administrada por el DBA)
cat .env.production | grep "DATABASE_CONNECTION_STRING"
```

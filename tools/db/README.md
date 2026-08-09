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

Si MySQL en producción no expone puerto al host (solo accesible dentro de la red interna de
Docker), el respaldo se hace con `mysqldump` ejecutado dentro del contenedor vía `docker exec`,
opcionalmente sobre un [contexto Docker remoto](../../README.md#servidor-con-csf-firewall-despliegue-en-vps) si despliegas por SSH.

### 1. Verificar nombre del contenedor

```bash
docker ps --format "table {{.Names}}\t{{.Status}}"
```

El nombre sigue el patrón `<CONTAINER_PREFIX>-mysql` (valor en `.env.production`).

### 2. Generar el dump localmente

```bash
docker exec <CONTAINER_PREFIX>-mysql \
  mysqldump -u root -p<MYSQL_ROOT_PASSWORD> <MYSQL_DATABASE> \
  > backup_$(date +%Y%m%d).sql
```

### 3. Restaurar en entorno local (para pruebas)

```bash
docker exec -i <CONTAINER_PREFIX>-mysql \
  mysql -u root -p<MYSQL_ROOT_PASSWORD> <MYSQL_DATABASE> \
  < backup_<fecha>.sql
```

### 4. Respaldo automático en el servidor (cron)

```bash
0 3 * * * docker exec <CONTAINER_PREFIX>-mysql \
  mysqldump -u root -p<MYSQL_ROOT_PASSWORD> <MYSQL_DATABASE> \
  | gzip > /backups/backup_$(date +\%Y\%m\%d).sql.gz
```

---

## Obtener credenciales Docker

Las credenciales están en el archivo de entorno del proyecto:

```bash
# Desarrollo
cat .env.development | grep -E "MYSQL_USER|MYSQL_PASSWORD|MYSQL_DATABASE|CONTAINER_PREFIX"

# Producción
cat .env.production | grep -E "MYSQL_ROOT_PASSWORD|MYSQL_DATABASE|CONTAINER_PREFIX"
```

-- Usuario de MariaDB que usa el programa (no use root en las PC del liceo).
-- Ejecutar una sola vez en el servidor, como root.
-- Cambie la clave y pongala igual en el archivo conexion.config.

CREATE USER IF NOT EXISTS 'usuario_secretaria'@'localhost' IDENTIFIED BY 'CAMBIE_ESTA_CLAVE';
CREATE USER IF NOT EXISTS 'usuario_secretaria'@'%'         IDENTIFIED BY 'CAMBIE_ESTA_CLAVE';

GRANT SELECT, INSERT, UPDATE, DELETE ON db_carabobo.* TO 'usuario_secretaria'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE ON db_carabobo.* TO 'usuario_secretaria'@'%';

FLUSH PRIVILEGES;

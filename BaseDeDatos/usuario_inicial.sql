-- Usuario inicial para poder entrar al sistema por primera vez.
-- Clave: admin123  (SHA-256, el mismo cifrado que usa CapaNegocio/Seguridad.cs)
-- Cambie la clave despues del primer ingreso.

USE db_carabobo;

INSERT INTO USUARIO (nombre, pass, rol, ESTADO)
VALUES ('admin',
        '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9',
        'Administrador',
        'Activo')
ON DUPLICATE KEY UPDATE pass = VALUES(pass), rol = VALUES(rol), ESTADO = 'Activo';

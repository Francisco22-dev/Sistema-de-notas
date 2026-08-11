# Sistema de Notas — conexion con la base de datos `db_carabobo`

## 1. Preparar la base de datos (una sola vez, en la PC servidor)

```bat
mysql -u root -p < db_carabobo.sql
mysql -u root -p < BaseDeDatos\usuario_conexion.sql
mysql -u root -p < BaseDeDatos\usuario_inicial.sql
```

Con eso queda creada la base, el usuario de conexion `usuario_secretaria` y el
usuario del programa **admin / admin123** (cambielo despues de entrar).

## 2. Configurar la conexion

Los datos de conexion ya **no** estan dentro del codigo: se leen del archivo
`conexion.config` que queda junto al `.exe` (`bin\Debug\net8.0-windows\`).

```
Server=127.0.0.1      # en las demas PC: la IP fija del servidor, ej. 192.168.1.50
Port=3306
Database=db_carabobo
Uid=usuario_secretaria
Pwd=CAMBIE_ESTA_CLAVE
```

Al abrir el programa se prueba la conexion y, si falla, se muestra el motivo en
castellano (servidor apagado, clave incorrecta, base inexistente, etc.).

## 3. Que cambio en el codigo

| Capa | Clase | Para que sirve |
|---|---|---|
| Entidades | `Persona`, `Direccion`, `Antropometricos`, `Salud`, `ExtraCurricular`, `Representante`, `Estudiante`, `Inscripcion`, `Usuario`, `Catalogo`, `GradoSeccion` | Una clase por tabla real de `db_carabobo` |
| CapaDatos | `ConexionBD` | Conexion configurable + prueba de red + traduccion de errores |
| CapaDatos | `CatalogoDatos` | Paises, estados, ciudades, municipios, parroquias, grados, secciones, materias, periodos |
| CapaDatos | `PersonaDatos` | Inserta `PERSONA` y `DIRECCION`, busca por cedula |
| CapaDatos | `RepresentanteDatos` | `PERSONA_REPRESENTANTE` |
| CapaDatos | `EstudianteDatos` | Inscripcion completa en una sola transaccion |
| CapaDatos | `UsuarioDatos` | Login contra `USUARIO` (`nombre`, `pass`, `rol`, `ESTADO`) |
| CapaNegocio | `InscripcionNegocio`, `UsuarioNegocio`, `Seguridad`, `SesionActual` | Validaciones, cifrado SHA-256 y sesion |
| Presentacion | `EstudianteForm`, `RepresentanteForm`, `EstudiantesControl`, `MainWindow` | Formularios con los campos que pide la base |

La inscripcion se guarda en una transaccion: `DIRECCION` -> `PERSONA` (representante)
-> `PERSONA_REPRESENTANTE` -> `PERSONA` (estudiante) -> `ANTROPOMETRICOS` -> `SALUD`
-> `EXTRA_CURRICULAR` -> `PERSONA_ESTUDIANTE` -> `INSCRIPCION`. Si algo falla, no
queda nada a medias.

## 4. Detalle importante de la base de datos

En `PERSONA_ESTUDIANTE` la columna `parroquia_nacimiento_id` es `NOT NULL`, pero la
restriccion `check_lugar_nacimiento` exige que sea `NULL` cuando el pais de nacimiento
no es Venezuela. Con el esquema actual **es imposible inscribir a un estudiante nacido
en el extranjero**. Si necesita hacerlo:

```sql
ALTER TABLE PERSONA_ESTUDIANTE MODIFY parroquia_nacimiento_id INT NULL;
```

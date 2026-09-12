using System;
using System.Data;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    public class EstudianteDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        // Propiedades auxiliares para rellenar los combos en cascada al editar
        public int EstadoNacimientoIdTemp { get; set; }
        public int MunicipioNacimientoIdTemp { get; set; }
        public int EstadoDireccionIdTemp { get; set; }
        public int EstadoDireccionRepIdTemp { get; set; }
        public Inscripcion? UltimaInscripcionTemp { get; set; }
        public Representante? RepresentanteCargadoTemp { get; set; }

        public int RegistrarInscripcionCompleta(Representante representante, Estudiante estudiante, Inscripcion inscripcion)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlTransaction transaccion = conexion.BeginTransaction())
            {
                try
                {
                    if (representante.Id == 0)
                        RepresentanteDatos.Insertar(representante, conexion, transaccion);

                    estudiante.RepresentantePrincipalId = representante.Id;

                    if (estudiante.PersonaId == 0)
                        estudiante.PersonaId = PersonaDatos.InsertarPersona(estudiante.Persona, conexion, transaccion);

                    estudiante.AntropometricoId = InsertarAntropometricos(estudiante.Antropometricos, conexion, transaccion);
                    estudiante.SaludId = InsertarSalud(estudiante.Salud, conexion, transaccion);
                    estudiante.ExtraCurricularId = InsertarExtraCurricular(estudiante.ExtraCurricular, conexion, transaccion);

                    estudiante.Id = InsertarEstudiante(estudiante, conexion, transaccion);

                    inscripcion.EstudianteId = estudiante.Id;
                    inscripcion.Id = InsertarInscripcion(inscripcion, conexion, transaccion);

                    transaccion.Commit();
                    return estudiante.Id;
                }
                catch (MySqlException ex)
                {
                    transaccion.Rollback();
                    throw new Exception(ConexionBD.TraducirError(ex), ex);
                }
                catch
                {
                    transaccion.Rollback();
                    throw;
                }
            }
        }

        public void ActualizarInscripcionCompleta(Representante representante, Estudiante estudiante, Inscripcion? inscripcion)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlTransaction transaccion = conexion.BeginTransaction())
            {
                try
                {
                    // 1. Actualizar o Registrar Representante
                    if (representante.Id == 0)
                        RepresentanteDatos.Insertar(representante, conexion, transaccion);
                    else
                        RepresentanteDatos.Actualizar(representante, conexion, transaccion);

                    estudiante.RepresentantePrincipalId = representante.Id;

                    // 2. Actualizar Persona del Estudiante y su Dirección
                    PersonaDatos.ActualizarPersona(estudiante.Persona, conexion, transaccion);

                    // 3. Actualizar Tablas de Apoyo
                    ActualizarAntropometricos(estudiante.Antropometricos, conexion, transaccion);
                    ActualizarSalud(estudiante.Salud, conexion, transaccion);
                    ActualizarExtraCurricular(estudiante.ExtraCurricular, conexion, transaccion);

                    // 4. Actualizar Ficha del Estudiante
                    ActualizarEstudiante(estudiante, conexion, transaccion);

                    // 5. Actualizar Matrícula
                    if (inscripcion != null)
                    {
                        if (inscripcion.Id > 0)
                            ActualizarInscripcion(inscripcion, conexion, transaccion);
                        else
                        {
                            inscripcion.EstudianteId = estudiante.Id;
                            InsertarInscripcion(inscripcion, conexion, transaccion);
                        }
                    }

                    transaccion.Commit();
                }
                catch (MySqlException ex)
                {
                    transaccion.Rollback();
                    throw new Exception(ConexionBD.TraducirError(ex), ex);
                }
                catch
                {
                    transaccion.Rollback();
                    throw;
                }
            }
        }

        public Estudiante? ObtenerPorId(int estudianteId)
        {
            const string consulta = @"
                SELECT e.id AS e_id, e.cedula_escolar, e.numero_hijo, e.lateralidad, e.telefono_estudiante, e.correo_estudiante,
                       e.persona_id, e.pais_nacimiento_id, e.parroquia_nacimiento_id, e.antropometrico_id, e.salud_id,
                       e.extra_curricular_id, e.representante_principal_id, e.representante_secundario_id,
                       e.situacion_padres, e.convive_con, e.padre_cedula, e.padre_nombres_apellidos, e.padre_telefono, e.padre_vive,
                       e.madre_cedula, e.madre_nombres_apellidos, e.madre_telefono, e.madre_vive,
                       e.representante_legal_tipo, e.oficio_cpnna_tribunal, e.observaciones_custodia, e.ESTADO,
                       -- PERSONA ESTUDIANTE
                       p.id AS p_id, p.nacionalidad, p.cedula_identidad, p.nombre_1, p.nombre_2,
                       p.apellido_1, p.apellido_2, p.fecha_nacimiento, p.sexo, p.direccion_id,
                       -- DIRECCION ESTUDIANTE
                       d.id AS d_id, d.ciudad_id, d.sector, d.avenida, d.calle, d.manzana, d.vereda,
                       d.numero_vivienda, d.tipo_vivienda, d.condicion_vivienda, d.infraestructura_vivienda,
                       c.estado_id AS dir_estado_id,
                       -- LUGAR DE NACIMIENTO
                       m.id AS nac_municipio_id, es.id AS nac_estado_id,
                       -- ANTROPOMETRICOS
                       ant.estatura, ant.peso, ant.talla_camisa, ant.talla_pantalon, ant.talla_zapato,
                       -- SALUD
                       s.reacciones_alergicas, s.cuales_alergias, s.enfermedades_padecidas, s.atencion_especial,
                       s.horario_tratamiento, s.atendido_por_especialista, s.nombre_especialista,
                       s.fecha_inicio_especialista, s.condicion_atencion,
                       -- EXTRA CURRICULAR
                       ex.realiza_deportes, ex.cuales_deportes, ex.posee_canaima, ex.fecha_asignacion_canaima,
                       ex.serial_canaima, ex.estado_canaima, ex.falla_canaima, ex.posee_cargador,
                       ex.estado_cargador, ex.falla_cargador,
                       -- REPRESENTANTE PRINCIPAL
                       r.id AS r_id, r.parentesco, r.estado_civil, r.ingreso_mensual, r.telefono_movil,
                       r.telefono_habitacion, r.correo_electronico, r.profesion, r.empresa_trabajo,
                       r.telefono_empresa, r.direccion_empresa, r.persona_id AS r_persona_id, r.ESTADO AS r_estado,
                       prep.id AS prep_id, prep.nacionalidad AS prep_nac, prep.cedula_identidad AS prep_cedula,
                       prep.nombre_1 AS prep_nom1, prep.nombre_2 AS prep_nom2,
                       prep.apellido_1 AS prep_ape1, prep.apellido_2 AS prep_ape2,
                       prep.fecha_nacimiento AS prep_fnac, prep.sexo AS prep_sexo, prep.direccion_id AS prep_dir_id,
                       drep.id AS drep_id, drep.ciudad_id AS drep_ciudad_id, drep.sector AS drep_sector,
                       drep.avenida AS drep_avenida, drep.calle AS drep_calle, drep.manzana AS drep_manzana,
                       drep.vereda AS drep_vereda, drep.numero_vivienda AS drep_numero, drep.tipo_vivienda AS drep_tipo,
                       drep.condicion_vivienda AS drep_condicion, drep.infraestructura_vivienda AS drep_infra,
                       crep.estado_id AS rep_dir_estado_id,
                       -- ULTIMA INSCRIPCION
                       i.id AS inscripcion_id, i.periodo_id, i.tipo_ingreso, i.colegio_procedencia, i.nivel_academico,
                       gs.grado_id, gs.seccion_id
                FROM PERSONA_ESTUDIANTE e
                INNER JOIN PERSONA p ON p.id = e.persona_id
                LEFT JOIN DIRECCION d ON d.id = p.direccion_id
                LEFT JOIN CIUDAD c ON c.id = d.ciudad_id
                LEFT JOIN PARROQUIA par ON par.id = e.parroquia_nacimiento_id
                LEFT JOIN MUNICIPIO m ON m.id = par.municipio_id
                LEFT JOIN ESTADO es ON es.id = m.estado_id
                LEFT JOIN ANTROPOMETRICOS ant ON ant.id = e.antropometrico_id
                LEFT JOIN SALUD s ON s.id = e.salud_id
                LEFT JOIN EXTRA_CURRICULAR ex ON ex.id = e.extra_curricular_id
                LEFT JOIN PERSONA_REPRESENTANTE r ON r.id = e.representante_principal_id
                LEFT JOIN PERSONA prep ON prep.id = r.persona_id
                LEFT JOIN DIRECCION drep ON drep.id = prep.direccion_id
                LEFT JOIN CIUDAD crep ON crep.id = drep.ciudad_id
                LEFT JOIN INSCRIPCION i ON i.id = (SELECT MAX(id) FROM INSCRIPCION WHERE estudiante_id = e.id)
                LEFT JOIN GRADO_SECCION gs ON gs.id = i.grado_seccion_id
                WHERE e.id = @id LIMIT 1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@id", estudianteId);
                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read()) return null;

                    Estudiante est = new Estudiante
                    {
                        Id = lector.GetInt32("e_id"),
                        CedulaEscolar = lector.GetString("cedula_escolar"),
                        NumeroHijo = lector.GetInt32("numero_hijo"),
                        Lateralidad = lector.GetString("lateralidad"),
                        TelefonoEstudiante = lector.IsDBNull(lector.GetOrdinal("telefono_estudiante")) ? null : lector.GetString("telefono_estudiante"),
                        CorreoEstudiante = lector.IsDBNull(lector.GetOrdinal("correo_estudiante")) ? null : lector.GetString("correo_estudiante"),
                        PersonaId = lector.GetInt32("persona_id"),
                        PaisNacimientoId = lector.GetInt32("pais_nacimiento_id"),
                        ParroquiaNacimientoId = lector.IsDBNull(lector.GetOrdinal("parroquia_nacimiento_id")) ? null : lector.GetInt32("parroquia_nacimiento_id"),
                        AntropometricoId = lector.GetInt32("antropometrico_id"),
                        SaludId = lector.GetInt32("salud_id"),
                        ExtraCurricularId = lector.GetInt32("extra_curricular_id"),
                        RepresentantePrincipalId = lector.GetInt32("representante_principal_id"),
                        RepresentanteSecundarioId = lector.IsDBNull(lector.GetOrdinal("representante_secundario_id")) ? null : lector.GetInt32("representante_secundario_id"),

                        SituacionPadres = lector.IsDBNull(lector.GetOrdinal("situacion_padres")) ? "Viven Juntos" : lector.GetString("situacion_padres"),
                        ConviveCon = lector.IsDBNull(lector.GetOrdinal("convive_con")) ? "Ambos Padres" : lector.GetString("convive_con"),
                        PadreCedula = lector.IsDBNull(lector.GetOrdinal("padre_cedula")) ? null : lector.GetString("padre_cedula"),
                        PadreNombresApellidos = lector.IsDBNull(lector.GetOrdinal("padre_nombres_apellidos")) ? null : lector.GetString("padre_nombres_apellidos"),
                        PadreTelefono = lector.IsDBNull(lector.GetOrdinal("padre_telefono")) ? null : lector.GetString("padre_telefono"),
                        PadreVive = lector.IsDBNull(lector.GetOrdinal("padre_vive")) ? "Si" : lector.GetString("padre_vive"),
                        MadreCedula = lector.IsDBNull(lector.GetOrdinal("madre_cedula")) ? null : lector.GetString("madre_cedula"),
                        MadreNombresApellidos = lector.IsDBNull(lector.GetOrdinal("madre_nombres_apellidos")) ? null : lector.GetString("madre_nombres_apellidos"),
                        MadreTelefono = lector.IsDBNull(lector.GetOrdinal("madre_telefono")) ? null : lector.GetString("madre_telefono"),
                        MadreVive = lector.IsDBNull(lector.GetOrdinal("madre_vive")) ? "Si" : lector.GetString("madre_vive"),
                        RepresentanteLegalTipo = lector.IsDBNull(lector.GetOrdinal("representante_legal_tipo")) ? "Madre" : lector.GetString("representante_legal_tipo"),
                        OficioCpnnaTribunal = lector.IsDBNull(lector.GetOrdinal("oficio_cpnna_tribunal")) ? null : lector.GetString("oficio_cpnna_tribunal"),
                        ObservacionesCustodia = lector.IsDBNull(lector.GetOrdinal("observaciones_custodia")) ? null : lector.GetString("observaciones_custodia"),
                        Estado = lector.GetString("ESTADO"),

                        Persona = new Persona
                        {
                            Id = lector.GetInt32("p_id"),
                            Nacionalidad = lector.GetString("nacionalidad"),
                            CedulaIdentidad = lector.IsDBNull(lector.GetOrdinal("cedula_identidad")) ? null : lector.GetString("cedula_identidad"),
                            Nombre1 = lector.GetString("nombre_1"),
                            Nombre2 = lector.IsDBNull(lector.GetOrdinal("nombre_2")) ? null : lector.GetString("nombre_2"),
                            Apellido1 = lector.GetString("apellido_1"),
                            Apellido2 = lector.IsDBNull(lector.GetOrdinal("apellido_2")) ? null : lector.GetString("apellido_2"),
                            FechaNacimiento = lector.IsDBNull(lector.GetOrdinal("fecha_nacimiento")) ? null : lector.GetDateTime("fecha_nacimiento"),
                            Sexo = lector.GetString("sexo"),
                            DireccionId = lector.IsDBNull(lector.GetOrdinal("direccion_id")) ? null : lector.GetInt32("direccion_id")
                        }
                    };

                    // Dirección del Estudiante
                    if (!lector.IsDBNull(lector.GetOrdinal("d_id")))
                    {
                        est.Persona.Direccion = new Direccion
                        {
                            Id = lector.GetInt32("d_id"),
                            CiudadId = lector.GetInt32("ciudad_id"),
                            Sector = lector.IsDBNull(lector.GetOrdinal("sector")) ? null : lector.GetString("sector"),
                            Avenida = lector.IsDBNull(lector.GetOrdinal("avenida")) ? null : lector.GetString("avenida"),
                            Calle = lector.IsDBNull(lector.GetOrdinal("calle")) ? null : lector.GetString("calle"),
                            Manzana = lector.IsDBNull(lector.GetOrdinal("manzana")) ? null : lector.GetString("manzana"),
                            Vereda = lector.IsDBNull(lector.GetOrdinal("vereda")) ? null : lector.GetString("vereda"),
                            NumeroVivienda = lector.IsDBNull(lector.GetOrdinal("numero_vivienda")) ? null : lector.GetString("numero_vivienda"),
                            TipoVivienda = lector.GetString("tipo_vivienda"),
                            CondicionVivienda = lector.GetString("condicion_vivienda"),
                            InfraestructuraVivienda = lector.GetString("infraestructura_vivienda")
                        };
                    }

                    // Guardar temporales para cargar desplegables en cascada
                    EstadoNacimientoIdTemp = lector.IsDBNull(lector.GetOrdinal("nac_estado_id")) ? 0 : lector.GetInt32("nac_estado_id");
                    MunicipioNacimientoIdTemp = lector.IsDBNull(lector.GetOrdinal("nac_municipio_id")) ? 0 : lector.GetInt32("nac_municipio_id");
                    EstadoDireccionIdTemp = lector.IsDBNull(lector.GetOrdinal("dir_estado_id")) ? 0 : lector.GetInt32("dir_estado_id");

                    // Antropométricos, Salud, Extracurricular
                    est.Antropometricos = new Antropometricos
                    {
                        Id = est.AntropometricoId,
                        Estatura = lector.IsDBNull(lector.GetOrdinal("estatura")) ? null : lector.GetDecimal("estatura"),
                        Peso = lector.IsDBNull(lector.GetOrdinal("peso")) ? null : lector.GetDecimal("peso"),
                        TallaCamisa = lector.IsDBNull(lector.GetOrdinal("talla_camisa")) ? null : lector.GetString("talla_camisa"),
                        TallaPantalon = lector.IsDBNull(lector.GetOrdinal("talla_pantalon")) ? null : lector.GetString("talla_pantalon"),
                        TallaZapato = lector.IsDBNull(lector.GetOrdinal("talla_zapato")) ? null : lector.GetInt32("talla_zapato")
                    };

                    est.Salud = new Salud
                    {
                        Id = est.SaludId,
                        ReaccionesAlergicas = lector.GetString("reacciones_alergicas"),
                        CualesAlergias = lector.IsDBNull(lector.GetOrdinal("cuales_alergias")) ? null : lector.GetString("cuales_alergias"),
                        EnfermedadesPadecidas = lector.IsDBNull(lector.GetOrdinal("enfermedades_padecidas")) ? null : lector.GetString("enfermedades_padecidas"),
                        AtencionEspecial = lector.GetString("atencion_especial"),
                        HorarioTratamiento = lector.IsDBNull(lector.GetOrdinal("horario_tratamiento")) ? null : lector.GetString("horario_tratamiento"),
                        AtendidoPorEspecialista = lector.GetString("atendido_por_especialista"),
                        NombreEspecialista = lector.IsDBNull(lector.GetOrdinal("nombre_especialista")) ? null : lector.GetString("nombre_especialista"),
                        FechaInicioEspecialista = lector.IsDBNull(lector.GetOrdinal("fecha_inicio_especialista")) ? null : lector.GetDateTime("fecha_inicio_especialista"),
                        CondicionAtencion = lector.IsDBNull(lector.GetOrdinal("condicion_atencion")) ? null : lector.GetString("condicion_atencion")
                    };

                    est.ExtraCurricular = new ExtraCurricular
                    {
                        Id = est.ExtraCurricularId,
                        RealizaDeportes = lector.GetString("realiza_deportes"),
                        CualesDeportes = lector.IsDBNull(lector.GetOrdinal("cuales_deportes")) ? null : lector.GetString("cuales_deportes"),
                        PoseeCanaima = lector.GetString("posee_canaima"),
                        FechaAsignacionCanaima = lector.IsDBNull(lector.GetOrdinal("fecha_asignacion_canaima")) ? null : lector.GetDateTime("fecha_asignacion_canaima"),
                        SerialCanaima = lector.IsDBNull(lector.GetOrdinal("serial_canaima")) ? null : lector.GetString("serial_canaima"),
                        EstadoCanaima = lector.IsDBNull(lector.GetOrdinal("estado_canaima")) ? null : lector.GetString("estado_canaima"),
                        FallaCanaima = lector.IsDBNull(lector.GetOrdinal("falla_canaima")) ? null : lector.GetString("falla_canaima"),
                        PoseeCargador = lector.GetString("posee_cargador"),
                        EstadoCargador = lector.IsDBNull(lector.GetOrdinal("estado_cargador")) ? null : lector.GetString("estado_cargador"),
                        FallaCargador = lector.IsDBNull(lector.GetOrdinal("falla_cargador")) ? null : lector.GetString("falla_cargador")
                    };

                    // Representante Principal y su Dirección
                    if (!lector.IsDBNull(lector.GetOrdinal("r_id")))
                    {
                        Representante rep = new Representante
                        {
                            Id = lector.GetInt32("r_id"),
                            Parentesco = lector.GetString("parentesco"),
                            EstadoCivil = lector.GetString("estado_civil"),
                            IngresoMensual = lector.IsDBNull(lector.GetOrdinal("ingreso_mensual")) ? null : lector.GetDecimal("ingreso_mensual"),
                            TelefonoMovil = lector.IsDBNull(lector.GetOrdinal("telefono_movil")) ? null : lector.GetString("telefono_movil"),
                            TelefonoHabitacion = lector.IsDBNull(lector.GetOrdinal("telefono_habitacion")) ? null : lector.GetString("telefono_habitacion"),
                            CorreoElectronico = lector.IsDBNull(lector.GetOrdinal("correo_electronico")) ? null : lector.GetString("correo_electronico"),
                            Profesion = lector.IsDBNull(lector.GetOrdinal("profesion")) ? null : lector.GetString("profesion"),
                            EmpresaTrabajo = lector.IsDBNull(lector.GetOrdinal("empresa_trabajo")) ? null : lector.GetString("empresa_trabajo"),
                            TelefonoEmpresa = lector.IsDBNull(lector.GetOrdinal("telefono_empresa")) ? null : lector.GetString("telefono_empresa"),
                            DireccionEmpresa = lector.IsDBNull(lector.GetOrdinal("direccion_empresa")) ? null : lector.GetString("direccion_empresa"),
                            PersonaId = lector.GetInt32("r_persona_id"),
                            Estado = lector.GetString("r_estado"),
                            Persona = new Persona
                            {
                                Id = lector.GetInt32("prep_id"),
                                Nacionalidad = lector.GetString("prep_nac"),
                                CedulaIdentidad = lector.IsDBNull(lector.GetOrdinal("prep_cedula")) ? null : lector.GetString("prep_cedula"),
                                Nombre1 = lector.GetString("prep_nom1"),
                                Nombre2 = lector.IsDBNull(lector.GetOrdinal("prep_nom2")) ? null : lector.GetString("prep_nom2"),
                                Apellido1 = lector.GetString("prep_ape1"),
                                Apellido2 = lector.IsDBNull(lector.GetOrdinal("prep_ape2")) ? null : lector.GetString("prep_ape2"),
                                FechaNacimiento = lector.IsDBNull(lector.GetOrdinal("prep_fnac")) ? null : lector.GetDateTime("prep_fnac"),
                                Sexo = lector.GetString("prep_sexo"),
                                DireccionId = lector.IsDBNull(lector.GetOrdinal("prep_dir_id")) ? null : lector.GetInt32("prep_dir_id")
                            }
                        };

                        if (!lector.IsDBNull(lector.GetOrdinal("drep_id")))
                        {
                            rep.Persona.Direccion = new Direccion
                            {
                                Id = lector.GetInt32("drep_id"),
                                CiudadId = lector.GetInt32("drep_ciudad_id"),
                                Sector = lector.IsDBNull(lector.GetOrdinal("drep_sector")) ? null : lector.GetString("drep_sector"),
                                Avenida = lector.IsDBNull(lector.GetOrdinal("drep_avenida")) ? null : lector.GetString("drep_avenida"),
                                Calle = lector.IsDBNull(lector.GetOrdinal("drep_calle")) ? null : lector.GetString("drep_calle"),
                                Manzana = lector.IsDBNull(lector.GetOrdinal("drep_manzana")) ? null : lector.GetString("drep_manzana"),
                                Vereda = lector.IsDBNull(lector.GetOrdinal("drep_vereda")) ? null : lector.GetString("drep_vereda"),
                                NumeroVivienda = lector.IsDBNull(lector.GetOrdinal("drep_numero")) ? null : lector.GetString("drep_numero"),
                                TipoVivienda = lector.GetString("drep_tipo"),
                                CondicionVivienda = lector.GetString("drep_condicion"),
                                InfraestructuraVivienda = lector.GetString("drep_infra")
                            };
                        }

                        EstadoDireccionRepIdTemp = lector.IsDBNull(lector.GetOrdinal("rep_dir_estado_id")) ? 0 : lector.GetInt32("rep_dir_estado_id");
                        RepresentanteCargadoTemp = rep;
                    }

                    // Última Inscripción
                    if (!lector.IsDBNull(lector.GetOrdinal("inscripcion_id")))
                    {
                        UltimaInscripcionTemp = new Inscripcion
                        {
                            Id = lector.GetInt32("inscripcion_id"),
                            PeriodoId = lector.GetInt32("periodo_id"),
                            EstudianteId = est.Id,
                            TipoIngreso = lector.GetString("tipo_ingreso"),
                            ColegioProcedencia = lector.IsDBNull(lector.GetOrdinal("colegio_procedencia")) ? null : lector.GetString("colegio_procedencia"),
                            NivelAcademico = lector.GetString("nivel_academico")
                        };
                    }

                    return est;
                }
            }
        }

        public bool ExisteCedulaEscolar(string cedulaEscolar, int idExcluir = 0)
        {
            const string consulta = "SELECT 1 FROM PERSONA_ESTUDIANTE WHERE cedula_escolar = @cedula AND id <> @id LIMIT 1;";
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@cedula", cedulaEscolar);
                comando.Parameters.AddWithValue("@id", idExcluir);
                return comando.ExecuteScalar() != null;
            }
        }

        public void CambiarEstado(int estudianteId, string nuevoEstado)
        {
            const string consulta = "UPDATE PERSONA_ESTUDIANTE SET ESTADO = @estado WHERE id = @id;";
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@estado", nuevoEstado);
                comando.Parameters.AddWithValue("@id", estudianteId);
                comando.ExecuteNonQuery();
            }
        }

        public DataTable ObtenerEstudiantesActivos(int periodoId = 0)
        {
            const string consulta = @"
                SELECT e.id AS Codigo,
                       e.cedula_escolar AS 'Cedula Escolar',
                       CONCAT(p.nacionalidad, '-', IFNULL(p.cedula_identidad, 'S/C')) AS Cedula,
                       CONCAT_WS(' ', p.nombre_1, p.nombre_2, p.apellido_1, p.apellido_2) AS Estudiante,
                       p.sexo AS Sexo,
                       p.fecha_nacimiento AS 'Fecha de Nacimiento',
                       g.nombre AS Grado,
                       s.nombre AS Seccion,
                       pa.nombre AS Periodo,
                       i.tipo_ingreso AS 'Tipo de Ingreso',
                       CONCAT_WS(' ', pr.nombre_1, pr.apellido_1) AS Representante,
                       r.telefono_movil AS 'Telefono Representante'
                FROM PERSONA_ESTUDIANTE e
                INNER JOIN PERSONA p ON p.id = e.persona_id
                INNER JOIN PERSONA_REPRESENTANTE r ON r.id = e.representante_principal_id
                INNER JOIN PERSONA pr ON pr.id = r.persona_id
                LEFT JOIN INSCRIPCION i ON i.estudiante_id = e.id AND (@periodo = 0 OR i.periodo_id = @periodo)
                LEFT JOIN PERIODO_ACADEMICO pa ON pa.id = i.periodo_id
                LEFT JOIN GRADO_SECCION gs ON gs.id = i.grado_seccion_id
                LEFT JOIN GRADO g ON g.id = gs.grado_id
                LEFT JOIN SECCION s ON s.id = gs.seccion_id
                WHERE e.ESTADO = 'Activo'
                ORDER BY p.apellido_1, p.nombre_1;";

            DataTable tabla = new DataTable();
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@periodo", periodoId);
                using (MySqlDataAdapter adaptador = new MySqlDataAdapter(comando))
                {
                    adaptador.Fill(tabla);
                }
            }
            return tabla;
        }

        // ================= Métodos Internos de Inserción y Actualización =================

        private static int InsertarAntropometricos(Antropometricos datos, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO ANTROPOMETRICOS (estatura, peso, talla_camisa, talla_pantalon, talla_zapato)
                VALUES (@estatura, @peso, @camisa, @pantalon, @zapato);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@estatura", (object?)datos.Estatura ?? DBNull.Value);
                comando.Parameters.AddWithValue("@peso", (object?)datos.Peso ?? DBNull.Value);
                comando.Parameters.AddWithValue("@camisa", PersonaDatos.Nulo(datos.TallaCamisa));
                comando.Parameters.AddWithValue("@pantalon", PersonaDatos.Nulo(datos.TallaPantalon));
                comando.Parameters.AddWithValue("@zapato", (object?)datos.TallaZapato ?? DBNull.Value);

                datos.Id = Convert.ToInt32(comando.ExecuteScalar());
                return datos.Id;
            }
        }

        private static void ActualizarAntropometricos(Antropometricos datos, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                UPDATE ANTROPOMETRICOS 
                SET estatura = @estatura, peso = @peso, talla_camisa = @camisa, talla_pantalon = @pantalon, talla_zapato = @zapato
                WHERE id = @id;";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@id", datos.Id);
                comando.Parameters.AddWithValue("@estatura", (object?)datos.Estatura ?? DBNull.Value);
                comando.Parameters.AddWithValue("@peso", (object?)datos.Peso ?? DBNull.Value);
                comando.Parameters.AddWithValue("@camisa", PersonaDatos.Nulo(datos.TallaCamisa));
                comando.Parameters.AddWithValue("@pantalon", PersonaDatos.Nulo(datos.TallaPantalon));
                comando.Parameters.AddWithValue("@zapato", (object?)datos.TallaZapato ?? DBNull.Value);
                comando.ExecuteNonQuery();
            }
        }

        private static int InsertarSalud(Salud salud, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO SALUD (reacciones_alergicas, cuales_alergias, enfermedades_padecidas, atencion_especial,
                                   horario_tratamiento, atendido_por_especialista, nombre_especialista,
                                   fecha_inicio_especialista, condicion_atencion)
                VALUES (@alergicas, @cuales, @enfermedades, @atencion, @horario, @especialista,
                        @nombreEspecialista, @fechaEspecialista, @condicion);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@alergicas", salud.ReaccionesAlergicas);
                comando.Parameters.AddWithValue("@cuales", PersonaDatos.Nulo(salud.CualesAlergias));
                comando.Parameters.AddWithValue("@enfermedades", PersonaDatos.Nulo(salud.EnfermedadesPadecidas));
                comando.Parameters.AddWithValue("@atencion", salud.AtencionEspecial);
                comando.Parameters.AddWithValue("@horario", PersonaDatos.Nulo(salud.HorarioTratamiento));
                comando.Parameters.AddWithValue("@especialista", salud.AtendidoPorEspecialista);
                comando.Parameters.AddWithValue("@nombreEspecialista", PersonaDatos.Nulo(salud.NombreEspecialista));
                comando.Parameters.AddWithValue("@fechaEspecialista", (object?)salud.FechaInicioEspecialista ?? DBNull.Value);
                comando.Parameters.AddWithValue("@condicion", PersonaDatos.Nulo(salud.CondicionAtencion));

                salud.Id = Convert.ToInt32(comando.ExecuteScalar());
                return salud.Id;
            }
        }

        private static void ActualizarSalud(Salud salud, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                UPDATE SALUD 
                SET reacciones_alergicas = @alergicas, cuales_alergias = @cuales, enfermedades_padecidas = @enfermedades,
                    atencion_especial = @atencion, horario_tratamiento = @horario, atendido_por_especialista = @especialista,
                    nombre_especialista = @nombreEspecialista, fecha_inicio_especialista = @fechaEspecialista, condicion_atencion = @condicion
                WHERE id = @id;";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@id", salud.Id);
                comando.Parameters.AddWithValue("@alergicas", salud.ReaccionesAlergicas);
                comando.Parameters.AddWithValue("@cuales", PersonaDatos.Nulo(salud.CualesAlergias));
                comando.Parameters.AddWithValue("@enfermedades", PersonaDatos.Nulo(salud.EnfermedadesPadecidas));
                comando.Parameters.AddWithValue("@atencion", salud.AtencionEspecial);
                comando.Parameters.AddWithValue("@horario", PersonaDatos.Nulo(salud.HorarioTratamiento));
                comando.Parameters.AddWithValue("@especialista", salud.AtendidoPorEspecialista);
                comando.Parameters.AddWithValue("@nombreEspecialista", PersonaDatos.Nulo(salud.NombreEspecialista));
                comando.Parameters.AddWithValue("@fechaEspecialista", (object?)salud.FechaInicioEspecialista ?? DBNull.Value);
                comando.Parameters.AddWithValue("@condicion", PersonaDatos.Nulo(salud.CondicionAtencion));
                comando.ExecuteNonQuery();
            }
        }

        private static int InsertarExtraCurricular(ExtraCurricular datos, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO EXTRA_CURRICULAR (realiza_deportes, cuales_deportes, posee_canaima, fecha_asignacion_canaima,
                                              serial_canaima, estado_canaima, falla_canaima, posee_cargador,
                                              estado_cargador, falla_cargador)
                VALUES (@deportes, @cuales, @canaima, @fechaCanaima, @serial, @estadoCanaima, @fallaCanaima,
                        @cargador, @estadoCargador, @fallaCargador);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@deportes", datos.RealizaDeportes);
                comando.Parameters.AddWithValue("@cuales", PersonaDatos.Nulo(datos.CualesDeportes));
                comando.Parameters.AddWithValue("@canaima", datos.PoseeCanaima);
                comando.Parameters.AddWithValue("@fechaCanaima", (object?)datos.FechaAsignacionCanaima ?? DBNull.Value);
                comando.Parameters.AddWithValue("@serial", PersonaDatos.Nulo(datos.SerialCanaima));
                comando.Parameters.AddWithValue("@estadoCanaima", PersonaDatos.Nulo(datos.EstadoCanaima));
                comando.Parameters.AddWithValue("@fallaCanaima", PersonaDatos.Nulo(datos.FallaCanaima));
                comando.Parameters.AddWithValue("@cargador", datos.PoseeCargador);
                comando.Parameters.AddWithValue("@estadoCargador", PersonaDatos.Nulo(datos.EstadoCargador));
                comando.Parameters.AddWithValue("@fallaCargador", PersonaDatos.Nulo(datos.FallaCargador));

                datos.Id = Convert.ToInt32(comando.ExecuteScalar());
                return datos.Id;
            }
        }

        private static void ActualizarExtraCurricular(ExtraCurricular datos, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                UPDATE EXTRA_CURRICULAR 
                SET realiza_deportes = @deportes, cuales_deportes = @cuales, posee_canaima = @canaima,
                    fecha_asignacion_canaima = @fechaCanaima, serial_canaima = @serial, estado_canaima = @estadoCanaima,
                    falla_canaima = @fallaCanaima, posee_cargador = @cargador, estado_cargador = @estadoCargador, falla_cargador = @fallaCargador
                WHERE id = @id;";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@id", datos.Id);
                comando.Parameters.AddWithValue("@deportes", datos.RealizaDeportes);
                comando.Parameters.AddWithValue("@cuales", PersonaDatos.Nulo(datos.CualesDeportes));
                comando.Parameters.AddWithValue("@canaima", datos.PoseeCanaima);
                comando.Parameters.AddWithValue("@fechaCanaima", (object?)datos.FechaAsignacionCanaima ?? DBNull.Value);
                comando.Parameters.AddWithValue("@serial", PersonaDatos.Nulo(datos.SerialCanaima));
                comando.Parameters.AddWithValue("@estadoCanaima", PersonaDatos.Nulo(datos.EstadoCanaima));
                comando.Parameters.AddWithValue("@fallaCanaima", PersonaDatos.Nulo(datos.FallaCanaima));
                comando.Parameters.AddWithValue("@cargador", datos.PoseeCargador);
                comando.Parameters.AddWithValue("@estadoCargador", PersonaDatos.Nulo(datos.EstadoCargador));
                comando.Parameters.AddWithValue("@fallaCargador", PersonaDatos.Nulo(datos.FallaCargador));
                comando.ExecuteNonQuery();
            }
        }

        private static int InsertarEstudiante(Estudiante estudiante, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO PERSONA_ESTUDIANTE (cedula_escolar, numero_hijo, lateralidad, telefono_estudiante, correo_estudiante,
                                                persona_id, pais_nacimiento_id, parroquia_nacimiento_id, antropometrico_id,
                                                salud_id, extra_curricular_id, representante_principal_id, representante_secundario_id,
                                                situacion_padres, convive_con, padre_cedula, padre_nombres_apellidos, padre_telefono, padre_vive,
                                                madre_cedula, madre_nombres_apellidos, madre_telefono, madre_vive,
                                                representante_legal_tipo, oficio_cpnna_tribunal, observaciones_custodia, ESTADO)
                VALUES (@cedulaEscolar, @numeroHijo, @lateralidad, @telEst, @corrEst,
                        @persona, @pais, @parroquia, @antropometrico,
                        @salud, @extra, @representante, @representanteSecundario,
                        @sitPadres, @conviveCon, @padreCed, @padreNom, @padreTel, @padreVive,
                        @madreCed, @madreNom, @madreTel, @madreVive,
                        @repTipo, @oficioCpnna, @obsCustodia, @estado);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@cedulaEscolar", estudiante.CedulaEscolar);
                comando.Parameters.AddWithValue("@numeroHijo", estudiante.NumeroHijo);
                comando.Parameters.AddWithValue("@lateralidad", estudiante.Lateralidad);
                comando.Parameters.AddWithValue("@telEst", PersonaDatos.Nulo(estudiante.TelefonoEstudiante));
                comando.Parameters.AddWithValue("@corrEst", PersonaDatos.Nulo(estudiante.CorreoEstudiante));
                comando.Parameters.AddWithValue("@persona", estudiante.PersonaId);
                comando.Parameters.AddWithValue("@pais", estudiante.PaisNacimientoId);
                comando.Parameters.AddWithValue("@parroquia", (object?)estudiante.ParroquiaNacimientoId ?? DBNull.Value);
                comando.Parameters.AddWithValue("@antropometrico", estudiante.AntropometricoId);
                comando.Parameters.AddWithValue("@salud", estudiante.SaludId);
                comando.Parameters.AddWithValue("@extra", estudiante.ExtraCurricularId);
                comando.Parameters.AddWithValue("@representante", estudiante.RepresentantePrincipalId);
                comando.Parameters.AddWithValue("@representanteSecundario", (object?)estudiante.RepresentanteSecundarioId ?? DBNull.Value);

                comando.Parameters.AddWithValue("@sitPadres", estudiante.SituacionPadres);
                comando.Parameters.AddWithValue("@conviveCon", estudiante.ConviveCon);
                comando.Parameters.AddWithValue("@padreCed", PersonaDatos.Nulo(estudiante.PadreCedula));
                comando.Parameters.AddWithValue("@padreNom", PersonaDatos.Nulo(estudiante.PadreNombresApellidos));
                comando.Parameters.AddWithValue("@padreTel", PersonaDatos.Nulo(estudiante.PadreTelefono));
                comando.Parameters.AddWithValue("@padreVive", estudiante.PadreVive);

                comando.Parameters.AddWithValue("@madreCed", PersonaDatos.Nulo(estudiante.MadreCedula));
                comando.Parameters.AddWithValue("@madreNom", PersonaDatos.Nulo(estudiante.MadreNombresApellidos));
                comando.Parameters.AddWithValue("@madreTel", PersonaDatos.Nulo(estudiante.MadreTelefono));
                comando.Parameters.AddWithValue("@madreVive", estudiante.MadreVive);

                comando.Parameters.AddWithValue("@repTipo", estudiante.RepresentanteLegalTipo);
                comando.Parameters.AddWithValue("@oficioCpnna", PersonaDatos.Nulo(estudiante.OficioCpnnaTribunal));
                comando.Parameters.AddWithValue("@obsCustodia", PersonaDatos.Nulo(estudiante.ObservacionesCustodia));
                comando.Parameters.AddWithValue("@estado", estudiante.Estado);

                return Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        private static void ActualizarEstudiante(Estudiante estudiante, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                UPDATE PERSONA_ESTUDIANTE 
                SET cedula_escolar = @cedulaEscolar, 
                    numero_hijo = @numeroHijo, 
                    lateralidad = @lateralidad,
                    telefono_estudiante = @telEst,
                    correo_estudiante = @corrEst,
                    pais_nacimiento_id = @pais, 
                    parroquia_nacimiento_id = @parroquia, 
                    representante_principal_id = @representante,
                    representante_secundario_id = @representanteSecundario,
                    situacion_padres = @sitPadres,
                    convive_con = @conviveCon,
                    padre_cedula = @padreCed,
                    padre_nombres_apellidos = @padreNom,
                    padre_telefono = @padreTel,
                    padre_vive = @padreVive,
                    madre_cedula = @madreCed,
                    madre_nombres_apellidos = @madreNom,
                    madre_telefono = @madreTel,
                    madre_vive = @madreVive,
                    representante_legal_tipo = @repTipo,
                    oficio_cpnna_tribunal = @oficioCpnna,
                    observaciones_custodia = @obsCustodia,
                    ESTADO = @estado
                WHERE id = @id;";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@id", estudiante.Id);
                comando.Parameters.AddWithValue("@cedulaEscolar", estudiante.CedulaEscolar);
                comando.Parameters.AddWithValue("@numeroHijo", estudiante.NumeroHijo);
                comando.Parameters.AddWithValue("@lateralidad", estudiante.Lateralidad);
                comando.Parameters.AddWithValue("@telEst", PersonaDatos.Nulo(estudiante.TelefonoEstudiante));
                comando.Parameters.AddWithValue("@corrEst", PersonaDatos.Nulo(estudiante.CorreoEstudiante));
                comando.Parameters.AddWithValue("@pais", estudiante.PaisNacimientoId);
                comando.Parameters.AddWithValue("@parroquia", (object?)estudiante.ParroquiaNacimientoId ?? DBNull.Value);
                comando.Parameters.AddWithValue("@representante", estudiante.RepresentantePrincipalId);
                comando.Parameters.AddWithValue("@representanteSecundario", (object?)estudiante.RepresentanteSecundarioId ?? DBNull.Value);

                comando.Parameters.AddWithValue("@sitPadres", estudiante.SituacionPadres);
                comando.Parameters.AddWithValue("@conviveCon", estudiante.ConviveCon);
                comando.Parameters.AddWithValue("@padreCed", PersonaDatos.Nulo(estudiante.PadreCedula));
                comando.Parameters.AddWithValue("@padreNom", PersonaDatos.Nulo(estudiante.PadreNombresApellidos));
                comando.Parameters.AddWithValue("@padreTel", PersonaDatos.Nulo(estudiante.PadreTelefono));
                comando.Parameters.AddWithValue("@padreVive", estudiante.PadreVive);

                comando.Parameters.AddWithValue("@madreCed", PersonaDatos.Nulo(estudiante.MadreCedula));
                comando.Parameters.AddWithValue("@madreNom", PersonaDatos.Nulo(estudiante.MadreNombresApellidos));
                comando.Parameters.AddWithValue("@madreTel", PersonaDatos.Nulo(estudiante.MadreTelefono));
                comando.Parameters.AddWithValue("@madreVive", estudiante.MadreVive);

                comando.Parameters.AddWithValue("@repTipo", estudiante.RepresentanteLegalTipo);
                comando.Parameters.AddWithValue("@oficioCpnna", PersonaDatos.Nulo(estudiante.OficioCpnnaTribunal));
                comando.Parameters.AddWithValue("@obsCustodia", PersonaDatos.Nulo(estudiante.ObservacionesCustodia));
                comando.Parameters.AddWithValue("@estado", estudiante.Estado);

                comando.ExecuteNonQuery();
            }
        }

        private static int InsertarInscripcion(Inscripcion inscripcion, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO INSCRIPCION (periodo_id, estudiante_id, grado_seccion_id, tipo_ingreso,
                                         colegio_procedencia, nivel_academico, fecha_inscripcion)
                VALUES (@periodo, @estudiante, @gradoSeccion, @tipoIngreso, @colegio, @nivel, @fecha);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@periodo", inscripcion.PeriodoId);
                comando.Parameters.AddWithValue("@estudiante", inscripcion.EstudianteId);
                comando.Parameters.AddWithValue("@gradoSeccion", inscripcion.GradoSeccionId);
                comando.Parameters.AddWithValue("@tipoIngreso", inscripcion.TipoIngreso);
                comando.Parameters.AddWithValue("@colegio", PersonaDatos.Nulo(inscripcion.ColegioProcedencia));
                comando.Parameters.AddWithValue("@nivel", inscripcion.NivelAcademico);
                comando.Parameters.AddWithValue("@fecha", inscripcion.FechaInscripcion);

                return Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        private static void ActualizarInscripcion(Inscripcion inscripcion, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                UPDATE INSCRIPCION 
                SET periodo_id = @periodo, grado_seccion_id = @gradoSeccion, tipo_ingreso = @tipoIngreso,
                    colegio_procedencia = @colegio, nivel_academico = @nivel
                WHERE id = @id;";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@id", inscripcion.Id);
                comando.Parameters.AddWithValue("@periodo", inscripcion.PeriodoId);
                comando.Parameters.AddWithValue("@gradoSeccion", inscripcion.GradoSeccionId);
                comando.Parameters.AddWithValue("@tipoIngreso", inscripcion.TipoIngreso);
                comando.Parameters.AddWithValue("@colegio", PersonaDatos.Nulo(inscripcion.ColegioProcedencia));
                comando.Parameters.AddWithValue("@nivel", inscripcion.NivelAcademico);
                comando.ExecuteNonQuery();
            }
        }
    }
}
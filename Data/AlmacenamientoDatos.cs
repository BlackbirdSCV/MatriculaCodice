using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using SistemaCodice.Models;

namespace SistemaCodice.Data
{
    /// <summary>
    /// Capa de persistencia (RNF05), búsqueda (RNF03) y resguardo de datos (RNF02).
    /// Los datos viven en una base de datos SQL (SQLite, archivo datos.db). La aplicación trabaja con
    /// los objetos en memoria (Datos) y Guardar() los sincroniza con las tablas en una única transacción.
    /// </summary>
    public class AlmacenamientoDatos
    {
        private const string FormatoFecha = "yyyy-MM-dd";

        private const string EsquemaSql = @"
CREATE TABLE IF NOT EXISTS Usuarios (
    IdUsuario INTEGER PRIMARY KEY,
    Rol TEXT NOT NULL CHECK (Rol IN ('Profesor', 'Estudiante')),
    Nombre TEXT NOT NULL,
    Rut TEXT NOT NULL UNIQUE,
    Correo TEXT NOT NULL UNIQUE COLLATE NOCASE,
    ContrasenaHash TEXT NOT NULL,
    Especialidad TEXT,
    FechaIngreso TEXT
);
CREATE TABLE IF NOT EXISTS Asignaturas (
    Codigo TEXT PRIMARY KEY,
    Nombre TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS AsignaturaProfesor (
    Codigo TEXT NOT NULL REFERENCES Asignaturas(Codigo) ON DELETE CASCADE,
    IdProfesor INTEGER NOT NULL REFERENCES Usuarios(IdUsuario) ON DELETE CASCADE,
    PRIMARY KEY (Codigo, IdProfesor)
);
CREATE TABLE IF NOT EXISTS AsignaturaEstudiante (
    Codigo TEXT NOT NULL REFERENCES Asignaturas(Codigo) ON DELETE CASCADE,
    IdEstudiante INTEGER NOT NULL REFERENCES Usuarios(IdUsuario) ON DELETE CASCADE,
    PRIMARY KEY (Codigo, IdEstudiante)
);
CREATE TABLE IF NOT EXISTS Notas (
    IdNota INTEGER PRIMARY KEY,
    IdEstudiante INTEGER NOT NULL REFERENCES Usuarios(IdUsuario) ON DELETE CASCADE,
    Codigo TEXT NOT NULL REFERENCES Asignaturas(Codigo) ON DELETE CASCADE,
    Valor REAL NOT NULL CHECK (Valor BETWEEN 1.0 AND 7.0),
    Fecha TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Asistencias (
    IdAsistencia INTEGER PRIMARY KEY,
    IdEstudiante INTEGER NOT NULL REFERENCES Usuarios(IdUsuario) ON DELETE CASCADE,
    Codigo TEXT NOT NULL REFERENCES Asignaturas(Codigo) ON DELETE CASCADE,
    Fecha TEXT NOT NULL,
    Asistio INTEGER NOT NULL CHECK (Asistio IN (0, 1)),
    UNIQUE (IdEstudiante, Codigo, Fecha)
);";

        public string RutaArchivo { get; }
        public DatosSistema Datos { get; private set; } = new DatosSistema();

        public AlmacenamientoDatos(string rutaArchivo = null)
        {
            RutaArchivo = rutaArchivo ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InstitutoCodice", "datos.db");
        }

        // ------------------------------------------------------------------ conexión

        private SqliteConnection Abrir()
        {
            var cadena = new SqliteConnectionStringBuilder
            {
                DataSource = RutaArchivo,
                Pooling = false // libera el archivo al cerrar (permite respaldarlo o borrarlo)
            }.ToString();
            var con = new SqliteConnection(cadena);
            con.Open();
            Ejecutar(con, null, "PRAGMA foreign_keys = ON;");
            return con;
        }

        private static void Ejecutar(SqliteConnection con, SqliteTransaction tx, string sql,
                                     params (string nombre, object valor)[] parametros)
        {
            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = sql;
                foreach (var par in parametros)
                    cmd.Parameters.AddWithValue(par.nombre, par.valor ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void Leer(SqliteConnection con, string sql, Action<SqliteDataReader> porFila)
        {
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = sql;
                using (var lector = cmd.ExecuteReader())
                {
                    while (lector.Read()) porFila(lector);
                }
            }
        }

        private static string Texto(DateTime fecha)
        {
            return fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture);
        }

        private static DateTime Fecha(string texto)
        {
            return DateTime.ParseExact(texto, FormatoFecha, CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------------ carga

        /// <summary>
        /// RNF05: carga la base de datos. Si no existe, la crea a partir del datos.json de una versión
        /// anterior (si lo hay) o con datos de ejemplo.
        /// </summary>
        public void Cargar()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo));

            if (File.Exists(RutaArchivo))
            {
                using (var con = Abrir())
                {
                    Ejecutar(con, null, EsquemaSql);
                    Datos = LeerDesdeBaseDeDatos(con);
                }
                return;
            }

            Datos = ObtenerDatosIniciales();
            Guardar();
        }

        private DatosSistema ObtenerDatosIniciales()
        {
            string jsonAnterior = Path.Combine(Path.GetDirectoryName(RutaArchivo), "datos.json");
            if (File.Exists(jsonAnterior))
            {
                try
                {
                    var migrados = JsonSerializer.Deserialize<DatosSistema>(File.ReadAllText(jsonAnterior));
                    if (migrados != null) return migrados;
                }
                catch (JsonException)
                {
                    // JSON dañado: se parte con datos de ejemplo.
                }
            }
            return DatosSemilla.Crear();
        }

        private static DatosSistema LeerDesdeBaseDeDatos(SqliteConnection con)
        {
            var d = new DatosSistema();

            Leer(con, "SELECT IdUsuario, Rol, Nombre, Rut, Correo, ContrasenaHash, Especialidad, FechaIngreso " +
                      "FROM Usuarios ORDER BY IdUsuario", r =>
            {
                if (r.GetString(1) == "Profesor")
                {
                    d.Profesores.Add(new Profesor
                    {
                        IdUsuario = r.GetInt32(0), Nombre = r.GetString(2), Rut = r.GetString(3),
                        Correo = r.GetString(4), ContrasenaHash = r.GetString(5),
                        Especialidad = r.IsDBNull(6) ? "" : r.GetString(6)
                    });
                }
                else
                {
                    d.Estudiantes.Add(new Estudiante
                    {
                        IdUsuario = r.GetInt32(0), Nombre = r.GetString(2), Rut = r.GetString(3),
                        Correo = r.GetString(4), ContrasenaHash = r.GetString(5),
                        FechaIngreso = r.IsDBNull(7) ? DateTime.Today : Fecha(r.GetString(7))
                    });
                }
            });

            Leer(con, "SELECT Codigo, Nombre FROM Asignaturas ORDER BY Codigo", r =>
                d.Asignaturas.Add(new Asignatura { Codigo = r.GetString(0), Nombre = r.GetString(1) }));

            Leer(con, "SELECT Codigo, IdProfesor FROM AsignaturaProfesor", r =>
            {
                var a = d.BuscarAsignatura(r.GetString(0));
                if (a != null) a.IdProfesores.Add(r.GetInt32(1));
            });

            Leer(con, "SELECT Codigo, IdEstudiante FROM AsignaturaEstudiante", r =>
            {
                var a = d.BuscarAsignatura(r.GetString(0));
                if (a != null) a.IdEstudiantes.Add(r.GetInt32(1));
            });

            Leer(con, "SELECT IdNota, IdEstudiante, Codigo, Valor, Fecha FROM Notas ORDER BY IdNota", r =>
                d.Notas.Add(new Nota
                {
                    IdNota = r.GetInt32(0), IdEstudiante = r.GetInt32(1), CodigoAsignatura = r.GetString(2),
                    Valor = r.GetDouble(3), Fecha = Fecha(r.GetString(4))
                }));

            Leer(con, "SELECT IdAsistencia, IdEstudiante, Codigo, Fecha, Asistio FROM Asistencias ORDER BY IdAsistencia", r =>
                d.Asistencias.Add(new Asistencia
                {
                    IdAsistencia = r.GetInt32(0), IdEstudiante = r.GetInt32(1), CodigoAsignatura = r.GetString(2),
                    Fecha = Fecha(r.GetString(3)), Asistio = r.GetInt32(4) == 1
                }));

            return d;
        }

        // ------------------------------------------------------------------ guardado

        /// <summary>RNF05: sincroniza el estado actual con las tablas SQL (todo o nada, en una transacción).</summary>
        public void Guardar()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo));
            Proteger();

            using (var con = Abrir())
            {
                Ejecutar(con, null, EsquemaSql);
                using (var tx = con.BeginTransaction())
                {
                    // Primero las tablas hijas, para respetar las claves foráneas.
                    foreach (var tabla in new[] { "Asistencias", "Notas", "AsignaturaEstudiante",
                                                  "AsignaturaProfesor", "Asignaturas", "Usuarios" })
                        Ejecutar(con, tx, "DELETE FROM " + tabla + ";");

                    foreach (var p in Datos.Profesores)
                    {
                        Ejecutar(con, tx,
                            "INSERT INTO Usuarios (IdUsuario, Rol, Nombre, Rut, Correo, ContrasenaHash, Especialidad) " +
                            "VALUES (@id, 'Profesor', @nombre, @rut, @correo, @hash, @esp);",
                            ("@id", p.IdUsuario), ("@nombre", p.Nombre), ("@rut", p.Rut),
                            ("@correo", p.Correo), ("@hash", p.ContrasenaHash), ("@esp", p.Especialidad));
                    }

                    foreach (var e in Datos.Estudiantes)
                    {
                        Ejecutar(con, tx,
                            "INSERT INTO Usuarios (IdUsuario, Rol, Nombre, Rut, Correo, ContrasenaHash, FechaIngreso) " +
                            "VALUES (@id, 'Estudiante', @nombre, @rut, @correo, @hash, @ingreso);",
                            ("@id", e.IdUsuario), ("@nombre", e.Nombre), ("@rut", e.Rut),
                            ("@correo", e.Correo), ("@hash", e.ContrasenaHash), ("@ingreso", Texto(e.FechaIngreso)));
                    }

                    foreach (var a in Datos.Asignaturas)
                    {
                        Ejecutar(con, tx, "INSERT INTO Asignaturas (Codigo, Nombre) VALUES (@cod, @nombre);",
                            ("@cod", a.Codigo), ("@nombre", a.Nombre));
                        foreach (int idProfesor in a.IdProfesores)
                            Ejecutar(con, tx, "INSERT INTO AsignaturaProfesor (Codigo, IdProfesor) VALUES (@cod, @id);",
                                ("@cod", a.Codigo), ("@id", idProfesor));
                        foreach (int idEstudiante in a.IdEstudiantes)
                            Ejecutar(con, tx, "INSERT INTO AsignaturaEstudiante (Codigo, IdEstudiante) VALUES (@cod, @id);",
                                ("@cod", a.Codigo), ("@id", idEstudiante));
                    }

                    foreach (var n in Datos.Notas)
                    {
                        Ejecutar(con, tx,
                            "INSERT INTO Notas (IdNota, IdEstudiante, Codigo, Valor, Fecha) VALUES (@id, @est, @cod, @valor, @fecha);",
                            ("@id", n.IdNota), ("@est", n.IdEstudiante), ("@cod", n.CodigoAsignatura),
                            ("@valor", n.Valor), ("@fecha", Texto(n.Fecha)));
                    }

                    foreach (var s in Datos.Asistencias)
                    {
                        Ejecutar(con, tx,
                            "INSERT INTO Asistencias (IdAsistencia, IdEstudiante, Codigo, Fecha, Asistio) " +
                            "VALUES (@id, @est, @cod, @fecha, @asistio);",
                            ("@id", s.IdAsistencia), ("@est", s.IdEstudiante), ("@cod", s.CodigoAsignatura),
                            ("@fecha", Texto(s.Fecha)), ("@asistio", s.Asistio ? 1 : 0));
                    }

                    tx.Commit();
                }
            }
        }

        /// <summary>RNF02: antes de escribir, deja una copia de respaldo (.bak) de la base de datos anterior.</summary>
        public void Proteger()
        {
            if (File.Exists(RutaArchivo)) File.Copy(RutaArchivo, RutaArchivo + ".bak", true);
        }

        // ------------------------------------------------------------------ búsqueda

        /// <summary>RNF03: busca estudiantes por nombre, RUT (con o sin formato) o correo.</summary>
        public List<Estudiante> Buscar(string criterio)
        {
            IEnumerable<Estudiante> consulta = Datos.Estudiantes;
            if (!string.IsNullOrWhiteSpace(criterio))
            {
                string c = criterio.Trim();
                bool tieneDigitos = c.Any(char.IsDigit);
                string cLimpio = Validador.LimpiarRut(c);
                consulta = consulta.Where(x =>
                    Contiene(x.Nombre, c) || Contiene(x.Correo, c) || Contiene(x.Rut, c) ||
                    (tieneDigitos && Validador.LimpiarRut(x.Rut).Contains(cLimpio)));
            }
            return consulta.OrderBy(x => x.Nombre).ToList();
        }

        private static bool Contiene(string texto, string fragmento)
        {
            return texto != null && texto.IndexOf(fragmento, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}

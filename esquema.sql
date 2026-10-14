-- Esquema de la base de datos SQLite (datos.db) - Sistema de Asistencia y Notas, Instituto Codice
-- La aplicacion lo crea automaticamente; este archivo es solo documentacion/consulta
-- (se puede abrir datos.db con "DB Browser for SQLite" para ver las tablas).

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Usuarios (
    IdUsuario      INTEGER PRIMARY KEY,
    Rol            TEXT NOT NULL CHECK (Rol IN ('Profesor', 'Estudiante')),
    Nombre         TEXT NOT NULL,
    Rut            TEXT NOT NULL UNIQUE,
    Correo         TEXT NOT NULL UNIQUE COLLATE NOCASE,
    ContrasenaHash TEXT NOT NULL,          -- PBKDF2-SHA256 con sal (nunca texto plano)
    Especialidad   TEXT,                   -- solo Profesor
    FechaIngreso   TEXT                    -- solo Estudiante (yyyy-MM-dd)
);

CREATE TABLE IF NOT EXISTS Asignaturas (
    Codigo TEXT PRIMARY KEY,
    Nombre TEXT NOT NULL
);

-- Relacion "imparte" (Profesor 1..* - 1..* Asignatura)
CREATE TABLE IF NOT EXISTS AsignaturaProfesor (
    Codigo     TEXT    NOT NULL REFERENCES Asignaturas(Codigo) ON DELETE CASCADE,
    IdProfesor INTEGER NOT NULL REFERENCES Usuarios(IdUsuario) ON DELETE CASCADE,
    PRIMARY KEY (Codigo, IdProfesor)
);

-- Relacion "cursa" (Estudiante 0..* - 0..* Asignatura)
CREATE TABLE IF NOT EXISTS AsignaturaEstudiante (
    Codigo       TEXT    NOT NULL REFERENCES Asignaturas(Codigo) ON DELETE CASCADE,
    IdEstudiante INTEGER NOT NULL REFERENCES Usuarios(IdUsuario) ON DELETE CASCADE,
    PRIMARY KEY (Codigo, IdEstudiante)
);

CREATE TABLE IF NOT EXISTS Notas (
    IdNota       INTEGER PRIMARY KEY,
    IdEstudiante INTEGER NOT NULL REFERENCES Usuarios(IdUsuario) ON DELETE CASCADE,
    Codigo       TEXT    NOT NULL REFERENCES Asignaturas(Codigo) ON DELETE CASCADE,
    Valor        REAL    NOT NULL CHECK (Valor BETWEEN 1.0 AND 7.0),
    Fecha        TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS Asistencias (
    IdAsistencia INTEGER PRIMARY KEY,
    IdEstudiante INTEGER NOT NULL REFERENCES Usuarios(IdUsuario) ON DELETE CASCADE,
    Codigo       TEXT    NOT NULL REFERENCES Asignaturas(Codigo) ON DELETE CASCADE,
    Fecha        TEXT    NOT NULL,
    Asistio      INTEGER NOT NULL CHECK (Asistio IN (0, 1)),
    UNIQUE (IdEstudiante, Codigo, Fecha)   -- una asistencia por estudiante, asignatura y dia
);

-- Ejemplo de consulta: promedio por estudiante y asignatura
-- SELECT u.Nombre, n.Codigo, ROUND(AVG(n.Valor), 1) AS Promedio
-- FROM Notas n JOIN Usuarios u ON u.IdUsuario = n.IdEstudiante
-- GROUP BY u.Nombre, n.Codigo;

using System;
using System.Collections.Generic;
using System.Linq;
using SistemaCodice.Data;

namespace SistemaCodice.Models
{
    public class Estudiante : Usuario
    {
        public DateTime FechaIngreso { get; set; } = DateTime.Today;

        /// <summary>Valida datos obligatorios y unicidad de RUT y correo.</summary>
        private void Validar(AlmacenamientoDatos db)
        {
            if (string.IsNullOrWhiteSpace(Nombre))
                throw new ArgumentException("El nombre es obligatorio.");
            if (!Validador.RutValido(Rut))
                throw new ArgumentException("El RUT no es válido (revise el dígito verificador).");
            if (!Validador.CorreoValido(Correo))
                throw new ArgumentException("El correo electrónico no es válido.");

            string rutLimpio = Validador.LimpiarRut(Rut);
            bool rutRepetido = db.Datos.TodosLosUsuarios()
                .Any(u => u.IdUsuario != IdUsuario && Validador.LimpiarRut(u.Rut) == rutLimpio);
            if (rutRepetido) throw new InvalidOperationException("Ya existe un usuario con ese RUT.");

            bool correoRepetido = db.Datos.TodosLosUsuarios()
                .Any(u => u.IdUsuario != IdUsuario && string.Equals(u.Correo, Correo, StringComparison.OrdinalIgnoreCase));
            if (correoRepetido) throw new InvalidOperationException("Ya existe un usuario con ese correo.");
        }

        /// <summary>RF01: agrega el estudiante al sistema y lo matricula en las asignaturas indicadas.</summary>
        public void Agregar(AlmacenamientoDatos db, IEnumerable<string> codigosAsignaturas = null)
        {
            Nombre = (Nombre ?? "").Trim();
            Correo = (Correo ?? "").Trim();
            Validar(db);

            IdUsuario = db.Datos.SiguienteIdUsuario();
            Rut = Validador.FormatearRut(Rut);
            // Contraseña inicial: RUT sin puntos, guion ni dígito verificador.
            ContrasenaHash = Hash(Validador.CuerpoRut(Rut));
            db.Datos.Estudiantes.Add(this);

            Matricular(db, codigosAsignaturas);
            db.Guardar();
        }

        /// <summary>RF02: edita los datos del estudiante (revierte los cambios si no son válidos).</summary>
        public void EditarDatos(AlmacenamientoDatos db, string nombre, string rut, string correo,
                                IEnumerable<string> codigosAsignaturas = null)
        {
            string nombreOriginal = Nombre, rutOriginal = Rut, correoOriginal = Correo;
            try
            {
                Nombre = (nombre ?? "").Trim();
                Rut = rut ?? "";
                Correo = (correo ?? "").Trim();
                Validar(db);
                Rut = Validador.FormatearRut(Rut);
            }
            catch
            {
                Nombre = nombreOriginal;
                Rut = rutOriginal;
                Correo = correoOriginal;
                throw;
            }

            if (codigosAsignaturas != null) Matricular(db, codigosAsignaturas);
            db.Guardar();
        }

        /// <summary>
        /// Elimina al estudiante junto con sus notas, asistencias y matrículas (no hay registros huérfanos).
        /// Extensión al UML original.
        /// </summary>
        public void Eliminar(AlmacenamientoDatos db)
        {
            foreach (var a in db.Datos.Asignaturas) a.QuitarEstudiante(this);
            db.Datos.Notas.RemoveAll(x => x.IdEstudiante == IdUsuario);
            db.Datos.Asistencias.RemoveAll(x => x.IdEstudiante == IdUsuario);
            db.Datos.Estudiantes.Remove(this);
            db.Guardar();
        }

        private void Matricular(AlmacenamientoDatos db, IEnumerable<string> codigos)
        {
            if (codigos == null) return;
            var lista = codigos.ToList();
            foreach (var a in db.Datos.Asignaturas)
            {
                if (lista.Contains(a.Codigo)) a.AgregarEstudiante(this);
                else a.QuitarEstudiante(this);
            }
        }

        /// <summary>RF06: notas del estudiante.</summary>
        public List<Nota> VerNotas(AlmacenamientoDatos db)
        {
            return db.Datos.Notas.Where(x => x.IdEstudiante == IdUsuario)
                .OrderBy(x => x.Fecha).ThenBy(x => x.IdNota).ToList();
        }

        /// <summary>RF06: asistencias del estudiante.</summary>
        public List<Asistencia> VerAsistencias(AlmacenamientoDatos db)
        {
            return db.Datos.Asistencias.Where(x => x.IdEstudiante == IdUsuario)
                .OrderBy(x => x.Fecha).ToList();
        }

        /// <summary>RF07: promedio aritmético de las notas en la asignatura (0 si no tiene notas).</summary>
        public double CalcularPromedio(Asignatura a, AlmacenamientoDatos db)
        {
            var notas = a.ObtenerNotas(this, db);
            return notas.Count == 0 ? 0.0 : notas.Average(x => x.Valor);
        }

        public override string ToString()
        {
            return Nombre + " (" + Rut + ")";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SistemaCodice.Data;

namespace SistemaCodice.Models
{
    public class Profesor : Usuario
    {
        public string Especialidad { get; set; } = "";

        private void VerificarAsignatura(Asignatura a)
        {
            if (!a.IdProfesores.Contains(IdUsuario))
                throw new InvalidOperationException("Usted no imparte la asignatura " + a.Codigo + ".");
        }

        private void VerificarAcceso(Estudiante e, Asignatura a)
        {
            VerificarAsignatura(a);
            if (!a.IdEstudiantes.Contains(e.IdUsuario))
                throw new InvalidOperationException(e.Nombre + " no cursa la asignatura " + a.Codigo + ".");
        }

        /// <summary>RF03: registra (o actualiza si ya existía) la asistencia de un estudiante en una fecha.</summary>
        public void RegistrarAsistencia(Estudiante e, Asignatura a, DateTime fecha, bool asistio, AlmacenamientoDatos db)
        {
            VerificarAcceso(e, a);
            if (fecha.Date > DateTime.Today)
                throw new ArgumentException("No se puede registrar asistencia en una fecha futura.");

            var existente = db.Datos.Asistencias.FirstOrDefault(x =>
                x.IdEstudiante == e.IdUsuario && x.CodigoAsignatura == a.Codigo && x.Fecha.Date == fecha.Date);

            if (existente != null)
            {
                existente.Modificar(asistio, db);
            }
            else
            {
                var nueva = new Asistencia
                {
                    IdEstudiante = e.IdUsuario,
                    CodigoAsignatura = a.Codigo,
                    Fecha = fecha.Date,
                    Asistio = asistio
                };
                nueva.Registrar(db);
            }
        }

        /// <summary>RF04: registra una nota.</summary>
        public void RegistrarNota(Estudiante e, Asignatura a, double valor, AlmacenamientoDatos db)
        {
            VerificarAcceso(e, a);
            var nota = new Nota { IdEstudiante = e.IdUsuario, CodigoAsignatura = a.Codigo, Valor = valor };
            nota.Registrar(db);
        }

        /// <summary>RF05: modifica una nota existente.</summary>
        public void ModificarNota(Nota n, double valor, AlmacenamientoDatos db)
        {
            var asignatura = db.Datos.BuscarAsignatura(n.CodigoAsignatura);
            if (asignatura == null) throw new InvalidOperationException("La asignatura de la nota ya no existe.");
            VerificarAsignatura(asignatura);
            n.Modificar(valor, db);
        }

        /// <summary>RF06: notas de todos los estudiantes de una asignatura.</summary>
        public List<Nota> VerNotas(Asignatura a, AlmacenamientoDatos db)
        {
            VerificarAsignatura(a);
            return db.Datos.Notas.Where(x => x.CodigoAsignatura == a.Codigo)
                .OrderBy(x => x.Fecha).ThenBy(x => x.IdNota).ToList();
        }

        /// <summary>RF06: asistencias de todos los estudiantes de una asignatura.</summary>
        public List<Asistencia> VerAsistencias(Asignatura a, AlmacenamientoDatos db)
        {
            VerificarAsignatura(a);
            return db.Datos.Asistencias.Where(x => x.CodigoAsignatura == a.Codigo)
                .OrderBy(x => x.Fecha).ToList();
        }
    }
}

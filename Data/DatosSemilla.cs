using System;
using SistemaCodice.Models;

namespace SistemaCodice.Data
{
    /// <summary>Datos de ejemplo para la primera ejecución (versión preliminar).</summary>
    public static class DatosSemilla
    {
        public static DatosSistema Crear()
        {
            var d = new DatosSistema();

            d.Profesores.Add(new Profesor
            {
                IdUsuario = 1, Nombre = "Carolina Rojas", Rut = "10.000.000-8",
                Correo = "profe@codice.cl", Especialidad = "Informática",
                ContrasenaHash = Usuario.Hash("profesor123")
            });

            d.Estudiantes.Add(NuevoEstudiante(2, "Ana Pérez", "12.345.678-5", "ana@codice.cl"));
            d.Estudiantes.Add(NuevoEstudiante(3, "Luis Soto", "11.111.111-1", "luis@codice.cl"));
            d.Estudiantes.Add(NuevoEstudiante(4, "Marta Díaz", "9.876.543-3", "marta@codice.cl"));

            var mat = new Asignatura { Codigo = "MAT101", Nombre = "Matemática Aplicada" };
            var pro = new Asignatura { Codigo = "PRO101", Nombre = "Programación I" };
            foreach (var a in new[] { mat, pro })
            {
                a.IdProfesores.Add(1);
                a.IdEstudiantes.AddRange(new[] { 2, 3, 4 });
                d.Asignaturas.Add(a);
            }

            AgregarNota(d, 2, "MAT101", 6.0, new DateTime(2026, 9, 10));
            AgregarNota(d, 2, "MAT101", 5.5, new DateTime(2026, 9, 24));
            AgregarNota(d, 2, "PRO101", 6.8, new DateTime(2026, 9, 17));
            AgregarNota(d, 3, "MAT101", 4.2, new DateTime(2026, 9, 10));
            AgregarNota(d, 3, "MAT101", 3.8, new DateTime(2026, 9, 24));
            AgregarNota(d, 4, "PRO101", 5.0, new DateTime(2026, 9, 17));

            foreach (var fecha in new[] { new DateTime(2026, 10, 5), new DateTime(2026, 10, 6) })
            {
                AgregarAsistencia(d, 2, "MAT101", fecha, true);
                AgregarAsistencia(d, 3, "MAT101", fecha, fecha.Day == 5);
                AgregarAsistencia(d, 4, "MAT101", fecha, true);
            }
            return d;
        }

        private static Estudiante NuevoEstudiante(int id, string nombre, string rut, string correo)
        {
            return new Estudiante
            {
                IdUsuario = id, Nombre = nombre, Rut = rut, Correo = correo,
                FechaIngreso = new DateTime(2026, 3, 2),
                ContrasenaHash = Usuario.Hash("estudiante123")
            };
        }

        private static void AgregarNota(DatosSistema d, int idEst, string cod, double valor, DateTime fecha)
        {
            d.Notas.Add(new Nota
            {
                IdNota = d.SiguienteIdNota(), IdEstudiante = idEst,
                CodigoAsignatura = cod, Valor = valor, Fecha = fecha
            });
        }

        private static void AgregarAsistencia(DatosSistema d, int idEst, string cod, DateTime fecha, bool asistio)
        {
            d.Asistencias.Add(new Asistencia
            {
                IdAsistencia = d.SiguienteIdAsistencia(), IdEstudiante = idEst,
                CodigoAsignatura = cod, Fecha = fecha, Asistio = asistio
            });
        }
    }
}

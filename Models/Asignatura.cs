using System.Collections.Generic;
using System.Linq;
using SistemaCodice.Data;

namespace SistemaCodice.Models
{
    public class Asignatura
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";

        // Asociaciones del UML (cursa / imparte): se guardan como listas de identificadores.
        public List<int> IdProfesores { get; set; } = new List<int>();
        public List<int> IdEstudiantes { get; set; } = new List<int>();

        public void AgregarEstudiante(Estudiante e)
        {
            if (!IdEstudiantes.Contains(e.IdUsuario)) IdEstudiantes.Add(e.IdUsuario);
        }

        public void QuitarEstudiante(Estudiante e)
        {
            IdEstudiantes.Remove(e.IdUsuario);
        }

        public void AsignarProfesor(Profesor p)
        {
            if (!IdProfesores.Contains(p.IdUsuario)) IdProfesores.Add(p.IdUsuario);
        }

        public List<Estudiante> ObtenerEstudiantes(AlmacenamientoDatos db)
        {
            return db.Datos.Estudiantes
                .Where(x => IdEstudiantes.Contains(x.IdUsuario))
                .OrderBy(x => x.Nombre).ToList();
        }

        public List<Nota> ObtenerNotas(Estudiante e, AlmacenamientoDatos db)
        {
            return db.Datos.Notas
                .Where(x => x.CodigoAsignatura == Codigo && x.IdEstudiante == e.IdUsuario)
                .OrderBy(x => x.Fecha).ThenBy(x => x.IdNota).ToList();
        }

        public override string ToString()
        {
            return Codigo + " – " + Nombre;
        }
    }
}

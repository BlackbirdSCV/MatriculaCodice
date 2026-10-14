using System.Collections.Generic;
using System.Linq;
using SistemaCodice.Models;

namespace SistemaCodice.Data
{
    /// <summary>Contenedor serializable (JSON) con todas las entidades del sistema.</summary>
    public class DatosSistema
    {
        public List<Estudiante> Estudiantes { get; set; } = new List<Estudiante>();
        public List<Profesor> Profesores { get; set; } = new List<Profesor>();
        public List<Asignatura> Asignaturas { get; set; } = new List<Asignatura>();
        public List<Nota> Notas { get; set; } = new List<Nota>();
        public List<Asistencia> Asistencias { get; set; } = new List<Asistencia>();

        public IEnumerable<Usuario> TodosLosUsuarios()
        {
            return Profesores.Cast<Usuario>().Concat(Estudiantes.Cast<Usuario>());
        }

        public int SiguienteIdUsuario()
        {
            return TodosLosUsuarios().Select(u => u.IdUsuario).DefaultIfEmpty(0).Max() + 1;
        }

        public int SiguienteIdNota()
        {
            return Notas.Select(n => n.IdNota).DefaultIfEmpty(0).Max() + 1;
        }

        public int SiguienteIdAsistencia()
        {
            return Asistencias.Select(a => a.IdAsistencia).DefaultIfEmpty(0).Max() + 1;
        }

        public Estudiante BuscarEstudiante(int id)
        {
            return Estudiantes.FirstOrDefault(x => x.IdUsuario == id);
        }

        public Asignatura BuscarAsignatura(string codigo)
        {
            return Asignaturas.FirstOrDefault(x => x.Codigo == codigo);
        }

        /// <summary>Autentica por correo y contraseña. Devuelve null si las credenciales no coinciden.</summary>
        public Usuario Autenticar(string correo, string contrasena)
        {
            foreach (var u in TodosLosUsuarios())
            {
                if (u.IniciarSesion(correo, contrasena)) return u;
            }
            return null;
        }
    }
}

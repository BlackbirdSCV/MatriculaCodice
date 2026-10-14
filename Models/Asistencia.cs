using System;
using SistemaCodice.Data;

namespace SistemaCodice.Models
{
    public class Asistencia
    {
        public int IdAsistencia { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;
        public bool Asistio { get; set; }
        public int IdEstudiante { get; set; }
        public string CodigoAsignatura { get; set; } = "";

        /// <summary>RF03: registra la asistencia y la persiste.</summary>
        public void Registrar(AlmacenamientoDatos db)
        {
            IdAsistencia = db.Datos.SiguienteIdAsistencia();
            Fecha = Fecha.Date;
            db.Datos.Asistencias.Add(this);
            db.Guardar();
        }

        public void Modificar(bool asistio, AlmacenamientoDatos db)
        {
            Asistio = asistio;
            db.Guardar();
        }
    }
}

using System;
using SistemaCodice.Data;

namespace SistemaCodice.Models
{
    public class Nota
    {
        public int IdNota { get; set; }
        public double Valor { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;
        public int IdEstudiante { get; set; }
        public string CodigoAsignatura { get; set; } = "";

        public static void ValidarValor(double valor)
        {
            if (valor < 1.0 || valor > 7.0)
                throw new ArgumentException("La nota debe estar entre 1,0 y 7,0.");
        }

        /// <summary>RF04: registra la nota y la persiste.</summary>
        public void Registrar(AlmacenamientoDatos db)
        {
            ValidarValor(Valor);
            Valor = Math.Round(Valor, 1);
            IdNota = db.Datos.SiguienteIdNota();
            Fecha = DateTime.Today;
            db.Datos.Notas.Add(this);
            db.Guardar();
        }

        /// <summary>RF05: modifica el valor de una nota existente.</summary>
        public void Modificar(double valor, AlmacenamientoDatos db)
        {
            ValidarValor(valor);
            Valor = Math.Round(valor, 1);
            db.Guardar();
        }
    }
}

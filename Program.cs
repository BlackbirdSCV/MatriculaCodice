using System;
using System.Windows.Forms;
using SistemaCodice.Data;
using SistemaCodice.Forms;
using SistemaCodice.Models;

namespace SistemaCodice
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();

            var db = new AlmacenamientoDatos();
            try
            {
                db.Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No fue posible cargar los datos (" + db.RutaArchivo + "):\n" + ex.Message,
                    "Instituto Códice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Ciclo: login -> ventana principal -> (cerrar sesión) -> login
            while (true)
            {
                Usuario usuario;
                using (var login = new LoginForm(db))
                {
                    if (login.ShowDialog() != DialogResult.OK) break;
                    usuario = login.UsuarioAutenticado;
                }

                using (var principal = new MainForm(usuario, db))
                {
                    Application.Run(principal);
                    if (!principal.CerroSesion) break;
                }
            }
        }
    }
}

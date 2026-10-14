using System;
using System.Drawing;
using System.Windows.Forms;
using SistemaCodice.Data;
using SistemaCodice.Models;

namespace SistemaCodice.Forms
{
    /// <summary>Pantalla de inicio de sesión (Usuario.iniciarSesion, RNF02).</summary>
    public class LoginForm : Form
    {
        private const int MaxIntentos = 3;

        private readonly AlmacenamientoDatos _db;
        private readonly TextBox txtCorreo;
        private readonly TextBox txtClave;
        private int _intentos;

        public Usuario UsuarioAutenticado { get; private set; }

        public LoginForm(AlmacenamientoDatos db)
        {
            _db = db;

            Text = "Instituto Códice – Iniciar sesión";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(380, 300);

            var titulo = new Label
            {
                Text = "Sistema de Asistencia y Notas",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 56, 100),
                AutoSize = false, TextAlign = ContentAlignment.MiddleCenter,
                Left = 10, Top = 16, Width = 360, Height = 34
            };
            var subtitulo = new Label
            {
                Text = "Instituto Códice",
                AutoSize = false, TextAlign = ContentAlignment.MiddleCenter,
                Left = 10, Top = 48, Width = 360, Height = 22
            };

            var lblCorreo = new Label { Text = "Correo:", Left = 30, Top = 92, AutoSize = true };
            txtCorreo = new TextBox { Left = 30, Top = 112, Width = 320 };
            var lblClave = new Label { Text = "Contraseña:", Left = 30, Top = 146, AutoSize = true };
            txtClave = new TextBox { Left = 30, Top = 166, Width = 320, UseSystemPasswordChar = true };

            var btnIngresar = new Button { Text = "Ingresar", Left = 30, Top = 206, Width = 320, Height = 34 };
            btnIngresar.Click += (s, e) => Ingresar();

            var ayuda = new Label
            {
                Text = "Demostración – Profesor: profe@codice.cl / profesor123\nEstudiante: ana@codice.cl / estudiante123",
                ForeColor = Color.DimGray, AutoSize = false,
                Left = 30, Top = 248, Width = 320, Height = 40
            };

            Controls.AddRange(new Control[] { titulo, subtitulo, lblCorreo, txtCorreo, lblClave, txtClave, btnIngresar, ayuda });
            AcceptButton = btnIngresar;
        }

        private void Ingresar()
        {
            var usuario = _db.Datos.Autenticar(txtCorreo.Text.Trim(), txtClave.Text);
            if (usuario != null)
            {
                UsuarioAutenticado = usuario;
                DialogResult = DialogResult.OK;
                return;
            }

            _intentos++;
            txtClave.Clear();
            if (_intentos >= MaxIntentos)
            {
                MessageBox.Show("Demasiados intentos fallidos. La aplicación se cerrará.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.Cancel;
                return;
            }
            MessageBox.Show("Correo o contraseña incorrectos. Intentos restantes: " + (MaxIntentos - _intentos),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtClave.Focus();
        }
    }
}

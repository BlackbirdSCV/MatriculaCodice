using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SistemaCodice.Data;
using SistemaCodice.Models;

namespace SistemaCodice.Forms
{
    /// <summary>
    /// Ventana principal. El Profesor accede a las 4 pestañas; el Estudiante solo a "Consultas".
    /// Pestañas: Estudiantes (RF01, RF02, RNF03) · Asistencia (RF03) · Notas (RF04, RF05) · Consultas (RF06, RF07).
    /// </summary>
    public class MainForm : Form
    {
        private readonly Usuario _usuario;
        private readonly AlmacenamientoDatos _db;
        private readonly TabControl _tabs = new TabControl();
        private int _suspender; // >0 mientras se rellenan controles (evita eventos en cascada)

        public bool CerroSesion { get; private set; }

        // --- Estudiantes
        private TextBox txtBuscar, txtNombre, txtRut, txtCorreo;
        private DataGridView gridEst;
        private CheckedListBox clbAsig;
        private Estudiante _estSel;

        // --- Asistencia
        private ComboBox cboAsisAsig;
        private DateTimePicker dtpAsis;
        private DataGridView gridAsis;
        private Label lblAsisEstado;

        // --- Notas
        private ComboBox cboNotasAsig, cboNotasEst;
        private NumericUpDown nudNota;
        private DataGridView gridNotas;

        // --- Consultas
        private ComboBox cboConEst, cboConAsig;
        private DataGridView gridConNotas, gridConAsis;
        private Label lblResumen;

        private TabPage tabEst, tabAsis, tabNotas, tabCon;

        public MainForm(Usuario usuario, AlmacenamientoDatos db)
        {
            _usuario = usuario;
            _db = db;

            Text = "Instituto Códice – Sistema de Asistencia y Notas";
            var area = Screen.PrimaryScreen.WorkingArea;
            ClientSize = new Size(Math.Min(960, area.Width - 40), Math.Min(600, area.Height - 80));
            MinimumSize = new Size(Math.Min(760, area.Width - 40), Math.Min(520, area.Height - 80));
            StartPosition = FormStartPosition.CenterScreen;

            string rol = usuario is Profesor ? "Profesor" : "Estudiante";
            var lblUsuario = new Label
            {
                Text = usuario.Nombre + "  (" + rol + ")",
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0)
            };
            var btnSalir = new Button
            {
                Text = "Cerrar sesión", Dock = DockStyle.Right, Width = 130,
                FlatStyle = FlatStyle.Flat, ForeColor = Color.White
            };
            btnSalir.Click += (s, e) => { usuario.CerrarSesion(); CerroSesion = true; Close(); };

            var encabezado = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.FromArgb(31, 56, 100) };
            encabezado.Controls.Add(lblUsuario);
            encabezado.Controls.Add(btnSalir);

            _tabs.Dock = DockStyle.Fill;
            if (usuario is Profesor)
            {
                tabEst = CrearTabEstudiantes();
                tabAsis = CrearTabAsistencia();
                tabNotas = CrearTabNotas();
                _tabs.TabPages.AddRange(new[] { tabEst, tabAsis, tabNotas });
            }
            tabCon = CrearTabConsultas();
            _tabs.TabPages.Add(tabCon);

            // Control con Dock=Fill se agrega primero; luego los que se acoplan a los bordes.
            Controls.Add(_tabs);
            Controls.Add(encabezado);

            _tabs.SelectedIndexChanged += (s, e) => ActualizarPestanaActiva();
            Load += (s, e) => ActualizarPestanaActiva();
        }

        // ================================================================= utilidades

        private static DataGridView CrearGrid()
        {
            return new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void Info(string mensaje)
        {
            MessageBox.Show(this, mensaje, "Instituto Códice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Error(string mensaje)
        {
            MessageBox.Show(this, mensaje, "Instituto Códice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private Profesor ProfesorActual { get { return _usuario as Profesor; } }

        /// <summary>Asignaturas visibles para el usuario (las que imparte o las que cursa).</summary>
        private List<Asignatura> AsignaturasDelUsuario()
        {
            return _db.Datos.Asignaturas
                .Where(a => _usuario is Profesor ? a.IdProfesores.Contains(_usuario.IdUsuario)
                                                 : a.IdEstudiantes.Contains(_usuario.IdUsuario))
                .OrderBy(a => a.Nombre).ToList();
        }

        /// <summary>Rellena un combo conservando (si existe) la selección anterior.</summary>
        private void Rellenar(ComboBox cbo, IEnumerable<object> items)
        {
            string previo = cbo.SelectedItem == null ? null : cbo.SelectedItem.ToString();
            _suspender++;
            cbo.Items.Clear();
            foreach (var item in items) cbo.Items.Add(item);
            if (cbo.Items.Count > 0)
            {
                int indice = 0;
                if (previo != null)
                {
                    for (int i = 0; i < cbo.Items.Count; i++)
                        if (cbo.Items[i].ToString() == previo) { indice = i; break; }
                }
                cbo.SelectedIndex = indice;
            }
            _suspender--;
        }

        private static ComboBox CrearCombo(int left, int top, int width)
        {
            return new ComboBox
            {
                Left = left, Top = top, Width = width, DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 4, 14, 0)
            };
        }

        /// <summary>Etiqueta para barras con flujo automático (se adapta a la fuente y escala de pantalla).</summary>
        private static Label Etiqueta(string texto)
        {
            return new Label { Text = texto, AutoSize = true, Margin = new Padding(8, 8, 4, 0) };
        }

        /// <summary>Barra superior: los controles se colocan uno tras otro y nunca se superponen.</summary>
        private static FlowLayoutPanel CrearBarra()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink,
                WrapContents = true, Padding = new Padding(2, 4, 2, 6)
            };
        }

        private void ActualizarPestanaActiva()
        {
            var tab = _tabs.SelectedTab;
            if (tab == tabEst) { CargarAsignaturasClb(); CargarEstudiantes(); }
            else if (tab == tabAsis) { Rellenar(cboAsisAsig, AsignaturasDelUsuario().Cast<object>()); CargarAsistencia(); }
            else if (tab == tabNotas) { Rellenar(cboNotasAsig, AsignaturasDelUsuario().Cast<object>()); AlCambiarAsignaturaNotas(); }
            else if (tab == tabCon) { PrepararConsultas(); }
        }

        // ================================================================= ESTUDIANTES (RF01, RF02, RNF03)

        private TabPage CrearTabEstudiantes()
        {
            var tab = new TabPage("Estudiantes");

            txtBuscar = new TextBox { Left = 205, Top = 12, Width = 220 };
            txtBuscar.TextChanged += (s, e) => CargarEstudiantes();
            var btnNuevo = new Button { Text = "+ Nuevo estudiante", Left = 445, Top = 8, Width = 170, Height = 30 };
            btnNuevo.Click += (s, e) => { LimpiarFormEstudiante(); txtNombre.Focus(); };

            txtBuscar.Margin = new Padding(0, 4, 14, 0);
            btnNuevo.Margin = new Padding(0, 1, 3, 0);
            var barra = CrearBarra();
            barra.Controls.AddRange(new Control[]
            {
                Etiqueta("Buscar (nombre, RUT o correo):"), txtBuscar, btnNuevo
            });

            gridEst = CrearGrid();
            gridEst.Dock = DockStyle.Fill;
            gridEst.Columns.Add("nombre", "Nombre");
            gridEst.Columns.Add("rut", "RUT");
            gridEst.Columns.Add("correo", "Correo");
            gridEst.Columns.Add("ingreso", "Ingreso");
            gridEst.SelectionChanged += (s, e) => SeleccionarEstudiante();

            txtNombre = new TextBox { Left = 15, Top = 46, Width = 290 };
            txtRut = new TextBox { Left = 15, Top = 96, Width = 290 };
            txtCorreo = new TextBox { Left = 15, Top = 146, Width = 290 };
            clbAsig = new CheckedListBox { Left = 15, Top = 200, Width = 290, Height = 90, CheckOnClick = true };

            var btnAgregar = new Button { Text = "Agregar nuevo", Left = 15, Top = 305, Width = 140, Height = 32 };
            btnAgregar.Click += (s, e) => AgregarEstudiante();
            var btnGuardar = new Button { Text = "Guardar cambios", Left = 165, Top = 305, Width = 140, Height = 32 };
            btnGuardar.Click += (s, e) => EditarEstudiante();
            var btnLimpiar = new Button { Text = "Limpiar formulario", Left = 15, Top = 345, Width = 290, Height = 30 };
            btnLimpiar.Click += (s, e) => LimpiarFormEstudiante();

            var btnEliminar = new Button
            {
                Text = "Eliminar estudiante seleccionado", Left = 15, Top = 383, Width = 290, Height = 30,
                ForeColor = Color.Firebrick
            };
            btnEliminar.Click += (s, e) => EliminarEstudiante();

            var nota = new Label
            {
                Text = "Para editar o eliminar, seleccione un estudiante de la lista. " +
                       "La contraseña inicial de uno nuevo es su RUT sin puntos, guion ni dígito verificador.",
                ForeColor = Color.DimGray, Left = 15, Top = 422, Width = 290, Height = 56
            };

            var grp = new GroupBox { Text = "Agregar / editar estudiante", Left = 5, Top = 0, Width = 320, Height = 490 };
            grp.Controls.AddRange(new Control[]
            {
                new Label { Text = "Nombre completo:", Left = 15, Top = 26, AutoSize = true }, txtNombre,
                new Label { Text = "RUT (ej: 12.345.678-5):", Left = 15, Top = 76, AutoSize = true }, txtRut,
                new Label { Text = "Correo electrónico:", Left = 15, Top = 126, AutoSize = true }, txtCorreo,
                new Label { Text = "Asignaturas que cursa:", Left = 15, Top = 180, AutoSize = true }, clbAsig,
                btnAgregar, btnGuardar, btnLimpiar, btnEliminar, nota
            });

            // Panel derecho con barra de desplazamiento: el formulario siempre es accesible.
            var panelDerecho = new Panel { Dock = DockStyle.Right, Width = 345, AutoScroll = true };
            panelDerecho.Controls.Add(grp);

            // Orden de acople: Fill primero; luego Right; Top al final.
            tab.Controls.Add(gridEst);
            tab.Controls.Add(panelDerecho);
            tab.Controls.Add(barra);
            return tab;
        }

        private void CargarAsignaturasClb()
        {
            clbAsig.Items.Clear();
            foreach (var a in _db.Datos.Asignaturas.OrderBy(x => x.Nombre)) clbAsig.Items.Add(a);
        }

        private void CargarEstudiantes()
        {
            _suspender++;
            gridEst.Rows.Clear();
            foreach (var est in _db.Buscar(txtBuscar.Text))
            {
                int i = gridEst.Rows.Add(est.Nombre, est.Rut, est.Correo,
                                         est.FechaIngreso.ToString("dd-MM-yyyy"));
                gridEst.Rows[i].Tag = est;
            }
            gridEst.ClearSelection();
            _suspender--;
            _estSel = null;
        }

        private void SeleccionarEstudiante()
        {
            if (_suspender > 0 || gridEst.SelectedRows.Count == 0) return;
            var est = gridEst.SelectedRows[0].Tag as Estudiante;
            if (est == null) return;

            _estSel = est;
            txtNombre.Text = est.Nombre;
            txtRut.Text = est.Rut;
            txtCorreo.Text = est.Correo;
            for (int i = 0; i < clbAsig.Items.Count; i++)
            {
                var a = (Asignatura)clbAsig.Items[i];
                clbAsig.SetItemChecked(i, a.IdEstudiantes.Contains(est.IdUsuario));
            }
        }

        private List<string> CodigosMarcados()
        {
            return clbAsig.CheckedItems.Cast<Asignatura>().Select(a => a.Codigo).ToList();
        }

        private void LimpiarFormEstudiante()
        {
            _estSel = null;
            txtNombre.Clear(); txtRut.Clear(); txtCorreo.Clear();
            for (int i = 0; i < clbAsig.Items.Count; i++) clbAsig.SetItemChecked(i, false);
            gridEst.ClearSelection();
        }

        private void AgregarEstudiante()
        {
            try
            {
                var nuevo = new Estudiante
                {
                    Nombre = txtNombre.Text, Rut = txtRut.Text, Correo = txtCorreo.Text,
                    FechaIngreso = DateTime.Today
                };
                nuevo.Agregar(_db, CodigosMarcados());
                Info("Estudiante agregado.\nContraseña inicial: " + Validador.CuerpoRut(nuevo.Rut));
                LimpiarFormEstudiante();
                CargarEstudiantes();
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        private void EditarEstudiante()
        {
            if (_estSel == null) { Error("Seleccione primero un estudiante de la lista."); return; }
            try
            {
                _estSel.EditarDatos(_db, txtNombre.Text, txtRut.Text, txtCorreo.Text, CodigosMarcados());
                Info("Datos actualizados.");
                LimpiarFormEstudiante();
                CargarEstudiantes();
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        private void EliminarEstudiante()
        {
            if (_estSel == null) { Error("Seleccione primero un estudiante de la lista."); return; }

            int cantNotas = _estSel.VerNotas(_db).Count;
            int cantAsistencias = _estSel.VerAsistencias(_db).Count;
            var respuesta = MessageBox.Show(this,
                "¿Eliminar a " + _estSel.Nombre + "?\n\nSe borrarán también sus " + cantNotas +
                " nota(s) y " + cantAsistencias + " registro(s) de asistencia.\nEsta acción no se puede deshacer.",
                "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (respuesta != DialogResult.Yes) return;

            try
            {
                _estSel.Eliminar(_db);
                Info("Estudiante eliminado.");
                LimpiarFormEstudiante();
                CargarEstudiantes();
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        // ================================================================= ASISTENCIA (RF03)

        private TabPage CrearTabAsistencia()
        {
            var tab = new TabPage("Asistencia");

            cboAsisAsig = CrearCombo(85, 12, 300);
            cboAsisAsig.SelectedIndexChanged += (s, e) => CargarAsistencia();
            dtpAsis = new DateTimePicker { Left = 455, Top = 12, Width = 120, Format = DateTimePickerFormat.Short, MaxDate = DateTime.Today.AddDays(1).AddSeconds(-1), Margin = new Padding(0, 4, 14, 0) };
            dtpAsis.ValueChanged += (s, e) => CargarAsistencia();

            var btnGuardar = new Button { Text = "Guardar asistencia", Left = 600, Top = 9, Width = 160, Height = 30 };
            btnGuardar.Click += (s, e) => GuardarAsistencia();

            lblAsisEstado = new Label { Dock = DockStyle.Top, Height = 26, Padding = new Padding(10, 4, 0, 0), ForeColor = Color.DimGray };

            gridAsis = CrearGrid();
            gridAsis.ReadOnly = false;
            gridAsis.Dock = DockStyle.Fill;
            gridAsis.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "presente", HeaderText = "Presente",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 80, MinimumWidth = 80
            });
            gridAsis.Columns.Add("nombre", "Estudiante");
            gridAsis.Columns.Add("rut", "RUT");
            gridAsis.Columns["nombre"].ReadOnly = true;
            gridAsis.Columns["rut"].ReadOnly = true;
            gridAsis.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (gridAsis.IsCurrentCellDirty) gridAsis.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            var barra = CrearBarra();
            barra.Controls.AddRange(new Control[]
            {
                Etiqueta("Asignatura:"), cboAsisAsig, Etiqueta("Fecha:"), dtpAsis, btnGuardar
            });
            // Orden de acople: Fill primero; luego el estado; la barra al final (queda arriba de todo).
            tab.Controls.Add(gridAsis);
            tab.Controls.Add(lblAsisEstado);
            tab.Controls.Add(barra);
            return tab;
        }

        private void CargarAsistencia()
        {
            if (_suspender > 0) return;
            gridAsis.Rows.Clear();
            var asig = cboAsisAsig.SelectedItem as Asignatura;
            if (asig == null) { lblAsisEstado.Text = "No tiene asignaturas asignadas."; return; }

            DateTime fecha = dtpAsis.Value.Date;
            var existentes = _db.Datos.Asistencias
                .Where(x => x.CodigoAsignatura == asig.Codigo && x.Fecha.Date == fecha).ToList();

            foreach (var est in asig.ObtenerEstudiantes(_db))
            {
                var reg = existentes.FirstOrDefault(x => x.IdEstudiante == est.IdUsuario);
                bool presente = reg == null || reg.Asistio;
                int i = gridAsis.Rows.Add(presente, est.Nombre, est.Rut);
                gridAsis.Rows[i].Tag = est;
            }

            lblAsisEstado.Text = existentes.Count > 0
                ? "Ya existe un registro para esta fecha; al guardar se actualizará."
                : "Sin registro para esta fecha (todos aparecen presentes por defecto; desmarque a los ausentes).";
        }

        private void GuardarAsistencia()
        {
            var asig = cboAsisAsig.SelectedItem as Asignatura;
            if (asig == null) { Error("Seleccione una asignatura."); return; }
            if (gridAsis.Rows.Count == 0) { Error("La asignatura no tiene estudiantes matriculados."); return; }

            try
            {
                gridAsis.EndEdit();
                DateTime fecha = dtpAsis.Value.Date;
                foreach (DataGridViewRow fila in gridAsis.Rows)
                {
                    var est = (Estudiante)fila.Tag;
                    bool presente = fila.Cells["presente"].Value is bool marcado && marcado;
                    ProfesorActual.RegistrarAsistencia(est, asig, fecha, presente, _db);
                }
                Info("Asistencia del " + fecha.ToString("dd-MM-yyyy") + " guardada.");
                CargarAsistencia();
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        // ================================================================= NOTAS (RF04, RF05)

        private TabPage CrearTabNotas()
        {
            var tab = new TabPage("Notas");

            cboNotasAsig = CrearCombo(85, 12, 250);
            cboNotasAsig.SelectedIndexChanged += (s, e) => AlCambiarAsignaturaNotas();
            cboNotasEst = CrearCombo(425, 12, 300);
            nudNota = new NumericUpDown
            {
                Left = 780, Top = 12, Width = 70, DecimalPlaces = 1,
                Minimum = 1.0m, Maximum = 7.0m, Increment = 0.1m, Value = 4.0m
            };

            var btnRegistrar = new Button { Text = "Registrar nota nueva", Left = 10, Top = 50, Width = 180, Height = 30 };
            btnRegistrar.Click += (s, e) => RegistrarNota();
            var btnModificar = new Button { Text = "Modificar nota seleccionada", Left = 200, Top = 50, Width = 210, Height = 30 };
            btnModificar.Click += (s, e) => ModificarNota();
            var ayuda = new Label
            {
                Text = "Escala 1,0 a 7,0. Seleccione una fila para ver o modificar su valor.",
                ForeColor = Color.DimGray, Left = 425, Top = 57, AutoSize = true
            };

            gridNotas = CrearGrid();
            gridNotas.Dock = DockStyle.Fill;
            gridNotas.Columns.Add("fecha", "Fecha");
            gridNotas.Columns.Add("estudiante", "Estudiante");
            gridNotas.Columns.Add("nota", "Nota");
            gridNotas.SelectionChanged += (s, e) => SeleccionarNota();

            nudNota.Margin = new Padding(0, 4, 14, 0);
            var barra = CrearBarra();
            barra.Controls.AddRange(new Control[]
            {
                Etiqueta("Asignatura:"), cboNotasAsig, Etiqueta("Estudiante:"), cboNotasEst,
                Etiqueta("Nota:"), nudNota, btnRegistrar, btnModificar, ayuda
            });
            ayuda.Margin = new Padding(8, 9, 3, 0);
            barra.SetFlowBreak(nudNota, true);

            tab.Controls.Add(gridNotas);
            tab.Controls.Add(barra);
            return tab;
        }

        private void AlCambiarAsignaturaNotas()
        {
            if (_suspender > 0) return;
            var asig = cboNotasAsig.SelectedItem as Asignatura;
            Rellenar(cboNotasEst, asig == null ? new List<object>() : asig.ObtenerEstudiantes(_db).Cast<object>());
            CargarNotas();
        }

        private void CargarNotas()
        {
            _suspender++;
            gridNotas.Rows.Clear();
            var asig = cboNotasAsig.SelectedItem as Asignatura;
            if (asig != null)
            {
                foreach (var nota in ProfesorActual.VerNotas(asig, _db))
                {
                    var est = _db.Datos.BuscarEstudiante(nota.IdEstudiante);
                    int i = gridNotas.Rows.Add(nota.Fecha.ToString("dd-MM-yyyy"),
                                               est == null ? "(eliminado)" : est.Nombre,
                                               nota.Valor.ToString("0.0"));
                    gridNotas.Rows[i].Tag = nota;
                }
            }
            gridNotas.ClearSelection();
            _suspender--;
        }

        private void SeleccionarNota()
        {
            if (_suspender > 0 || gridNotas.SelectedRows.Count == 0) return;
            var nota = gridNotas.SelectedRows[0].Tag as Nota;
            if (nota == null) return;

            nudNota.Value = (decimal)Math.Min(7.0, Math.Max(1.0, nota.Valor));
            foreach (var item in cboNotasEst.Items)
            {
                if (((Estudiante)item).IdUsuario == nota.IdEstudiante) { cboNotasEst.SelectedItem = item; break; }
            }
        }

        private void RegistrarNota()
        {
            var asig = cboNotasAsig.SelectedItem as Asignatura;
            var est = cboNotasEst.SelectedItem as Estudiante;
            if (asig == null || est == null) { Error("Seleccione una asignatura y un estudiante."); return; }
            try
            {
                ProfesorActual.RegistrarNota(est, asig, (double)nudNota.Value, _db);
                CargarNotas();
                Info("Nota registrada.");
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        private void ModificarNota()
        {
            if (gridNotas.SelectedRows.Count == 0) { Error("Seleccione la nota que desea modificar."); return; }
            var nota = gridNotas.SelectedRows[0].Tag as Nota;
            if (nota == null) return;
            try
            {
                ProfesorActual.ModificarNota(nota, (double)nudNota.Value, _db);
                CargarNotas();
                Info("Nota modificada.");
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        // ================================================================= CONSULTAS (RF06, RF07)

        private TabPage CrearTabConsultas()
        {
            var tab = new TabPage("Consultas");

            cboConEst = CrearCombo(85, 12, 300);
            cboConEst.SelectedIndexChanged += (s, e) => AlCambiarEstudianteConsulta();
            cboConAsig = CrearCombo(475, 12, 280);
            cboConAsig.SelectedIndexChanged += (s, e) => CargarConsulta();

            var barra = CrearBarra();
            barra.Controls.AddRange(new Control[]
            {
                Etiqueta("Estudiante:"), cboConEst, Etiqueta("Asignatura:"), cboConAsig
            });

            gridConNotas = CrearGrid();
            gridConNotas.Dock = DockStyle.Fill;
            gridConNotas.Columns.Add("fecha", "Fecha");
            gridConNotas.Columns.Add("nota", "Nota");

            gridConAsis = CrearGrid();
            gridConAsis.Dock = DockStyle.Fill;
            gridConAsis.Columns.Add("fecha", "Fecha");
            gridConAsis.Columns.Add("estado", "Asistencia");

            var fuenteTitulo = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            var lblNotasTit = new Label { Text = "Notas (RF06)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = fuenteTitulo };
            var lblAsisTit = new Label { Text = "Asistencias (RF06)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = fuenteTitulo };

            var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tabla.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tabla.Controls.Add(lblNotasTit, 0, 0);
            tabla.Controls.Add(lblAsisTit, 1, 0);
            tabla.Controls.Add(gridConNotas, 0, 1);
            tabla.Controls.Add(gridConAsis, 1, 1);

            lblResumen = new Label
            {
                Dock = DockStyle.Bottom, Height = 52, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(31, 56, 100)
            };

            tab.Controls.Add(tabla);
            tab.Controls.Add(lblResumen);
            tab.Controls.Add(barra);
            return tab;
        }

        private void PrepararConsultas()
        {
            if (_usuario is Profesor)
            {
                // Estudiantes matriculados en alguna asignatura que imparte el profesor.
                var ids = new HashSet<int>(AsignaturasDelUsuario().SelectMany(a => a.IdEstudiantes));
                var lista = _db.Datos.Estudiantes.Where(x => ids.Contains(x.IdUsuario)).OrderBy(x => x.Nombre);
                Rellenar(cboConEst, lista.Cast<object>());
                cboConEst.Enabled = true;
            }
            else
            {
                Rellenar(cboConEst, new object[] { _usuario });
                cboConEst.Enabled = false;
            }
            AlCambiarEstudianteConsulta();
        }

        private void AlCambiarEstudianteConsulta()
        {
            if (_suspender > 0) return;
            var est = cboConEst.SelectedItem as Estudiante;
            var asignaturas = est == null
                ? new List<Asignatura>()
                : AsignaturasDelUsuario().Where(a => a.IdEstudiantes.Contains(est.IdUsuario)).ToList();
            Rellenar(cboConAsig, asignaturas.Cast<object>());
            CargarConsulta();
        }

        private void CargarConsulta()
        {
            if (_suspender > 0) return;
            gridConNotas.Rows.Clear();
            gridConAsis.Rows.Clear();
            lblResumen.Text = "";

            var est = cboConEst.SelectedItem as Estudiante;
            var asig = cboConAsig.SelectedItem as Asignatura;
            if (est == null || asig == null) return;

            List<Nota> notas;
            List<Asistencia> asistencias;
            var prof = _usuario as Profesor;
            if (prof != null)
            {
                notas = prof.VerNotas(asig, _db).Where(x => x.IdEstudiante == est.IdUsuario).ToList();
                asistencias = prof.VerAsistencias(asig, _db).Where(x => x.IdEstudiante == est.IdUsuario).ToList();
            }
            else
            {
                notas = est.VerNotas(_db).Where(x => x.CodigoAsignatura == asig.Codigo).ToList();
                asistencias = est.VerAsistencias(_db).Where(x => x.CodigoAsignatura == asig.Codigo).ToList();
            }

            foreach (var n in notas)
                gridConNotas.Rows.Add(n.Fecha.ToString("dd-MM-yyyy"), n.Valor.ToString("0.0"));
            foreach (var a in asistencias)
                gridConAsis.Rows.Add(a.Fecha.ToString("dd-MM-yyyy"), a.Asistio ? "Presente" : "Ausente");

            // RF07: promedio por asignatura
            string textoPromedio;
            if (notas.Count == 0)
            {
                textoPromedio = "Promedio: sin notas registradas";
            }
            else
            {
                double promedio = est.CalcularPromedio(asig, _db);
                textoPromedio = "Promedio: " + promedio.ToString("0.0") + (promedio >= 4.0 ? "  (aprobando)" : "  (bajo 4,0)");
            }

            string textoAsistencia;
            if (asistencias.Count == 0)
            {
                textoAsistencia = "Asistencia: sin registros";
            }
            else
            {
                int presentes = asistencias.Count(x => x.Asistio);
                double pct = 100.0 * presentes / asistencias.Count;
                textoAsistencia = "Asistencia: " + pct.ToString("0") + "% (" + presentes + " de " + asistencias.Count + " clases)";
            }
            lblResumen.Text = textoPromedio + "     |     " + textoAsistencia;
        }
    }
}

using SistemaPOS.Data;
using SistemaPOS.Helpers;
using SistemaPOS.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaPOS.Forms.Admin
{
    public class UsuarioForm : Form
    {
        private DataGridView dgv;
        private TextBox txtNombre, txtPassword, txtCambiarPassword;
        private ComboBox cbRol;
        private Button btnAgregar, btnGuardar, btnEliminar, btnLimpiar;

        private AppDbContext db = new AppDbContext();
        private Usuario seleccionado;

        public UsuarioForm()
        {
            InicializarUI();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Cargar();
        }

        private void InicializarUI()
        {
            this.BackColor = Color.White;

            var layout = new TableLayoutPanel  { Dock = DockStyle.Fill, RowCount = 4 };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            // 🏷️ TÍTULO
            var lblTitulo = new Label
            {
                Text = "Gestión de Usuarios",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 45, 70),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };

            // 📊 TABLA
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AllowUserToAddRows = false, // Evita la fila vacía al final
                AllowUserToResizeRows = false, //EVITA QUE SE PUEDA CAMBIAR ALTO ARRASTRANDO FILA
                RowTemplate = { Height = 40 }
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 45, 70);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            // --- COLUMNA DE CONTEO (#) ---
            // Creamos una columna manual que no viene de la base de datos
            var colIndex = new DataGridViewTextBoxColumn { HeaderText = "#", ReadOnly = true, Width = 40 };
            dgv.Columns.Add(colIndex);

            // Evento para pintar el número de fila (1, 2, 3...)
            dgv.CellFormatting += (s, e) => {
                if (e.ColumnIndex == 0 && e.RowIndex >= 0)
                {
                    e.Value = (e.RowIndex + 1).ToString();
                }

                // Buscamos la columna por nombre "Password"
                if (dgv.Columns[e.ColumnIndex].Name == "Password" && e.Value != null)
                {
                    e.Value = new string('●', 8); // Dibuja 8 puntos negros
                }
            };

            dgv.MouseDown += (s, e) => {
                var hit = dgv.HitTest(e.X, e.Y);
                // Si el clic fue en el espacio en blanco (ninguna celda)
                if (hit.Type == DataGridViewHitTestType.None)
                {
                    dgv.ClearSelection();
                    Limpiar(); // Esto pone 'seleccionado = null' y habilita el botón Agregar
                }
            };

            //METODO PARA QUITAR LA SELECCION AUTOMATICA
            dgv.DataBindingComplete += (s, e) => {
                dgv.ClearSelection();
                dgv.CurrentCell = null;
            };


            // 🧾 INPUTS
            var panelInputs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(20, 10, 20, 10) };
            panelInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            panelInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));


            Font fuenteInputs = new Font("Segoe UI", 11);

            txtNombre = new TextBox { PlaceholderText = "Nombre", BorderStyle = BorderStyle.None, Dock = DockStyle.Fill, Font = fuenteInputs };
            Panel panelNombre = new Panel  { Height = 30,   BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Padding = new Padding(5, 6, 5, 0), Dock = DockStyle.Top };
            panelNombre.Controls.Add(txtNombre);

            // PASSWORD (NUEVO)
            txtPassword = new TextBox();
            var panelPasswordCustom = CrearInputPassword(txtPassword, "Contraseña");

            // CONFIRMAR PASSWORD (NUEVO)
            txtCambiarPassword = new TextBox();
            var panelConfirmarCustom = CrearInputPassword(txtCambiarPassword, "Confirmar Contraseña");

            // 🔽 DROPDOWN ROL
            cbRol = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Font = fuenteInputs };
            cbRol.Items.AddRange(new object[] { "ADMIN", "CAJERO" });

            // Agregar al layout (asegúrate de agregar los PANELES, no los txt directamente)
            panelInputs.Controls.Add(panelNombre, 0, 0);
            panelInputs.Controls.Add(panelPasswordCustom, 1, 0);
            panelInputs.Controls.Add(panelConfirmarCustom, 2, 0);
            panelInputs.Controls.Add(cbRol, 3, 0);

            // 🔘 BOTONES
            var panelBotones = new FlowLayoutPanel {  Dock = DockStyle.Fill,  FlowDirection = FlowDirection.RightToLeft,  Padding = new Padding(20)  };

            btnAgregar = new Button { Text = "Agregar", Width = 120, Height = 40 }; UIStyles.BotonPrimary(btnAgregar);
            btnGuardar = new Button { Text = "Actualizar", Width = 120, Height = 40 }; UIStyles.BotonNeutral(btnGuardar);
            btnEliminar = new Button { Text = "Eliminar", Width = 120, Height = 40 }; UIStyles.BotonDanger(btnEliminar);
            btnLimpiar = new Button { Text = "Limpiar", Width = 120, Height = 40 }; UIStyles.BotonSuccess(btnLimpiar);

            panelBotones.Controls.Add(btnLimpiar);
            panelBotones.Controls.Add(btnEliminar);
            panelBotones.Controls.Add(btnGuardar);
            panelBotones.Controls.Add(btnAgregar);

            layout.Controls.Add(lblTitulo, 0, 0);
            layout.Controls.Add(dgv, 0, 1);
            layout.Controls.Add(panelInputs, 0, 2);
            layout.Controls.Add(panelBotones, 0, 3);

            this.Controls.Add(layout);
            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) Limpiar();
            };

            btnAgregar.Click += Agregar;
            btnGuardar.Click += Guardar;
            btnEliminar.Click += Eliminar;
            btnLimpiar.Click += Limpiar;
            dgv.SelectionChanged += Seleccionar;
        }

        //============LOGICA DE NEGOCIO===========


        //FUNIONC PARA AGREGAR OJITOS A LOS TEXTOS DE CONTRASEÑA
        private Panel CrearInputPassword(TextBox txt, string placeholder)
        {
            Panel contenedor = new Panel
            {
                Height = 30,//Ajusta según tu diseño
                Dock = DockStyle.Top,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, // El borde lo da el panel
                Padding = new Padding(5, 6, 5, 0)
            };

            txt.BorderStyle = BorderStyle.None; // Quitamos el borde al textbox
            txt.Dock = DockStyle.Fill;
            txt.PlaceholderText = placeholder;
            txt.UseSystemPasswordChar = true;
            txt.Font = new Font("Segoe UI", 10);

            Button btnOjo = new Button
            {
                Text = "👁",
                Width = 25,
                Height = 18, // Altura menor para que no toque los bordes del panel
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8), // Fuente un poco más pequeña
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnOjo.FlatAppearance.BorderSize = 0;
            btnOjo.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnOjo.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnOjo.BackColor = Color.Transparent;

            btnOjo.Click += (s, e) => {
                txt.UseSystemPasswordChar = !txt.UseSystemPasswordChar;
            };

            contenedor.Controls.Add(txt);
            contenedor.Controls.Add(btnOjo);

            return contenedor;
        }

        //FUNCION PARA CARGAR INICIAR
        private void Cargar()
        {
            //ORDENAR POR NOMBRE
            dgv.DataSource = db.Usuarios.OrderBy(p => p.Nombre).ToList();

            // BLOQUEAR EDICION EN TODAS LAS COLUMNAS
            foreach (DataGridViewColumn col in dgv.Columns) {  col.ReadOnly = true; }

            // Ocultar el ID real de la base de datos
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;

            //VALIDACION PARA LA COLUMNA DE #
            if (dgv.Columns.Count > 0)
            {
                dgv.Columns[0].Width = 60;
                dgv.Columns[0].Resizable = DataGridViewTriState.False;
            }

            // COLUMNA ACTIVO
            if (dgv.Columns["Activo"] != null)
            {
                var colActivo = dgv.Columns["Activo"];
                colActivo.ReadOnly = false; // Habilitamos edición
                colActivo.Width = 100;      // Tamaño fijo
            }

            //COLUMNA DE PASSWORD
            if (dgv.Columns["CambiarPassword"] != null) dgv.Columns["CambiarPassword"].Visible = false;

            if (dgv.Columns["Password"] != null)
            {
                dgv.Columns["Password"].HeaderText = "Contraseña";
                dgv.Columns["Password"].Width = 120;
            }

            // EDICION DE COLUMNAS
            if (dgv.Columns["Rol"] != null) { dgv.Columns["Rol"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells; }
            if (dgv.Columns["Activo"] != null) { dgv.Columns["Activo"].Width = 100; }
            if (dgv.Columns["Nombre"] != null) { dgv.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }

            this.BeginInvoke(new Action(() =>
            {
                Limpiar();
            }));

        }


        //FUNCION PARA AGREGAR USUARIO
        private void Agregar(object sender, EventArgs e)
        {
            // 1. Validar coincidencia de passwords antes de crear el objeto
            if (txtPassword.Text != txtCambiarPassword.Text)
            {
                MessageBox.Show("Las contraseñas no coinciden");
                return;
            }

            // Creamos el objeto temporal para validar
            var nuevo = new Usuario
            {
                Nombre = txtNombre.Text.Trim().ToUpper(),
                Password = BCrypt.Net.BCrypt.HashPassword(txtPassword.Text),
                Rol = cbRol.Text
            };

            // Validamos contra DataAnnotations (Required, EmailAddress, etc)
            if (!ValidationHelper.EsValido(nuevo)) return;

            // Validaciones de Base de Datos (Duplicados)
            if (db.Usuarios.Any(p => p.Nombre == nuevo.Nombre))
            {
                MessageBox.Show("Este nombre de usuario ya está registrado.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Contraseña requerida");
                return;
            }


            db.Usuarios.Add(nuevo);
            db.SaveChanges();
            Cargar();
            Limpiar();
        }


        //FUNCION PARA ACTUALIZAR USUARIO
        private void Guardar(object sender, EventArgs e)
        {
            if (seleccionado == null) return;

            // --- 1. VALIDACIONES DE UI (Cosas que no tocan el objeto aún) ---
            if (db.Usuarios.Any(u => u.Nombre == txtNombre.Text && u.Id != seleccionado.Id))
            {
                MessageBox.Show("El nombre de usuario ya está en uso.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtPassword.Text) && txtPassword.Text != txtCambiarPassword.Text)
            {
                MessageBox.Show("Las contraseñas nuevas no coinciden.");
                return;
            }

            // --- 2. LA PREGUNTA CRUCIAL (Antes de modificar nada) ---
            var confirm = MessageBox.Show("¿Seguro que desea actualizar los datos?", "Confirmar", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes) return; // Si dice NO, salimos y el objeto sigue intacto.

            // --- 3. ACTUALIZACIÓN DEL OBJETO ---
            if (!string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                seleccionado.Password = BCrypt.Net.BCrypt.HashPassword(txtPassword.Text);
            }

            seleccionado.Nombre = txtNombre.Text.Trim();
            seleccionado.Rol = cbRol.Text;

            // --- 4. PERSISTENCIA ---
            if (ValidationHelper.EsValido(seleccionado))
            {
                db.SaveChanges();
                Cargar();
                Limpiar();
                MessageBox.Show("Usuario actualizado con éxito.");
            }
            else
            {
                // Si falló la validación del modelo, revertimos los cambios del objeto desde la DB
                db.Entry(seleccionado).Reload();
                MessageBox.Show("Datos inválidos, los cambios fueron revertidos.");
            }
        }


        //FUNCION PARA ELIMINAR USUARIO
        private void Eliminar(object sender, EventArgs e)
        {
            if (seleccionado == null) return;

            var confirm = MessageBox.Show("¿Seguro que desea eliminar?", "Confirmar", MessageBoxButtons.YesNo);
            if (confirm == DialogResult.Yes)
            {
                db.Usuarios.Remove(seleccionado);
                db.SaveChanges();
                Cargar();
                Limpiar();
            }
        }


        //FUNCION PARA SELEECINAR
        private void Seleccionar(object sender, EventArgs e)
        {
            // Si no hay filas seleccionadas, limpiamos y salimos
            if (dgv.SelectedRows.Count == 0)
            {
                // No llamamos a Limpiar() aquí para evitar bucles infinitos con SelectionChanged
                return;
            }

            seleccionado = dgv.CurrentRow.DataBoundItem as Usuario;

            if (seleccionado == null) return;

            txtNombre.Text = seleccionado.Nombre;
            cbRol.Text = seleccionado.Rol;
            txtPassword.Text = "";
            txtCambiarPassword.Text = "";

            btnGuardar.Enabled = true;
            btnEliminar.Enabled = true;
            btnAgregar.Enabled = false;
        }


        //FUNCNION PARA LIMPIAR CAMPOS DE TEXTO
        private void Limpiar(object sender = null, EventArgs e = null)
        {
            // 1. Limpiar Inputs
            txtNombre.Clear();
            txtPassword.Clear();
            txtCambiarPassword.Clear();
            cbRol.SelectedIndex = -1;

            // 2. ROMPER la conexión con el usuario seleccionado
            seleccionado = null;

            // 3. Resetear el estado visual del Grid (LA CLAVE)
            dgv.CurrentCell = null; // Quita el foco de la celda actual
            dgv.ClearSelection();   // Quita el azul de la fila

            // 4. Configurar botones
            btnGuardar.Enabled = false;
            btnEliminar.Enabled = false;
            btnAgregar.Enabled = true;

            // Opcional: Poner el foco en el nombre para empezar a escribir de una vez
            txtNombre.Focus();
        }
    }
}
using SistemaPOS.Data;
using SistemaPOS.Helpers;
using SistemaPOS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;


namespace SistemaPOS.Forms.Admin
{
    public class ProveedorForm : Form
    {
        private DataGridView dgv;   //Tabla DAta grid
        private TextBox txtNombre, txtTelefono, txtEmail; //Inputs de la UI
        private Button btnAgregar, btnGuardar, btnEliminar, btnLimpiar; //Botones de UI

        private AppDbContext db = new AppDbContext(); //conexion a DB
        private Proveedor seleccionado; //elemento seleccionado

        //El iniciador
        public ProveedorForm()
        {
            InicializarUI();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Limpiar();

            this.BeginInvoke(new Action(() =>
            {
                Cursor = Cursors.WaitCursor; // 🔥 empieza carga
                Cargar();
                Cursor = Cursors.Default; // 🔥 termina
            }));
        }

        //===========INTERFZA UI======================
        private void InicializarUI()
        {
            this.BackColor = Color.White;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4 };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // título
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // tabla
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // inputs
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // botones

            // 🏷️ TÍTULO
            var lblTitulo = new Label
            {
                Text = "Gestión de Proveedores",
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
            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == 0 && e.RowIndex >= 0)
                {
                    e.Value = (e.RowIndex + 1).ToString();
                }
            };

            //METODO PARA QUITAR LA SELECCION AUTOMATICA
            dgv.DataBindingComplete += (s, e) =>
            {
                dgv.ClearSelection();
                dgv.CurrentCell = null;
            };


            // 🧾 INPUTS
            var panelInputs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(20, 10, 20, 10) };
            panelInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            panelInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            panelInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

            txtNombre = new TextBox { PlaceholderText = "Nombre", Dock = DockStyle.Fill };
            txtTelefono = new TextBox { PlaceholderText = "Teléfono", Dock = DockStyle.Fill };
            txtEmail = new TextBox { PlaceholderText = "Email", Dock = DockStyle.Fill };

            panelInputs.Controls.Add(txtNombre, 0, 0);
            panelInputs.Controls.Add(txtTelefono, 1, 0);
            panelInputs.Controls.Add(txtEmail, 2, 0);

            // 🔘 BOTONES
            var panelBotones = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(20) };
            btnAgregar = new Button { Text = "Agregar", Width = 120, Height = 40 }; UIStyles.BotonPrimary(btnAgregar);
            btnGuardar = new Button { Text = "Actualizar", Width = 120, Height = 40 }; UIStyles.BotonNeutral(btnGuardar);
            btnEliminar = new Button { Text = "Eliminar", Width = 120, Height = 40 }; UIStyles.BotonDanger(btnEliminar);
            btnLimpiar = new Button { Text = "Limpiar", Width = 120, Height = 40 }; UIStyles.BotonSuccess(btnLimpiar);

            panelBotones.Controls.Add(btnLimpiar);
            panelBotones.Controls.Add(btnEliminar);
            panelBotones.Controls.Add(btnGuardar);
            panelBotones.Controls.Add(btnAgregar);

            // 🧩 ARMADO FINAL
            layout.Controls.Add(lblTitulo, 0, 0);
            layout.Controls.Add(dgv, 0, 1);
            layout.Controls.Add(panelInputs, 0, 2);
            layout.Controls.Add(panelBotones, 0, 3);

            this.Controls.Add(layout);
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Limpiar();
            };

            // Eventos
            btnAgregar.Click += Agregar;
            btnGuardar.Click += Guardar;
            btnEliminar.Click += Eliminar;
            btnLimpiar.Click += Limpiar;
            dgv.SelectionChanged += Seleccionar;
        }


        //================LOGICA DE NEGOCIO===============
        // FUNCION PARA CARGAR DESDE DB
        private void Cargar()
        {

            // ORDENAR POR NOMBRE
            dgv.DataSource = db.Proveedores.OrderBy(p => p.Nombre).ToList();

            // OCULTAR EL CAMPO ID
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;

            // BLOQUEAR EDICION EN TODAS LAS COLUMNAS
            foreach (DataGridViewColumn col in dgv.Columns) { col.ReadOnly = true; }

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

            // EDITAR DATOS DE LAS COLUMNAS
            if (dgv.Columns["Telefono"] != null) { dgv.Columns["Telefono"].Width = 100; }
            if (dgv.Columns["Email"] != null) { dgv.Columns["Email"].Width = 250; }
            if (dgv.Columns["Nombre"] != null) { dgv.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }

            // Opcional: Poner nombres bonitos a las cabeceras
            dgv.Columns["Nombre"].HeaderText = "Proveedor";
            dgv.Columns["Telefono"].HeaderText = "Teléfono";
            dgv.Columns["Email"].HeaderText = "Correo Electrónico";

            this.BeginInvoke(new Action(() =>
            {
                Limpiar();
            }));
        }

        // FUNICON PARA GUARDAR UN NUEVO PROVEEDOR
        private void Agregar(object sender, EventArgs e)
        {
            // Creamos el objeto temporal para validar
            var nuevo = new Proveedor
            {
                Nombre = txtNombre.Text.Trim().ToUpper(),
                Telefono = txtTelefono.Text.Trim(),
                Email = txtEmail.Text.Trim().ToUpper()
            };

            // Validamos contra DataAnnotations (Required, EmailAddress, etc)
            if (!ValidationHelper.EsValido(nuevo)) return;

            // Validaciones de Base de Datos (Duplicados)
            if (db.Proveedores.Any(p => p.Nombre == nuevo.Nombre))
            {
                MessageBox.Show("Este nombre del proveedor ya está registrado.");
                return;
            }

            db.Proveedores.Add(nuevo);
            db.SaveChanges();

            Cargar();
            Limpiar();
        }

        // FUNCION PARA ACTUALIZAR UN NUEVO PROVEEDOR
        private void Guardar(object sender, EventArgs e)
        {
            if (seleccionado == null)
            {
                MessageBox.Show("Por favor, seleccione un proveedor de la lista.");
                return;
            }

            string nombreNuevo = txtNombre.Text.Trim().ToUpper();
            string telefonoNuevo = txtTelefono.Text.Trim().ToUpper();
            string emailNuevo = txtEmail.Text.Trim().ToUpper();

            //VALIDAMOS NOMBRE SI YA EXISTE ANTES DE ACTUALIZAR
            bool nombreYaExiste = db.Proveedores.Any(p => p.Nombre == nombreNuevo && p.Id != seleccionado.Id);

            if (nombreYaExiste)
            {
                MessageBox.Show("Ya existe otro proveedor con este nombre. Intente con uno diferente.");
                return;
            }

            // PREGUNTAR ANTES DE GUARDAR
            var confirm = MessageBox.Show("¿Seguro que desea actualizar los datos?", "Confirmar", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes) return;


            // Actualizamos los datos del objeto que ya está "trackeado" por EF
            seleccionado.Nombre = nombreNuevo;
            seleccionado.Telefono = telefonoNuevo;
            seleccionado.Email = emailNuevo;

            // VALIDACION DE MODELO Y PERSISTENCIA ---
            if (ValidationHelper.EsValido(seleccionado))
            {
                db.SaveChanges();
                Cargar();
                Limpiar();
                MessageBox.Show("Categoría actualizada con éxito.");
            }
            else
            {
                // Si falló la validación del modelo, revertimos los cambios del objeto desde la DB
                db.Entry(seleccionado).Reload();
                MessageBox.Show("Datos inválidos, los cambios fueron revertidos.");
            }
        }

        // FUNIONC PARA ELIMINAR UN PROVEEDOR
        private void Eliminar(object sender, EventArgs e)
        {
            if (seleccionado == null) return;

            var confirm = MessageBox.Show("¿Seguro que desea eliminar?", "Confirmar", MessageBoxButtons.YesNo);
            if (confirm == DialogResult.Yes)
            {
                db.Proveedores.Remove(seleccionado);
                db.SaveChanges();
                Cargar();
                Limpiar();
            }
        }

        // FUNCION PARA SELECCIONAR UNA FILA
        private void Seleccionar(object sender, EventArgs e)
        {

            // SINO HAY SELECCION NO HACEMOS NADA
            if (dgv.CurrentRow == null || dgv.SelectedRows.Count == 0)
            {
                return;
            }

            seleccionado = dgv.CurrentRow.DataBoundItem as Proveedor;

            if (seleccionado != null)
            {
                txtNombre.Text = seleccionado.Nombre;
                txtTelefono.Text = seleccionado.Telefono;
                txtEmail.Text = seleccionado.Email;

                btnGuardar.Enabled = true;
                btnEliminar.Enabled = true;
                btnAgregar.Enabled = false;
            }
        }

        // FUNCION LIMPIAR CAMPOS Y VARIABLES
        private void Limpiar(object sender = null, EventArgs e = null)
        {
            //LIMPIAMOS INPUTS
            txtNombre.Clear();
            txtTelefono.Clear();
            txtEmail.Clear();
            seleccionado = null;

            // RESETEAR SELEECIONADO DEL GRID
            dgv.CurrentCell = null; // Quita el foco de la celda actual
            dgv.ClearSelection();   // Quita el azul de la fila

            // CONFIGURAMOS BOTONES
            btnGuardar.Enabled = false;
            btnEliminar.Enabled = false;
            btnAgregar.Enabled = true;

            // PONEMOS EL FOCO EN ESCRIBIR NOMBRE
            txtNombre.Focus();
        }

    }
}
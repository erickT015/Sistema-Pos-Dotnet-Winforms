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
    public class CategoriaForm : Form
    {
        private DataGridView dgv; //TABLA DE DATOS
        private TextBox txtNombre;  //IMPUT DE TEXTO
        private Button btnAgregar, btnGuardar, btnEliminar, btnLimpiar; //BOTONESS

        private AppDbContext db = new AppDbContext(); //CONEXION A DB
        private Categoria seleccionado; //ELEMENTO SELECCIONADO

        public CategoriaForm()
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

            var layout = new TableLayoutPanel {  Dock = DockStyle.Fill, RowCount = 4};

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // título
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // tabla
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // inputs
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // botones

            // 🏷️ TÍTULO
            var lblTitulo = new Label
            {
                Text = "Gestión de Categorías",
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
               // AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
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
                if (e.ColumnIndex == 0 && e.RowIndex >= 0)  {  e.Value = (e.RowIndex + 1).ToString(); }
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
            var panelInputs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(20, 10, 20, 10) };

            panelInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            txtNombre = new TextBox  { PlaceholderText = "Nombre de la categoría", Dock = DockStyle.Fill  };
            panelInputs.Controls.Add(txtNombre, 0, 0);

            // 🔘 BOTONES
            var panelBotones = new FlowLayoutPanel {  Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(20) };

            btnAgregar = new Button { Text = "Agregar", Width = 120, Height = 40 }; UIStyles.BotonPrimary(btnAgregar);
            btnGuardar = new Button { Text = "Actualizar", Width = 120, Height = 40 };  UIStyles.BotonNeutral(btnGuardar);
            btnEliminar = new Button { Text = "Eliminar", Width = 120, Height = 40 }; UIStyles.BotonDanger(btnEliminar);
            btnLimpiar = new Button { Text = "Limpiar", Width = 120, Height = 40 }; UIStyles.BotonSuccess(btnLimpiar);

            panelBotones.Controls.Add(btnLimpiar);
            panelBotones.Controls.Add(btnEliminar);
            panelBotones.Controls.Add(btnGuardar);
            panelBotones.Controls.Add(btnAgregar);


            // 🧩 ARMADO
            layout.Controls.Add(lblTitulo, 0, 0);
            layout.Controls.Add(dgv, 0, 1);
            layout.Controls.Add(panelInputs, 0, 2);
            layout.Controls.Add(panelBotones, 0, 3);

            this.Controls.Add(layout);
            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) Limpiar();
            };

            // Eventos
            btnAgregar.Click += Agregar;
            btnGuardar.Click += Guardar;
            btnEliminar.Click += Eliminar;
            btnLimpiar.Click += Limpiar;
            dgv.SelectionChanged += Seleccionar;
        }

        //========================LOGICA DE NEGOCIO


        //FUNCION PARA CARGAR DESDE DB
        private void Cargar()
        {
            dgv.DataSource = db.Categorias.OrderBy(p => p.Nombre).ToList();

            // OCULTAR EL CAMPO DE ID
            if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;

            // BLOQUEAR EDICION EN TODAS LAS COLUMNAS
            foreach (DataGridViewColumn col in dgv.Columns) { col.ReadOnly = true; }

            //VALIDACION PARA LA COLUMNA DE #
            if (dgv.Columns.Count > 0)
            {
                dgv.Columns[0].Width = 60;
                dgv.Columns[0].Resizable = DataGridViewTriState.False;
            }

            //VALIDACION COLUMNA ACTIVO
            if (dgv.Columns["Activo"] != null)
            {
                var colActivo = dgv.Columns["Activo"];
                colActivo.ReadOnly = false; // Habilitamos edición
                colActivo.Width = 100;      // Tamaño fijo
            }

            // COLUMNA NOMBRE CON EL RESTO DEL TAMAÑO DEL GRID
            if (dgv.Columns["Nombre"] != null) { dgv.Columns["Nombre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }

            this.BeginInvoke(new Action(() =>
            {
                Limpiar();
            }));
        }


        //FUNCION DE GUARDAR CATEGRIA
        private void Agregar(object sender, EventArgs e)
        {
            // Creamos el objeto temporal para validar
            var nuevo = new Categoria
            {
                Nombre = txtNombre.Text.Trim().ToUpper()
            }; 

            // Validamos contra DataAnnotations (Required, EmailAddress, etc)
            if (!ValidationHelper.EsValido(nuevo)) return;

            // Validaciones de Base de Datos (Duplicados)
            if (db.Categorias.Any(p => p.Nombre == nuevo.Nombre))
            {
                MessageBox.Show("Este nombre de la categoría ya está registrado.");
                return;
            }

            db.Categorias.Add(nuevo);
            db.SaveChanges();

            Cargar();
            Limpiar();
        }


        //FUNCION DE ACTUALIZAR CATEGRIA
        private void Guardar(object sender, EventArgs e)
        {
            if (seleccionado == null)
            {
                MessageBox.Show("Por favor, seleccione una categoría de la lista.");
                return;
            }

            // 1. Capturamos el nombre nuevo para comparar
            string nombreNuevo = txtNombre.Text.Trim().ToUpper();

            // 2. VALIDACIÓN DE DUPLICADO , Buscamos si hay OTRO registro (Id diferente) que ya tenga ese nombre
            bool nombreYaExiste = db.Categorias.Any(p => p.Nombre == nombreNuevo && p.Id != seleccionado.Id);

            if (nombreYaExiste)
            {
                MessageBox.Show("Ya existe otra categoría con este nombre. Intente con uno diferente.");
                return;
            }

            // -PREGUNATR CONFIRMACION ANTES DE GUARDAR
            var confirm = MessageBox.Show("¿Seguro que desea actualizar los datos?", "Confirmar", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes) return;


            seleccionado.Nombre = nombreNuevo;

            // --- 4. VALIDACION DE MODELO Y PERSISTENCIA ---
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


        //FUNCION PARA ELIMINAR CATEGORIA
        private void Eliminar(object sender, EventArgs e)
        {
            if (seleccionado == null) return;

            var confirm = MessageBox.Show("¿Seguro que desea eliminar?", "Confirmar", MessageBoxButtons.YesNo);
            if (confirm == DialogResult.Yes)
            {
                db.Categorias.Remove(seleccionado);
                db.SaveChanges();
                Cargar();
                Limpiar();
            }
        }


        //FUNCION PARA SELECCIONAR COLUMNA
        private void Seleccionar(object sender, EventArgs e)
        {
            // Si el Grid se limpia o no hay filas, salimos sin hacer nada
            if (dgv.CurrentRow == null || dgv.SelectedRows.Count == 0)
            {
                return;
            }

            seleccionado = dgv.CurrentRow.DataBoundItem as Categoria;

            if (seleccionado != null)
            {
                txtNombre.Text = seleccionado.Nombre;
                btnGuardar.Enabled = true;
                btnEliminar.Enabled = true;
                btnAgregar.Enabled = false;
            }
        }


        //FUNCINO PARA LIMPIAR LOS CAMPOS
        private void Limpiar(object sender = null, EventArgs e = null)
        {
            //LIMPIAMOS INPUTS
            txtNombre.Clear();
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

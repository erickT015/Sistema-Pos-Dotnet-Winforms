using SistemaPOS.Data;
using SistemaPOS.Helpers;
using SistemaPOS.Models;
using SistemaPOS.Models.DTOs.Inventario;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;
using System.IO;

namespace SistemaPOS.Forms
{
    public class InventarioForm : Form
    {
        private DataGridView dgv; // 🔥 Aquí se pinta la tabla (grid principal)
        private TextBox txtScan;  // 🔥 Input para escaneo rápido
        private Button btnGuardar, btnModo, btnExportar, btnImportar, btnEditar;

        private AppDbContext db = new AppDbContext(); // 🔥 conexión a BD

        private bool modoEdicion = false;        // 🔥 modo inventario ON/OFF
        private bool esAdmin = true;             // 🔥 permisos
        private bool modoEdicionManual = false;  // 🔥 edición manual activa

        private string columnaOrdenada = "";     // 🔥 qué columna ordeno
        private bool ordenAscendente = true;     // 🔥 dirección orden

        private int paginaActual = 1;            // 🔥 paginación
        private int tamanioPagina = 500;
        private int totalPaginas = 1;
        private Label lblPagina;

        private TextBox txtBuscar;               // 🔥 buscador
        private Button btnBuscar;

        private List<InventarioItemDTO> cacheProductos = new(); // 🔥 TODO cargado en memoria
        private List<InventarioItemDTO> vistaActual = new();    // 🔥 lo que se muestra

        private HashSet<int> filasEditadas = new(); // 🔥 filas modificadas
        private Stack<(int row, string col, object oldValue)> undoStack = new(); // 🔥 CTRL+Z

        public InventarioForm()
        {
            InicializarUI();        // 🔥 Construye toda la pantalla (header, grid, botones)
            ConfigurarGrid();       // 🔥 Define columnas del dgv
            CargarCache();          // 🔥 Trae datos de BD → cacheProductos
            ConfigurarVirtualMode();// 🔥 optimiza rendimiento
            CargarVista();          // 🔥 llena el grid (vistaActual)
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ActivarModo(false);
        }

        // ================= UI =================
        private void InicializarUI()
        {
            this.BackColor = Color.White;
            this.Text = "Gestión de Inventario - Minisuper Hayda";
            this.Size = new Size(1100, 700);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

           
            // ================= HEADER =================
            var panelTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(240, 240, 240)
            };

            //  INPUT DE ESCANEO RÁPIDO
            txtScan = new TextBox
            {
                PlaceholderText = "Escanear producto...",
                Width = 200,
                Font = new Font("Segoe UI", 10),
                Enabled = false
            };

            // Evento para detectar Enter y procesar el código escaneado
            txtScan.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    Escanear(txtScan.Text.Trim());
                    txtScan.Clear();
                }
            };

            // BOTON PARA ACTIVAR O DESACTIVAR MODO INVENTARIO
            btnModo = new Button { Text = "Iniciar Inventario", Width = 150, Height = 35, FlatStyle = FlatStyle.Flat };
            btnModo.Click += (s, e) => ActivarModo(!modoEdicion);

            // BOTON PARA INVERTIR ORDEN DE LA COLUMNA ORDENADA
            var btnInvertir = new Button { Text = "⇅ Orden", Width = 80, Height = 35, FlatStyle = FlatStyle.Flat };
            btnInvertir.Click += (s, e) =>
            {
                ordenAscendente = !ordenAscendente;
                OrdenarLista();
            };

            //BOTON PARA EXPORTAR EXCEL
            btnExportar = new Button
            {
                Text = "Exportar",
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(33, 115, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnExportar.Click += (s, e) => ExportarExcel();

            //BOTON PAARA IMPORTAR EXCEL
            btnImportar = new Button
            {
                Text = "Importar",
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(33, 115, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnImportar.Click += (s, e) => ImportarExcel();

            // INPUT DE BUSQUEDA POR NOMBRE
            txtBuscar = new TextBox
            {
                PlaceholderText = "Buscar producto...",
                Width = 200,
                Font = new Font("Segoe UI", 10)
            };

            //BOTON PARA BUSCAR POR NOMBRE
            btnBuscar = new Button
            {
                Text = "Buscar",
                Width = 80,
                Height = 35,
                FlatStyle = FlatStyle.Flat
            };

            // EVENTO PARA BUSCAR AL HACER CLICK O ENTER
            btnBuscar.Click += (s, e) =>
            {
                paginaActual = 1;
                CargarVista();
            };

            // BOTON DE MODO EDICIÓN MANUAL EN GRID PRINCIPAL
            btnEditar = new Button
            {
                Text = "Editar",
                Width = 100,
                Height = 35,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = true //USE COLOR ESTANDAR DE WINDOWS EL GRIS
            };

            // CLICK PARA ACTIVAR/DESACTIVAR MODO EDICIÓN MANUAL
            btnEditar.Click += (s, e) => ToggleEdicion();

            // EVENTO  EN EL INPUT DE BUSQUEDA PARA DETECTAR ENTER
            txtBuscar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    paginaActual = 1;
                    CargarVista();
                }
            };

            //DEFINIMOS EL ORDEN DE LOS INPUTS Y BOTONES EN EL PANEL TOP
            panelTop.Controls.Add(new Label { Text = "Scanner:", AutoSize = true, Margin = new Padding(0, 8, 0, 0) });
            panelTop.Controls.Add(txtScan);
            panelTop.Controls.Add(btnModo);
            panelTop.Controls.Add(btnInvertir);
            panelTop.Controls.Add(new Label { Text = "|", AutoSize = true, Margin = new Padding(5, 8, 5, 0) });
            panelTop.Controls.Add(btnExportar);
            panelTop.Controls.Add(btnImportar);
            panelTop.Controls.Add(txtBuscar);
            panelTop.Controls.Add(btnBuscar);
            panelTop.Controls.Add(btnEditar);


            // ================= GRID =================
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // BLOQUEAR ENTER PARA NO CREAR NUEVA FILA (PORQUE ESTAMOS EN MODO VIRTUAL)
            dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    e.Handled = true;
            };

            // MANEJO DE ERRORES DE DATAGRIDVIEW (POR EJEMPLO, SI EL USUARIO ESCRIBE TEXTO EN UN CAMPO NUMÉRICO)
            dgv.DataError += (s, e) => { e.ThrowException = false; };

            // DETETCTA SI HUBO CAMBIOS EN LA CELDA PARA MARCARLA COMO EDITADA Y CAMBIARLE EL COLOR
            dgv.CellValueChanged += (s, e) =>
            {
                if (dgv.Columns[e.ColumnIndex].DataPropertyName == "Ajuste")
                {
                    dgv.InvalidateRow(e.RowIndex);
                }
            };

            //  EVENTO PARA QUE SI CAMBIO DE CELDA GUARDE INMEDIATO EL VALOR ANTERIOR Y MARQUE LA FILA COMO EDITADA
            dgv.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgv.IsCurrentCellDirty)
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            // DETECTA SI UNA FILA ESTÁ EN LA LISTA DE FILAS EDITADAS Y LE CAMBIA EL COLOR DE FONDO
            dgv.RowPrePaint += (s, e) =>
            {
                if (filasEditadas.Contains(e.RowIndex))
                {
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
                }
            };

            // AL TERMINAR DE EDITAR UNA CELDA, LA MARCA COMO EDITADA (POR SI EL USUARIO NO CAMBIA DE CELDA Y NO SE DISPARA EL CELLVALUEPUSHED)
            dgv.CellEndEdit += (s, e) =>
            {
                if (modoEdicionManual)
                {
                    filasEditadas.Add(e.RowIndex);
                    dgv.InvalidateRow(e.RowIndex);
                }
            };

            // EVENTO PARA PERMITIR REVERTIR EN LA ULTIMA CELDA EDITADA CON CTRL+Z (DESHACER)
            dgv.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Z)
                {
                    Undo();
                }
            };


            dgv.KeyDown += Dgv_KeyDown;

            // ================= BOTONES =================


            //BOTON PARA GUARDAR CAMBIOS EN MODO INVENTARIO 
            btnGuardar = new Button
            {
                Text = "Guardar Cambios",
                Width = 160,
                Height = 35,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnGuardar.Click += (s, e) => Guardar();


            // PANEL INFERIOR PARA PAGINACIÓN Y GUARDAR
            var panelBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3
            };

            panelBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            panelBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            panelBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

            var panelPaginacion = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Anchor = AnchorStyles.None,
                AutoSize = true
            };

            lblPagina = new Label { AutoSize = true };

            /*
            btnAnterior = new Button { Text = "◀", Width = 50 };
            btnSiguiente = new Button { Text = "▶", Width = 50 };
            lblPagina = new Label { AutoSize = true };

            btnAnterior.Click += (s, e) =>
            {
                if (paginaActual > 1)
                {
                    paginaActual--;
                    CargarVista();
                }
            };

            btnSiguiente.Click += (s, e) =>
            {
                if (paginaActual < totalPaginas)
                {
                    paginaActual++;
                    CargarVista();
                }
            };
            

            panelPaginacion.Controls.Add(btnAnterior);
            panelPaginacion.Controls.Add(lblPagina);
            panelPaginacion.Controls.Add(btnSiguiente);
            */
            panelPaginacion.Controls.Add(lblPagina);

            var panelDerecha = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill
            };

            panelDerecha.Controls.Add(btnGuardar);

            panelBottom.Controls.Add(new Panel(), 0, 0);
            panelBottom.Controls.Add(panelPaginacion, 1, 0);
            panelBottom.Controls.Add(panelDerecha, 2, 0);

            // 🔥 IMPORTANTE (TE FALTABA)
            layout.Controls.Add(panelTop, 0, 0);
            layout.Controls.Add(dgv, 0, 1);
            layout.Controls.Add(panelBottom, 0, 2);

            this.Controls.Add(layout);
        }

        // ================= LOGICA =================
        private void ActivarModo(bool activo)
        {
            modoEdicion = activo;
            txtScan.Enabled = activo;
            dgv.ReadOnly = !activo;

            btnModo.Text = activo ? "Cancelar Inventario" : "Iniciar Inventario";
            btnModo.BackColor = activo ? Color.IndianRed : Color.LightGray;
            btnModo.ForeColor = activo ? Color.White : Color.Black;

            btnGuardar.Enabled = activo;
            btnImportar.Enabled = !activo;
            btnExportar.Enabled = !activo;
            btnEditar.Enabled = !activo;

            if (activo)
            {
                vistaActual.Clear();
                dgv.RowCount = 0;
            }
            else
            {
                CargarVista();
            }
        }

        // FUNCION PARA ESCANEAR CODIGOS
        private void Escanear(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return;

            var itemExistente = vistaActual.FirstOrDefault(x => x.Codigo == codigo);

            if (itemExistente != null)
            {
                itemExistente.Ajuste += 1;
                dgv.Refresh();
                return;
            }

            var producto = db.Productos.FirstOrDefault(p => p.Codigo == codigo);

            InventarioItemDTO nuevoItem;

            if (producto != null)
            {
                nuevoItem = new InventarioItemDTO
                {
                    Id = producto.Id,
                    Codigo = producto.Codigo,
                    Nombre = producto.Nombre,
                    PrecioCompra = producto.PrecioCompra,
                    PrecioVenta = producto.PrecioVenta,
                    StockActual = producto.Cantidad,
                    CategoriaId = producto.CategoriaId,
                    ProveedorId = producto.ProveedorId,
                    TipoVentaId = producto.TipoVentaId,
                    Ajuste = 0,
                    EsNuevo = false
                };
            }
            else
            {
                if (!esAdmin) { MessageBox.Show("No existe."); return; }

                nuevoItem = new InventarioItemDTO
                {
                    Codigo = codigo,
                    Nombre = "NUEVO PRODUCTO",
                    Ajuste = 0,
                    StockActual = 0,
                    EsNuevo = true,
                    TipoVentaId = 1,
                    CategoriaId = db.Categorias.Select(c => c.Id).FirstOrDefault(),
                    ProveedorId = db.Proveedores.Select(p => p.Id).FirstOrDefault()
                };
            }

            vistaActual.Add(nuevoItem);
            dgv.RowCount = vistaActual.Count;
            dgv.Refresh();
        }

        // FUNCION PARA GUARDAR O ACTUALIZAR 
        private void Guardar()
        {
            // Forzamos el fin de edición por si el usuario sigue posicionado en una celda
            dgv.EndEdit();

            if (filasEditadas.Count == 0)
            {
                MessageBox.Show("No hay cambios detectados.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Usamos Distinct para no procesar la misma fila varias veces
                var indicesParaProcesar = filasEditadas.Distinct().ToList();

                foreach (var rowIndex in indicesParaProcesar)
                {
                    if (rowIndex < 0 || rowIndex >= vistaActual.Count) continue;

                    var item = vistaActual[rowIndex];

                    // VALIDACIÓN CRÍTICA: Si falla, el Helper muestra el mensaje y salimos
                    if (!ValidationHelper.EsValido(item)) return;

                    var producto = db.Productos.FirstOrDefault(p => p.Codigo == item.Codigo);

                    if (producto != null)
                    {
                        // ACTUALIZAR EXISTENTE
                        producto.Nombre = item.Nombre.Trim().ToUpper();
                        producto.PrecioCompra = item.PrecioCompra;
                        producto.PrecioVenta = item.PrecioVenta;
                        producto.Cantidad = item.Total; // Total = StockActual + Ajuste
                        producto.CategoriaId = item.CategoriaId;
                        producto.ProveedorId = item.ProveedorId;
                        producto.TipoVentaId = item.TipoVentaId;
                        producto.Activo = item.Activo;
                    }
                    else if (item.EsNuevo)
                    {
                        // INSERTAR NUEVO
                        var nuevoProd = new Producto
                        {
                            Codigo = item.Codigo,
                            Nombre = item.Nombre.Trim().ToUpper(),
                            PrecioCompra = item.PrecioCompra,
                            PrecioVenta = item.PrecioVenta,
                            Cantidad = item.Total,
                            CategoriaId = item.CategoriaId,
                            ProveedorId = item.ProveedorId,
                            TipoVentaId = item.TipoVentaId,
                            Activo = true
                        };
                        db.Productos.Add(nuevoProd);
                    }
                }

                db.SaveChanges();
                MessageBox.Show("¡Datos guardados con éxito!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // --- EL RESET DE FÁBRICA ---
                LimpiarGridTotalmente();

                // Limpieza de estados
                undoStack.Clear();

                // Recargar para sincronizar IDs de nuevos productos y limpiar colores
                CargarCache();
                CargarVista();
                filasEditadas.Clear();
                dgv.Refresh();

                ActivarModo(!modoEdicion);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // FUNCION PRINCIPAL DE CARGA
        private void CargarVista()
        {
            IEnumerable<InventarioItemDTO> query = cacheProductos;

            if (!string.IsNullOrWhiteSpace(txtBuscar.Text))
            {
                string filtro = txtBuscar.Text.Trim().ToLower();
                query = query.Where(p => p.Nombre.ToLower().Contains(filtro));
            }

            vistaActual = query
                .OrderBy(p => p.Nombre)
                .ToList();

            dgv.RowCount = vistaActual.Count;

            lblPagina.Text = $"Registros: {vistaActual.Count}";
        }

        //FUNCION PARA CONFIGURAR EL GRID Y DEFINIR ESTILOS
        private void ConfigurarGrid()
        {
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Clear();

            // 1. Columnas de Texto básicas
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", HeaderText = "Id", Visible = false, Width = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", DataPropertyName = "Codigo", HeaderText = "Código", Width = 150, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", DataPropertyName = "Nombre", HeaderText = "Producto", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });


            // 2. Dropdown QUEMADO (Tipo de Venta)
            // Usamos una lista de objetos anónimos definida explícitamente
            var tiposVenta = new[] {
        new { Id = 1, Nombre = "Unidad" },
        new { Id = 2, Nombre = "Peso (Kg)" },
        new { Id = 3, Nombre = "Líquido (Litro)" }
    }.ToList();

            var colTipo = new DataGridViewComboBoxColumn
            {
                HeaderText = "Tipo Venta",
                DataPropertyName = "TipoVentaId",
                DataSource = tiposVenta,
                DisplayMember = "Nombre",
                ValueMember = "Id",
                Width = 110,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox // Lo hace ver como combo siempre
            };
            dgv.Columns.Add(colTipo);

            // 3. Dropdown Dinámico (Categorías)
            dgv.Columns.Add(new DataGridViewComboBoxColumn
            {
                HeaderText = "Categoría",
                DataPropertyName = "CategoriaId",
                DataSource = db.Categorias.ToList(),
                DisplayMember = "Nombre",
                ValueMember = "Id",
                Width = 120,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            });

            // 4. Dropdown Dinámico (Proveedores)
            dgv.Columns.Add(new DataGridViewComboBoxColumn
            {
                HeaderText = "Proveedor",
                DataPropertyName = "ProveedorId",
                DataSource = db.Proveedores.ToList(),
                DisplayMember = "Nombre",
                ValueMember = "Id",
                Width = 250,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            });

            // 5. Precios
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PrecioCompra", HeaderText = "P. Compra", Width = 90 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PrecioVenta", HeaderText = "P. Venta", Width = 80 });

            // 6. Inventario (Lectura y Ajuste)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StockActual", HeaderText = "Stock", Width = 60, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(240, 240, 240) } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Ajuste", HeaderText = "Ajuste", Width = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Total", HeaderText = "Total", Width = 60, ReadOnly = true });

            dgv.ColumnHeaderMouseClick += (s, e) => {
                string nombreColumna = dgv.Columns[e.ColumnIndex].DataPropertyName;

                // Si clickea la misma columna, invierte el orden. Si es otra, pone Ascendente.
                if (columnaOrdenada == nombreColumna)
                    ordenAscendente = !ordenAscendente;
                else
                    ordenAscendente = true;

                columnaOrdenada = nombreColumna;
                OrdenarLista();
            };

        }

        //FUNCION PARA ORDENAR LISTAS TIPO FILTRO
        private void OrdenarLista()
        {
            if (string.IsNullOrEmpty(columnaOrdenada) || vistaActual.Count == 0) return;

            if (ordenAscendente)
                vistaActual = vistaActual.OrderBy(x => GetPropertyValue(x, columnaOrdenada)).ToList();
            else
                vistaActual = vistaActual.OrderByDescending(x => GetPropertyValue(x, columnaOrdenada)).ToList();

            dgv.Refresh();
        }

        // Función auxiliar para leer la propiedad por nombre (Reflection)
        private object GetPropertyValue(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);
        }


        private void ConfigurarVirtualMode()
        {
            dgv.VirtualMode = true;
            dgv.ReadOnly = false;

            dgv.CellValueNeeded += Dgv_CellValueNeeded;
            dgv.CellValuePushed += Dgv_CellValuePushed; // 🔥 ESTE ES EL IMPORTANTE
        }

        private void ToggleEdicion()
        {
            // Caso: Estamos en edición y el usuario quiere salir (presionó "Guardar/Editar" de nuevo)
            if (modoEdicionManual && filasEditadas.Count > 0)
            {
                var res = MessageBox.Show("Tiene cambios sin guardar. ¿Desea aplicarlos ahora?",
                                         "Cambios Pendientes",
                                         MessageBoxButtons.YesNoCancel,
                                         MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    Guardar();
                    // --- EL RESET DE FÁBRICA ---
                    LimpiarGridTotalmente();
                    // --- EL RESET DE FÁBRICA ---
                    // LimpiarGridTotalmente();
                    // Si después de intentar guardar siguen habiendo filas editadas (porque falló validación)
                    // No cerramos el modo edición.
                    if (filasEditadas.Count > 0) return;
                }
                else if (res == DialogResult.No)
                {
                    // DESCARTAR CAMBIOS: Limpiamos todo y recargamos de la caché/DB
                    filasEditadas.Clear();
                    undoStack.Clear();
                    CargarVista();
                    MessageBox.Show("Cambios descartados.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else // DialogResult.Cancel
                {
                    return; // No hacemos nada, el usuario se queda editando
                }
            }

            // Cambiar estado lógico
            modoEdicionManual = !modoEdicionManual;

            if (!modoEdicionManual)
            {
                filasEditadas.Clear();
                dgv.Refresh();
            }

            // Configurar UI
            dgv.ReadOnly = !modoEdicionManual;
            btnExportar.Enabled = !modoEdicionManual;
            btnImportar.Enabled = !modoEdicionManual;
            btnModo.Enabled = !modoEdicionManual;

            if (modoEdicionManual)
            {
                btnEditar.Text = "Guardar";
                btnEditar.BackColor = Color.LightGreen; // Tu color verde
            }
            else
            {
                btnEditar.Text = "Editar";
                // Esto elimina el color definido y devuelve el gris por defecto de Windows
                btnEditar.BackColor = default;
                btnEditar.UseVisualStyleBackColor = true;
            }

            // btnEditar.Text = modoEdicionManual ? "Finalizar" : "Editar";

            // Feedback visual opcional
            //   btnEditar.BackColor = modoEdicionManual ? Color.FromArgb(40, 167, 69) : Color.FromArgb(18, 18, 18);




            if (filasEditadas.Count == 0)
            {
                string mensaje = modoEdicionManual ? "Modo edición activado." : "Modo edición desactivado.";
                // Puedes usar un label de estado en lugar de un MessageBox para no interrumpir
                // lblStatus.Text = mensaje; 
            }
        }

        //FUNCION PARA EXPORTAR PRODUCTOS
        private void ExportarExcel()
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Productos");
                    var datos = db.Productos.ToList();

                    string[] headers = { "Id", "Codigo", "Nombre", "PrecioCompra", "PrecioVenta", "Cantidad", "CantidadMinima", "TipoVentaId", "CategoriaId", "ProveedorId", "Activo" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cell(1, i + 1).Value = headers[i];
                        ws.Cell(1, i + 1).Style.Font.Bold = true;
                    }

                    int f = 2;
                    foreach (var p in datos)
                    {
                        ws.Cell(f, 1).Value = p.Id;
                        ws.Cell(f, 2).Value = p.Codigo;
                        ws.Cell(f, 3).Value = p.Nombre;
                        ws.Cell(f, 4).Value = p.PrecioCompra;
                        ws.Cell(f, 5).Value = p.PrecioVenta;
                        ws.Cell(f, 6).Value = p.Cantidad;
                        ws.Cell(f, 7).Value = p.CantidadMinima;
                        ws.Cell(f, 8).Value = p.TipoVentaId;
                        ws.Cell(f, 9).Value = p.CategoriaId;
                        ws.Cell(f, 10).Value = p.ProveedorId;
                        ws.Cell(f, 11).Value = p.Activo ? 1 : 0;
                        f++;
                    }
                    ws.Columns().AdjustToContents();

                    SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "Inventario_Hayda" };
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        workbook.SaveAs(sfd.FileName);
                        MessageBox.Show("Archivo exportado.");
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        //FUNCION PARA IMPORTAR PRODUCTOS
        private void ImportarExcel()
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Workbook|*.xlsx" };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    using (var wb = new XLWorkbook(ofd.FileName))
                    {
                        var ws = wb.Worksheet(1);
                        var filas = ws.RangeUsed().RowsUsed().Skip(1);

                        foreach (var f in filas)
                        {
                            string cod = f.Cell(2).GetValue<string>()?.Trim();
                            if (string.IsNullOrEmpty(cod)) continue;

                            // --- FUNCIÓN DE LIMPIEZA PARA NÚMEROS ---
                            // Esto quita espacios de miles y símbolos de moneda aunque Excel jure que es "número"
                            Func<int, decimal> LeerDecimal = (col) => {
                                string raw = f.Cell(col).GetValue<string>()
                                    .Replace(" ", "")       // Quita espacios normales
                                    .Replace("\u00A0", "")  // Quita espacios de formato Excel (non-breaking space)
                                    .Replace("₡", "")       // Por si se cola el símbolo de colón
                                    .Trim();

                                decimal.TryParse(raw, out decimal resultado);
                                return resultado;
                            };

                            var p = db.Productos.FirstOrDefault(x => x.Codigo == cod);

                            if (p == null) // Producto Nuevo
                            {
                                db.Productos.Add(new Producto
                                {
                                    Codigo = cod,
                                    Nombre = f.Cell(3).GetValue<string>(),
                                    PrecioCompra = LeerDecimal(4), // <--- Usa la limpieza
                                    PrecioVenta = LeerDecimal(5),  // <--- Usa la limpieza
                                    Cantidad = f.Cell(6).GetValue<int>(),
                                    CantidadMinima = f.Cell(7).GetValue<int>(),
                                    TipoVentaId = f.Cell(8).GetValue<int>() == 0 ? 1 : f.Cell(8).GetValue<int>(),
                                    CategoriaId = f.Cell(9).GetValue<int>() == 0 ? 1 : f.Cell(9).GetValue<int>(),
                                    ProveedorId = f.Cell(10).GetValue<int>() == 0 ? 1 : f.Cell(10).GetValue<int>(),
                                    Activo = f.Cell(11).GetValue<int>() == 1
                                });
                            }
                            else // Actualizar Existente
                            {
                                p.Nombre = f.Cell(3).GetValue<string>();
                                p.PrecioCompra = LeerDecimal(4);
                                p.PrecioVenta = LeerDecimal(5);
                                p.Cantidad = f.Cell(6).GetValue<int>();
                                p.Activo = f.Cell(11).GetValue<int>() == 1;
                            }
                        }

                        db.SaveChanges();
                        trans.Commit();
                        CargarVista();
                        MessageBox.Show("¡Importación exitosa! Se limpiaron los formatos de número automáticamente.", "Minisuper Hayda");
                    }
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    MessageBox.Show("Error crítico: " + ex.Message);
                }
            }
        }

        private void CargarCache()
        {
            cacheProductos = db.Productos
                .Select(p => new InventarioItemDTO
                {
                    Id = p.Id,
                    Codigo = p.Codigo,
                    Nombre = p.Nombre,
                    PrecioCompra = p.PrecioCompra,
                    PrecioVenta = p.PrecioVenta,
                    StockActual = p.Cantidad,
                    CantidadMinima = p.CantidadMinima,
                    CategoriaId = p.CategoriaId,
                    ProveedorId = p.ProveedorId,
                    TipoVentaId = p.TipoVentaId,
                    Activo = p.Activo,
                    Ajuste = 0,
                    EsNuevo = false
                })
                .ToList();
        }

        private void Dgv_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= vistaActual.Count) return;

            var item = vistaActual[e.RowIndex];
            var col = dgv.Columns[e.ColumnIndex].DataPropertyName;

            switch (col)
            {
                case "Codigo": e.Value = item.Codigo; break;
                case "Nombre": e.Value = item.Nombre; break;
                case "PrecioCompra": e.Value = item.PrecioCompra; break;
                case "PrecioVenta": e.Value = item.PrecioVenta; break;
                case "StockActual": e.Value = item.StockActual; break;
                case "Ajuste": e.Value = item.Ajuste; break;
                case "Total": e.Value = item.Total; break;
                case "CategoriaId": e.Value = item.CategoriaId; break;
                case "ProveedorId": e.Value = item.ProveedorId; break;
                case "TipoVentaId": e.Value = item.TipoVentaId; break;
                case "Activo": e.Value = item.Activo; break;
            }
        }

        private void Dgv_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= vistaActual.Count) return;

            var item = vistaActual[e.RowIndex];
            var col = dgv.Columns[e.ColumnIndex].DataPropertyName;

            object oldValue = null;

            try
            {
                switch (col)
                {
                    case "Nombre":
                        oldValue = item.Nombre;
                        item.Nombre = e.Value?.ToString();
                        break;

                    case "PrecioCompra":
                        oldValue = item.PrecioCompra;
                        item.PrecioCompra = Convert.ToDecimal(e.Value);
                        break;

                    case "PrecioVenta":
                        oldValue = item.PrecioVenta;
                        item.PrecioVenta = Convert.ToDecimal(e.Value);
                        break;

                    case "Ajuste":
                        oldValue = item.Ajuste;
                        item.Ajuste = Convert.ToInt32(e.Value);
                        break;

                    case "CategoriaId":
                        oldValue = item.CategoriaId;
                        item.CategoriaId = Convert.ToInt32(e.Value);
                        break;

                    case "ProveedorId":
                        oldValue = item.ProveedorId;
                        item.ProveedorId = Convert.ToInt32(e.Value);
                        break;

                    case "TipoVentaId":
                        oldValue = item.TipoVentaId;
                        item.TipoVentaId = Convert.ToInt32(e.Value);
                        break;

                    case "Activo":
                        oldValue = item.Activo;
                        item.Activo = Convert.ToBoolean(e.Value);
                        break;
                }

                // 🔥 GUARDAR EN UNDO
                undoStack.Push((e.RowIndex, col, oldValue));

                // 🔥 MARCAR FILA COMO EDITADA
                filasEditadas.Add(e.RowIndex);
            }
            catch { }

            dgv.InvalidateRow(e.RowIndex);
        }

        private void Undo()
        {
            if (undoStack.Count == 0) return;

            var (row, col, oldValue) = undoStack.Pop();

            if (row < 0 || row >= vistaActual.Count) return;

            var item = vistaActual[row];

            switch (col)
            {
                case "Nombre": item.Nombre = oldValue?.ToString(); break;
                case "PrecioCompra": item.PrecioCompra = Convert.ToDecimal(oldValue); break;
                case "PrecioVenta": item.PrecioVenta = Convert.ToDecimal(oldValue); break;
                case "Ajuste": item.Ajuste = Convert.ToInt32(oldValue); break;
                case "CategoriaId": item.CategoriaId = Convert.ToInt32(oldValue); break;
                case "ProveedorId": item.ProveedorId = Convert.ToInt32(oldValue); break;
                case "TipoVentaId": item.TipoVentaId = Convert.ToInt32(oldValue); break;
                case "Activo": item.Activo = Convert.ToBoolean(oldValue); break;
            }

            dgv.Refresh();
        }

        private void QuitarElementoSeleccionado()
        {
            // Verificamos que haya una fila seleccionada
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0) return;

            int index = dgv.CurrentRow.Index;


            // Validamos que el índice exista en nuestra lista
            if (index < vistaActual.Count)
            {
                // PREGUNTAR ANTES DE QUITAR
                var confirm = MessageBox.Show("¿Seguro que desea quitar la fila?", "Confirmar", MessageBoxButtons.YesNo);
                if (confirm != DialogResult.Yes) return;

                // 1. Quitamos el objeto de la lista que alimenta al Grid
                vistaActual.RemoveAt(index);

                // 2. IMPORTANTE: En Modo Virtual debemos actualizar el RowCount manualmente
                dgv.RowCount = vistaActual.Count;

                // 3. Limpiar el rastro de "fila editada" si existía para ese índice
                // Como los índices cambian al eliminar, lo más sano es limpiar o refrescar
                filasEditadas.Remove(index);

                // 4. Refrescar el Grid para mostrar los cambios
                dgv.Refresh();

                // Opcional: Actualizar el label de conteo que ya tienes
                lblPagina.Text = $"Registros: {vistaActual.Count}";
            }
        }

        private void Dgv_KeyDown(object sender, KeyEventArgs e)
        {
            // Solo permitimos quitar si estamos en Modo Inventario (activo)
            if (e.KeyCode == Keys.Escape && modoEdicion)
            {
                // Evitamos que el ESC haga otras acciones por defecto en el Grid
                e.Handled = true;

                QuitarElementoSeleccionado();
            }
        }

        // FUNCION PARA PINTAR FILAS SI ES PRODUCTO NUEVO O ACTUALIZAR
        private void PintarFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                // En Virtual Mode, no usamos row.DataBoundItem directamente de forma fiable
                // Es mejor acceder a tu lista 'vistaActual' por índice
                if (row.Index < 0 || row.Index >= vistaActual.Count) continue;

                var item = vistaActual[row.Index];

                if (item.EsNuevo)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    row.DefaultCellStyle.SelectionBackColor = Color.Green; // Color cuando está seleccionada
                }
                else if (filasEditadas.Contains(row.Index))
                {
                    // Si la fila ha sido editada (está en el set de filasEditadas)
                    row.DefaultCellStyle.BackColor = Color.LightYellow;
                    row.DefaultCellStyle.SelectionBackColor = Color.Goldenrod;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }
            }
        }

        private void LimpiarGridTotalmente()
        {
            // 1. Limpiamos las fuentes de datos
            vistaActual.Clear();
            cacheProductos.Clear(); // Si quieres que refresque desde DB la próxima vez

            // 2. Limpiamos los rastreadores de cambios
            filasEditadas.Clear();
            undoStack.Clear();

            // 3. Reset visual del Grid
            dgv.RowCount = 0;
            dgv.Invalidate();
            dgv.Refresh();

            // 4. Limpiar buscador
            txtBuscar.Clear();
        }
    }
}
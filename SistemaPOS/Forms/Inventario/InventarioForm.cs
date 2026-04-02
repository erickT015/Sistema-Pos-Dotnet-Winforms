using ClosedXML.Excel;
using SistemaPOS.Data;
using SistemaPOS.Helpers;
using SistemaPOS.Models;
using SistemaPOS.Models.DTOs.Inventario;
using SistemaPOS.Services;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Globalization;

namespace SistemaPOS.Forms
{
    public class InventarioForm : Form
    {
        private InventarioService _service;

        private DataGridView dgv; // Aquí se pinta la tabla (grid principal)
        private TextBox txtScan;  // Input para escaneo rápido
        private Button btnGuardar, btnModo, btnExportar, btnImportar, btnEditar, btnLimpiar;

        private AppDbContext db = new AppDbContext(); // conexión a BD

        private bool modoEdicion = false;        // modo inventario ON/OFF
        private bool esAdmin = true;             // permisos
        private bool modoEdicionManual = false;  // edición manual activa

        private string columnaOrdenada = "";     // qué columna ordeno
        private bool ordenAscendente = true;     // dirección orden

        private int paginaActual = 1;            // paginación
        private int tamanioPagina = 500;
        private int totalPaginas = 1;
        private Label lblPagina;

        private TextBox txtBuscar;               // buscador
        private Button btnBuscar;

        private List<InventarioItemDTO> cacheProductos = new(); // TODO cargado en memoria
        private List<InventarioItemDTO> vistaActual = new();    // lo que se muestra

        private HashSet<int> filasEditadas = new(); // filas modificadas
        private Stack<(int row, string col, object oldValue)> undoStack = new(); // CTRL+Z

        public InventarioForm()
        {
            _service = new InventarioService(db);
            InicializarUI();        // Construye toda la pantalla (header, grid, botones)
            ConfigurarGrid();       // Define columnas del dgv
            CargarCache();          // Trae datos de BD → cacheProductos
            ConfigurarVirtualMode();// optimiza rendimiento
            CargarVista();          // llena el grid (vistaActual)
            SetModoEdicion(false);
            btnLimpiar.Enabled = false;
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

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); //TAMAÑO FIJO DE 70
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // PORCENTAJE DEL 100%
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); //TAMAÑO FIJO DE 55


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
            btnExportar.Click += btnExportar_Click;

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
            btnImportar.Click += btnImportar_Click;

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

            //BOTON DE LIMPIAR MARCADOS
            btnLimpiar = new Button
            {
                Text = "Limpiar Marcados",
                Width = 140,
                Height = 35,
                BackColor = Color.FromArgb(192, 57, 43), // Un rojo elegante (Alizarin)
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnLimpiar.Click += btnLimpiar_Click;


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
            panelTop.Controls.Add(btnLimpiar);


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
                {
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            // DETECTA SI UNA FILA ESTÁ EN LA LISTA DE FILAS EDITADAS Y LE CAMBIA EL COLOR DE FONDO
            dgv.RowPrePaint += (s, e) =>
            {
                if (filasEditadas.Contains(e.RowIndex))
                {
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
                }
            };

            // AL PRESIONAR UNA CELDA SI ES ESTA, RETORNA
            dgv.CellBeginEdit += (s, e) =>
            {
                var col = dgv.Columns[e.ColumnIndex].DataPropertyName;

                if (col == "Codigo")
                    e.Cancel = true;

                if (col == "GananciaUnitaria")
                    e.Cancel = true;
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

            // EVENTO QUE LLAMA LA FUNCION PARA DETECTAR SI EL USUARIO PRESIONA ESC PARA QUITAR FILA
            dgv.KeyDown += Dgv_KeyDown;



            // ================= BOTONES FOOTER =================

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


            //TEXTO DE PAGINACION, DEFINIMOS PROPIEDADES
            lblPagina = new Label
            {
                Text = "Página 1",
                AutoSize = true,
                Anchor = AnchorStyles.Top
            };

            // PANEL PADRE INFERIOR FOOTER ( - ,PAGINACIÓN, BOTÓN GUARDAR)
            var panelBottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                Padding = new Padding(0, 12, 0, 0) //GENERA UN PADDING DE TODOS LOS ELEMENOTS, EN ESTA CASO 10 DESDE ARRIBA
            };

            panelBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            panelBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            panelBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

            // SUBPANEL DE BOTÓN GUARDAR (ALINEADO A LA DERECHA)
            var panelDerecha = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill //   QUE SE PEGUE ARRIBA
            };

            // DEFINIMOS QUE CONTIENE CADA PANEL (EN ESTE CASO EL PANEL FOOTER DE LA DERECHA)
            panelDerecha.Controls.Add(btnGuardar);
         //   panelDerecha.Controls.Add(btnLimpiar);

            //METEMOS CADA PANEL EN EL PANEL PRINCIPAL (PANEL BOTTOM)
            panelBottom.Controls.Add(new Panel(), 0, 0); //PANEL VACIO
            panelBottom.Controls.Add(lblPagina, 1, 0);  //PANDEL CENTRAL
            panelBottom.Controls.Add(panelDerecha, 2, 0);   //PANEL DERECHA

            //METEMOS TODO EN EL TABLELAYOUT PRINCIPAL
            layout.Controls.Add(panelTop, 0, 0);
            layout.Controls.Add(dgv, 0, 1);
            layout.Controls.Add(panelBottom, 0, 2);

            this.Controls.Add(layout);
        }


        // ========== LOGICA INICIAL DE CARGA =================

        // CONSTRUYE LA VISTA ACTUAL DESDE CACHE APLICANDO FILTRO DE BUSQUEDA Y ORDEN
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

        // CARGA TODOS LO PRODUCTOS DESDE DB HACIA LA CACHE EN MEMORIA
        private void CargarCache()
        {
            cacheProductos = _service.QueryInventario().ToList();
        }

        //RECARGA COMPLETAMENTE LOS DASTOS TRAYENDO DESDE DB Y RECONSTRUYE LA VISTA
        private void RecargarDatos()
        {
            CargarCache();
            CargarVista();
        }

        // LIMPIA UI Y DECIDE SI RECONSTRUYE DESDE DB O DESDE CACHE
        private void RefrescarTodo(bool recargarDesdeDb = false, bool limpiarBusqueda = false)
        {
            //  SIEMPRE limpiamos UI
            LimpiarGridTotalmente(recargarDesdeDb);

            if (recargarDesdeDb)
            {
                //  CASO PESADO → DB
                RecargarDatos();
            }
            else
            {
                //  CASO LIVIANO → CACHE
                CargarVista();
            }

            //  estado edición
            ResetEstadoEdicion();

            if (limpiarBusqueda)
                txtBuscar.Clear();

            dgv.Refresh();
        }



        // =========LOGICA DE INVENTARIO ESCANEO =======================

        // ACTIVA O DESACTIVA EL MODO INVENTARIO CONTROLANDO LIMPIEZA DE VISTA Y ESTADO
        private void ActivarModo(bool activo)
        {
            modoEdicion = activo;

            if (activo)
            {
                vistaActual.Clear();
                dgv.RowCount = 0;
            }
            else
            {
                RefrescarTodo(false); //  cache, NO DB
            }

            AplicarEstadoUI();
        }

        // PROCESA UN CODIGO ESCANEADO, AJUSTANDO EXISTENTE O CREANDO PRODUCTO NUEVO
        private void Escanear(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return;

            var itemExistente = vistaActual.FirstOrDefault(x => x.Codigo == codigo);

            if (itemExistente != null)
            {
                if (!esAdmin)
                {
                    MessageBox.Show("No tienes permisos para modificar inventario.");
                    return;
                }

                itemExistente.Ajuste += 1;
                dgv.InvalidateRow(vistaActual.IndexOf(itemExistente));
                return;
            }

            var producto = _service.ObtenerPorCodigo(codigo);

            InventarioItemDTO nuevoItem;

            if (producto != null)
            {
                nuevoItem = _service.CrearDesdeProducto(producto);
            }
            else
            {
                if (!esAdmin)
                {
                    MessageBox.Show("No puede agregar productos desde acá, hagalo en compras.");
                    return;
                }


                var categoriaDefault = db.Categorias.Select(c => c.Id).FirstOrDefault();
                var proveedorDefault = db.Proveedores.Select(p => p.Id).FirstOrDefault();

                nuevoItem = _service.CrearNuevoProducto(codigo, categoriaDefault, proveedorDefault);
            }

            vistaActual.Add(nuevoItem);
            dgv.RowCount = vistaActual.Count;
            dgv.Refresh();
        }

        // VALIDA  Y GUARDA LOS CAMBIOS REALIZADOS EN INVENTARIO GENERAL
        private void Guardar()
        {
            dgv.EndEdit();

            if (filasEditadas.Count == 0)
            {
                MessageBox.Show("No hay cambios detectados.", "Aviso");
                return;
            }

            try
            {
                var itemsParaGuardar = ObtenerItemsEditados();

                if (!ValidarItems(itemsParaGuardar))
                    return;

                _service.GuardarItems(itemsParaGuardar);

                MessageBox.Show("¡Datos guardados con éxito!");

                PostGuardar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}");
            }
        }

        //EJECUTA ACCIONES POSTERIORES AL GUARDADO COMO RECARGA O RESET DE ESTADO
        private void PostGuardar()
        {
            RefrescarTodo(true); //  DB

            SetModoEdicion(false);

            if (modoEdicion)
            {
                ActivarModo(false);
            }
        }



        // ================ LOGICA DE INVENTARIO MANUAL ====================

        //ALTERNA ENTRE MODOEDICION MANUAL VALIDANDO CAMBIOS PENDIENTES ANTES DE GUARDAR
        private void ToggleEdicion()
        {
            if (!ManejarCambiosPendientes())
                return;

            SetModoEdicion(!modoEdicionManual);
        }

        //ACTIVA O DESACTIVA EL MODO DE EDICION MANUAL Y AJUSTA EL ESTADO DEL SISTEMA
        private void SetModoEdicion(bool activo)
        {
            modoEdicionManual = activo;

            if (!activo)
                ResetEstadoEdicion();

            AplicarEstadoUI();
        }

        //MANEJA CONFIRMACION DE CAMBIOS PENDIENTES PERMITIENDO GUARDAR, DESCARTAR O CANCELAR
        private bool ManejarCambiosPendientes()
        {
            if (!modoEdicionManual || filasEditadas.Count == 0)
                return true; // no hay nada que bloquear

            var res = MessageBox.Show(
                "Tiene cambios sin guardar. ¿Desea aplicarlos ahora?",
                "Cambios Pendientes",
                MessageBoxButtons.YesNoCancel);

            if (res == DialogResult.Yes)
            {
                Guardar();
                return false;
            }

            if (res == DialogResult.No)
            {
                ResetEstadoEdicion(); 
                RefrescarTodo(false);
                MessageBox.Show("Cambios descartados.");
                return true;
            }

            return false; // cancel
        }



        // ========== FLUJO GRID, INTERACCION DIRECTA CON CELDAS =======================

        //PROPORCIONA VALIRES AL GRID SEGUN EN MODO VIRATUAL SEGUN LA SELECCION
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
                case "GananciaUnitaria": e.Value = item.GananciaUnitaria; break;
                case "PorcentajeGanancia": e.Value = item.PorcentajeGanancia; break;
                case "Ajuste": e.Value = item.Ajuste; break;
                case "Total": e.Value = item.Total; break;
                case "CategoriaId": e.Value = item.CategoriaId; break;
                case "ProveedorId": e.Value = item.ProveedorId; break;
                case "TipoVentaId": e.Value = item.TipoVentaId; break;
                case "Activo": e.Value = item.Activo; break;
                case "Eliminar": e.Value = item.IsSelected; break;
            }
        }

        //APLICA CAMBIOS EDITADOS EN EL GRID, VALIDANDO Y REGISTRANDO PARA UNDO
        private void Dgv_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= vistaActual.Count) return;

            var item = vistaActual[e.RowIndex];
            var col = dgv.Columns[e.ColumnIndex].DataPropertyName;

            if (!esAdmin)
            {
                if (col == "Ajuste" || col == "StockActual" || col == "Total")
                    return;
            }

            if (dgv.Columns[e.ColumnIndex].Name == "IsSelected")
            {
                vistaActual[e.RowIndex].IsSelected = (bool)e.Value;
            }

            if (col == "GananciaUnitaria")
                return;

            if (col == "Codigo")
                return;

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
                        {
                            oldValue = item.PrecioCompra;

                            if (NumberHelper.TryParseDecimal(e.Value, out decimal resultado))
                            {
                                item.PrecioCompra = resultado;
                                RecalcularGanancia(item); // calcular ganancia
                            }
                            else
                            {
                                MessageBox.Show("Número inválido (ej: 1500,50)");
                                return;
                            }

                            break;
                        }

                    case "PrecioVenta":
                        {
                            oldValue = item.PrecioVenta;

                            if (NumberHelper.TryParseDecimal(e.Value, out decimal resultado))
                            {
                                item.PrecioVenta = resultado;

                                CalcularDesdeVenta(item);
                            }
                            else
                            {
                                MessageBox.Show("Número inválido");
                                return;
                            }

                            break;
                        }

                    case "PorcentajeGanancia":
                        {
                            oldValue = item.PorcentajeGanancia;

                            if (NumberHelper.TryParseDecimal(e.Value, out decimal resultado))
                            {
                                item.PorcentajeGanancia = resultado;

                                // 🔥 recalcula precio venta
                                CalcularDesdePorcentaje(item);
                            }
                            else
                            {
                                MessageBox.Show("Número inválido");
                                return;
                            }

                            break;
                        }

                    case "Ajuste":
                        oldValue = item.Ajuste;

                        if (!int.TryParse(e.Value?.ToString(), out int ajuste))
                        {
                            MessageBox.Show("Ajuste inválido");
                            return;
                        }

                        item.Ajuste = ajuste;
                        break;

                    case "CategoriaId":
                        oldValue = item.CategoriaId;
                        item.CategoriaId = Convert.ToInt32(e.Value);
                        break;

                    /* case "StockActual": //ESTOY PENSANDO SI QUITARLO YA QUE ME GUSTA QUE SEA OBLIGATORIO EDITAR POR AJUSTE
                         oldValue = item.StockActual;
                         if (!int.TryParse(e.Value?.ToString(), out int StockActual))
                         {
                             MessageBox.Show("Ajuste inválido");
                             return;
                         }

                         item.Ajuste = StockActual;
                         break;*/

                    /* case "Total":
                         oldValue = item.Total;
                         item.Total = Convert.ToInt32(e.Value);
                         break;*/

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

                //  GUARDAR EN UNDO
                undoStack.Push((e.RowIndex, col, oldValue));

                //  MARCAR FILA COMO EDITADA
                filasEditadas.Add(e.RowIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar celda: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
            }

            dgv.InvalidateRow(e.RowIndex);
        }

        //REVIERTE EL ULTIMO CAMBIO REALIZADO EN EL GRID
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

        //ELIMINA LA FILA SELECCIONADA ACTUALIZANDO ESTADO, INDICES Y UI
        private void QuitarElementoSeleccionado()
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0)
                return;

            int index = dgv.CurrentRow.Index;

            if (index >= vistaActual.Count)
                return;

            var confirm = MessageBox.Show(
                "¿Seguro que desea quitar la fila?",
                "Confirmar",
                MessageBoxButtons.YesNo);

            if (confirm != DialogResult.Yes)
                return;

            // 1. eliminar del origen
            vistaActual.RemoveAt(index);

            // 2. corregir estructuras internas 
            ReindexarFilasEditadas(index);
            LimpiarUndoDeFila(index);

            // 3. actualizar grid
            dgv.RowCount = vistaActual.Count;
            dgv.Refresh();

            // 4. actualizar label
            lblPagina.Text = $"Registros: {vistaActual.Count}";
        }

        //MANEJA TECLAS DEL GRID, PERMITE ELIMINAR FILAS CON ESC
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



        // =========== BOTONES ===========================

        //ELIMINA U OCULTA PRODUCTOS MARCADOS APLICANDO A DB Y REFRESCA VISTA
        private void btnLimpiar_Click(object? sender, EventArgs e)
        {
            int cuantos = cacheProductos.Count(x => x.IsSelected);

            if (cuantos == 0)
            {
                MessageBox.Show("No has marcado ningún producto para limpiar.");
                return;
            }

            var confirm = MessageBox.Show(
                $"¿Seguro que quieres eliminar/ocultar {cuantos} productos?",
                "Confirmar Limpieza",
                MessageBoxButtons.YesNo);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _service.EliminarMarcados(cacheProductos);

                CargarCache();
                RefrescarTodo(true);

                SetModoEdicion(false);

                MessageBox.Show("Limpieza completada con éxito.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar: " + ex.Message);
            }
        }

        //EXPORTA EL INVENTARIO A UN EXCEL
        private void btnExportar_Click(object? sender, EventArgs e)
        {
            string fechaActual = DateTime.Now.ToString("yyyy-MM-dd");

            SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"Inventario_Hayda_{fechaActual}.xlsx" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _service.ExportarAExcel(sfd.FileName);
                    MessageBox.Show("Archivo exportado correctamente.");
                }
                catch (Exception ex) { MessageBox.Show("Error al exportar: " + ex.Message); }
            }
        }

        // IMPORTA PRODUCTOS DESDE EXCEL Y RECARGA LA VISTA
        private void btnImportar_Click(object? sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Workbook|*.xlsx" };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            //  VALIDACIÓN CONFIRMACIÓN 
            var confirm = MessageBox.Show(
                "¿Seguro que deseas importar este archivo?\nEsto actualizará productos existentes y agregará nuevos.",
                "Confirmar importación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes)
                return; // DETENER

            try
            {
                _service.ImportarDesdeExcel(ofd.FileName);
                RefrescarTodo(true);
                MessageBox.Show("¡Importación exitosa!", "Minisuper Hayda");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error crítico en importación: " + ex.Message);
            }
        }


        // ========= CONFIGURACION DE UI / GRID ==============

        //APLICA EL ESTADO VISUAL DE LA UI SEGUN EL MODO ACTUAL Y PERMISOS
        private void AplicarEstadoUI()
        {
            // ESTADO BASE TODO BLOQUEADO
            bool puedeEditar = false;
            bool puedeEscanear = false;

            //  Modo edición manual
            if (modoEdicionManual)
            {
                puedeEditar = true;
            }

            //  Modo inventario
            if (modoEdicion)
            {
                puedeEditar = true;
                puedeEscanear = true;
            }

            //  PERMISOS ADMIN
            //  GRID Y ENTRADAS

            dgv.ReadOnly = !puedeEditar;
            txtScan.Enabled = puedeEscanear;

            // BOTONES PRINCIPALES

            // Guardar (solo inventario)
            btnGuardar.Enabled = modoEdicion;

            // Limpiar (solo edición manual)
            btnLimpiar.Enabled = modoEdicionManual;

            //  BLOQUEO ENTRE MODOS
            btnModo.Enabled = !modoEdicionManual; // no inventario si estoy editando
            btnEditar.Enabled = !modoEdicion;     // no editar si estoy en inventario

            // Importar / Exportar solo en vista
            bool enVista = !modoEdicion && !modoEdicionManual;
            btnImportar.Enabled = enVista;
            btnExportar.Enabled = enVista;

            // TEXTOS Y COLORES

            // Botón editar
            if (modoEdicionManual)
            {
                btnEditar.Text = "Guardar";
                btnEditar.BackColor = Color.LightGreen;
            }
            else
            {
                btnEditar.Text = "Editar";
                btnEditar.BackColor = default;
                btnEditar.UseVisualStyleBackColor = true;
            }

            // Botón inventario
            btnModo.Text = modoEdicion ? "Cancelar Inventario" : "Iniciar Inventario";
            btnModo.BackColor = modoEdicion ? Color.IndianRed : Color.LightGray;
            btnModo.ForeColor = modoEdicion ? Color.White : Color.Black;
        }

        //CONFIGURA COLUMNAS, ESTILOS Y COMPORTAMIENTOS DEL GRID
        private void ConfigurarGrid()
        {
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Clear();

            // 1. Columna de Checkbox
            var colCheck = new DataGridViewCheckBoxColumn { Name = "IsSelected", HeaderText = "✔", DataPropertyName = "Eliminar",  Width = 50 };
            dgv.Columns.Add(colCheck);

            // 1. Columnas de Texto básicas
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", HeaderText = "Id", Visible = false, Width = 50, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", DataPropertyName = "Codigo", HeaderText = "Código", Width = 150, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nombre", DataPropertyName = "Nombre", HeaderText = "Producto", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            //  Dropdown QUEMADO (Tipo de Venta)
            var tiposVenta = new[] {  new { Id = 1, Nombre = "Unidad" }, new { Id = 2, Nombre = "Peso (Kg)" },  new { Id = 3, Nombre = "Líquido (Litro)" } }.ToList();

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

            //  Dropdown Dinámico (Categorías)
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

            // Dropdown Dinámico (Proveedores)
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrecioCompra", DataPropertyName = "PrecioCompra", HeaderText = "P. Compra", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PrecioVenta", HeaderText = "P. Venta", Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "GananciaUnitaria", HeaderText = "Ganancia", ReadOnly = true, Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", BackColor = Color.FromArgb(240, 240, 240) } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PorcentajeGanancia", HeaderText = "%", Width = 60,  DefaultCellStyle = new DataGridViewCellStyle { Format = "N1" } });

            // 6. Inventario (Lectura y Ajuste)
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StockActual", DataPropertyName = "StockActual", HeaderText = "Stock", Width = 60, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(240, 240, 240) } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ajuste", DataPropertyName = "Ajuste", HeaderText = "Ajuste", Width = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn {Name = "Total", DataPropertyName = "Total", HeaderText = "Total", Width = 60, ReadOnly = true });

            // VALIDACION PARA AJUSTE SI NO ES ADMIN
            if (!esAdmin)
            {
                dgv.Columns["Ajuste"].Visible = false;
                dgv.Columns["StockActual"].Visible = false;
            }

            dgv.ColumnHeaderMouseClick += (s, e) =>
            {
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

        // ACTIVA EL MODO VIRTUAL DEL GRIS Y ENLAZA LOS EVENTOS NECESARIOS
        private void ConfigurarVirtualMode()
        {
            dgv.VirtualMode = true;
            dgv.ReadOnly = false;

            dgv.CellValueNeeded += Dgv_CellValueNeeded;
            dgv.CellValuePushed += Dgv_CellValuePushed; // 🔥 ESTE ES EL IMPORTANTE


        }

        // ORDENA LA LISTA SELECCIONADA ACTUAL SEGUN LA COLUMNA SELECCIOANDA
        private void OrdenarLista()
        {
            if (string.IsNullOrEmpty(columnaOrdenada) || vistaActual.Count == 0) return;

            Func<InventarioItemDTO, object> keySelector = columnaOrdenada switch
            {
                nameof(InventarioItemDTO.Codigo) => x => x.Codigo,
                nameof(InventarioItemDTO.Nombre) => x => x.Nombre,
                nameof(InventarioItemDTO.PrecioCompra) => x => x.PrecioCompra,
                nameof(InventarioItemDTO.PrecioVenta) => x => x.PrecioVenta,
                nameof(InventarioItemDTO.StockActual) => x => x.StockActual,
                nameof(InventarioItemDTO.Ajuste) => x => x.Ajuste,
                nameof(InventarioItemDTO.Total) => x => x.Total,
                nameof(InventarioItemDTO.CategoriaId) => x => x.CategoriaId,
                nameof(InventarioItemDTO.ProveedorId) => x => x.ProveedorId,
                nameof(InventarioItemDTO.TipoVentaId) => x => x.TipoVentaId,
                _ => x => x.Nombre
            };

            vistaActual = ordenAscendente
                ? vistaActual.OrderBy(keySelector).ToList()
                : vistaActual.OrderByDescending(keySelector).ToList();

            dgv.Refresh();
        }



        // ============== LIMPIEZA Y RESET ======================

        //LIMPIA COMPLETAMENTE EL GRID, ESTADO INTERNO Y OPCIONALMENTE LA CACHE DE PRODUCTOS
        private void LimpiarGridTotalmente(bool limpiarCache = false)
        {
            vistaActual.Clear();

            if (limpiarCache)
                cacheProductos.Clear();

            filasEditadas.Clear();
            undoStack.Clear();

            dgv.RowCount = 0;
            dgv.Invalidate();
            dgv.Refresh();
        }

        //REINICIALIZA EL ESTADO DE EDICIÓN MANUAL, LIMPIANDO FILAS EDITADAS Y STACK DE UNDO
        private void ResetEstadoEdicion()
        {
            filasEditadas.Clear();
            undoStack.Clear();
        }


        // ================ HELPERS DE NEGOCIO ===============================

        // OBTIENE LA LISTA DE ITEMS MODIFICADOS LISTOS PARA GUARDAR 
        private List<InventarioItemDTO> ObtenerItemsEditados()
        {
            var indicesParaProcesar = filasEditadas.Distinct().ToList();

            return indicesParaProcesar
                .Where(i => i >= 0 && i < vistaActual.Count)
                .Select(i => vistaActual[i])
                .ToList();
        }

        //VALIDA QUE TODOS LOS ITEMS CUMPLAN CON REGLAS DE NEGOCIO ANTES DE GUARDAR
        private bool ValidarItems(List<InventarioItemDTO> items)
        {
            foreach (var item in items)
            {
                if (!ValidationHelper.EsValido(item))
                    return false;
            }
            return true;
        }

        // RECALCULA LA GANANCIA UNIDADARIA Y PORCENTAJE DE GANANCIA A PARTIR DEL PRECIO DE COMPRA Y VENTA
        private void RecalcularGanancia(InventarioItemDTO item)
        {
            item.GananciaUnitaria = item.PrecioVenta - item.PrecioCompra;

            if (item.PrecioCompra > 0)
                item.PorcentajeGanancia = (item.GananciaUnitaria / item.PrecioCompra) * 100;
            else
                item.PorcentajeGanancia = 0;
        }

        // CALCULA EL PORCENTAJE DE GANANCIA A PARTIR DEL PRECIO DE VENTA Y COMPRA
        private void CalcularDesdeVenta(InventarioItemDTO item)
        {
            item.GananciaUnitaria = item.PrecioVenta - item.PrecioCompra;

            if (item.PrecioCompra > 0)
                item.PorcentajeGanancia = (item.GananciaUnitaria / item.PrecioCompra) * 100;
            else
                item.PorcentajeGanancia = 0;
        }

        // CALCULA EL PRECIO DE VENTA Y GANANCIA UNIDADARIA A PARTIR DEL PORCENTAJE DE GANANCIA SOBRE EL PRECIO DE COMPRA
        private void CalcularDesdePorcentaje(InventarioItemDTO item)
        {
            if (item.PrecioCompra > 0)
            {
                item.PrecioVenta = item.PrecioCompra * (1 + (item.PorcentajeGanancia / 100));
                item.GananciaUnitaria = item.PrecioVenta - item.PrecioCompra;
            }
        }


        // ================== HELPERS INTERNOS  =================

        // REAJUSTA LOS INDICES DE LAS FILAS EDITADAS CUANDO SE ELIMINA UNA FILA, PARA MANTENER LA CONSISTENCIA
        private void ReindexarFilasEditadas(int indexEliminado)
        {
            var nuevas = new HashSet<int>();

            foreach (var i in filasEditadas)
            {
                if (i == indexEliminado)
                    continue;

                if (i > indexEliminado)
                    nuevas.Add(i - 1);
                else
                    nuevas.Add(i);
            }

            filasEditadas = nuevas;
        }

        //LIMPIA Y REORDENA ELSTACK DE UNDO TRAS ELIMINAR UNA FILA
        private void LimpiarUndoDeFila(int indexEliminado)
        {
            var nuevoStack = new Stack<(int row, string col, object oldValue)>();

            foreach (var item in undoStack)
            {
                if (item.row == indexEliminado)
                    continue;

                if (item.row > indexEliminado)
                    nuevoStack.Push((item.row - 1, item.col, item.oldValue));
                else
                    nuevoStack.Push(item);
            }

            undoStack = new Stack<(int, string, object)>(nuevoStack.Reverse());
        }
    }
}

using SistemaPOS.Forms.Admin;
using System.Windows.Forms;

namespace SistemaPOS.Forms
{
    public partial class MainForm : Form
    {
        // =========================
        // 📦 CONTENEDORES
        // =========================
        private Panel sidebar;
        private Panel header;
        private Panel content;

        private Form currentForm;

        public MainForm()
        {
            InicializarUI();

        }

        private void InicializarUI()
        {
            this.Text = "Sistema POS";

            // 1. PRIMERO: definimos el tamaño que quieres que tenga cuando NO esté maximizado
            this.Size = new Size(1280, 800);

            // 2. SEGUNDO: Define el tamaño mínimo para que no se vea feo si lo encogen
            this.MinimumSize = new Size(1024, 720);

            // 3. TERCERO: Centra la ventana
            this.StartPosition = FormStartPosition.CenterScreen;

            // 4. AL FINAL: Maximiza la ventana
            this.WindowState = FormWindowState.Maximized;

            this.BackColor = Color.FromArgb(240, 240, 240);

            // 🟦 SIDEBAR
            sidebar = new Panel
            {
                Width = 220,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(20, 30, 50)
            };

            var lblLogo = new Label
            {
                Text = "Minisuper",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Height = 75,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 🔘 BOTÓN NUEVA VENTA
            var btnVenta = new Button
            {
                Text = "+ Nueva Venta",
                Height = 60,
                Dock = DockStyle.Top,
                Margin = new Padding(15), // Esto necesita un contenedor para funcionar bien, o usa Location
                BackColor = Color.FromArgb(55, 65, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnVenta.FlatAppearance.BorderSize = 0;
            // Un pequeño truco para el margen:
            Panel pnlContenedorVenta = new Panel { Height = 60, Dock = DockStyle.Top, Padding = new Padding(15, 10, 15, 10) };
            pnlContenedorVenta.Controls.Add(btnVenta);
            sidebar.Controls.Add(pnlContenedorVenta);

            // 📌 MENÚ COMPLETO
            // Panel (admin)
            sidebar.Controls.Add(CrearMenu("Panel", () => AbrirEnPanel(new ModuloAdmin())));

            // Inventario negativo
            sidebar.Controls.Add(CrearMenu("Inventario Negativo", null));

            // Devoluciones
            sidebar.Controls.Add(CrearMenu("Devoluciones", null));

            // Finanzas
            //sidebar.Controls.Add(CrearMenu("Finanzas", () => AbrirEnPanel(new FinanzasForm())));
            sidebar.Controls.Add(CrearMenu("Finanzas", null));

            // Gastos
            sidebar.Controls.Add(CrearMenu("Gastos", null));

            // Inventario
            sidebar.Controls.Add(CrearMenu("Inventario", () => AbrirEnPanel(new InventarioForm())));

            // Compras
            //sidebar.Controls.Add(CrearMenu("Compras", () => AbrirEnPanel(new ComprasForm())));
            sidebar.Controls.Add(CrearMenu("Compras", null));

            // Ventas (historial)
            sidebar.Controls.Add(CrearMenu("Ventas", null));

            // Inicio
            sidebar.Controls.Add(CrearMenu("Inicio", null));

            // Botón principal
            sidebar.Controls.Add(btnVenta);

            // Logo
            sidebar.Controls.Add(lblLogo);


            // ⬜ HEADER
            header = new Panel
            {
                Height = 75,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(245, 245, 248)
            };

            var lblUsuario = new Label
            {
                Text = "Bienvenido, Admin",
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = false,
                Dock = DockStyle.Right, // Se pega a la derecha del header
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 200,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            header.Controls.Add(lblUsuario);

            // 📦 CONTENT
            content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20) // 👈 ESTE ES EL FIX REAL
            };

            var lblInicio = new Label
            {
                Text = "BIENVENIDO",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 18, FontStyle.Bold)
            };

            content.Controls.Add(lblInicio);


            // 🧩 RENDERIZAR
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };

            // COLUMNAS
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); // sidebar fijo
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // resto

            // FILAS
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 75)); // header
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // contenido

            // AGREGAR CONTROLES
            layout.Controls.Add(sidebar, 0, 0);
            layout.SetRowSpan(sidebar, 2); // sidebar ocupa ambas filas

            layout.Controls.Add(header, 1, 0);
            layout.Controls.Add(content, 1, 1);

            // LIMPIAR Y AGREGAR
            this.Controls.Clear();
            this.Controls.Add(layout);

            sidebar.BringToFront();
            header.BringToFront();
        }

        // 🔘 CREAR ITEM MENÚ
        private Button CrearMenu(string texto, Action click)
        {
            var btn = new Button
            {
                Text = "      " + texto, // Espacio para el icono
                Height = 45,
                Dock = DockStyle.Top,
                // El azul de la foto es más tirando a grisáceo: 28, 35, 48
                BackColor = Color.FromArgb(28, 35, 48),
                ForeColor = Color.FromArgb(180, 190, 200), // Texto gris claro, no blanco puro
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular), // Letra más fina
                Cursor = Cursors.Hand
            };

            // Quitar el borde feo que sale al hacer click
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 50, 65);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 45, 60);

            btn.Click += (s, e) =>
            {
                // Marcamos el botón como "Activo" visualmente
                ResaltarBotonActivo((Button)s);
                click?.Invoke();
            };

            return btn;
        }
        private void ResaltarBotonActivo(Button btnSeleccionado)
        {
            foreach (Control c in sidebar.Controls)
            {
                if (c is Button b)
                {
                    b.BackColor = Color.FromArgb(28, 35, 48); // Color normal
                    b.ForeColor = Color.FromArgb(180, 190, 200);
                }
            }
            // Color cuando está activo (el celeste de la foto)
            btnSeleccionado.BackColor = Color.FromArgb(35, 45, 60);
            btnSeleccionado.ForeColor = Color.FromArgb(0, 190, 204);
        }


        // 🔄 RENDER
        private void AbrirEnPanel(Form formHijo)
        {
            if (currentForm != null)
                currentForm.Close();

            currentForm = formHijo;

            formHijo.TopLevel = false;
            formHijo.FormBorderStyle = FormBorderStyle.None;
            formHijo.Dock = DockStyle.Fill;

            content.Controls.Clear();
            content.Controls.Add(formHijo);

            formHijo.Show();
        }
    }
}
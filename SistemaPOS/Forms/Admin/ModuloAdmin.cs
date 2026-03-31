using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaPOS.Forms.Admin
{
    public class ModuloAdmin : Form
    {
        //private Panel menu;
        private Panel content;
        private Form currentForm;
        private FlowLayoutPanel menu;

        public ModuloAdmin()
        {
            InicializarUI();
            Abrir(new ProveedorForm());
        }

        private void InicializarUI()
        {
            this.BackColor = Color.White;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // menú fijo
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // contenido

            // =========================
            // 🔝 MENÚ SUPERIOR
            // =========================
             menu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 248),
                Padding = new Padding(10)
            };

            var btnProveedores = CrearBoton("Proveedores", () => Abrir(new ProveedorForm()));
            var btnCategorias = CrearBoton("Categorías", () => Abrir(new CategoriaForm()));
            var btnUsuarios = CrearBoton("Usuarios", () => Abrir(new UsuarioForm()));

            menu.Controls.Add(btnProveedores);
            menu.Controls.Add(btnCategorias);
            menu.Controls.Add(btnUsuarios);

            // =========================
            // 📦 CONTENIDO
            // =========================
            content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            layout.Controls.Add(menu, 0, 0);
            layout.Controls.Add(content, 0, 1);

            this.Controls.Add(layout);
        }

        private Button CrearBoton(string texto, Action click)
        {
            var btn = new Button
            {
                Text = texto,
                Width = 120,
                Height = 35,
                Margin = new Padding(10, 7, 0, 7),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderSize = 0;

            btn.Click += (s, e) =>
            {
                Resaltar((Button)s);
                click?.Invoke();
            };

            return btn;
        }

        private void Resaltar(Button btnActivo)
        {
            foreach (Control c in menu.Controls)
            {
                if (c is Button b)
                {
                    b.BackColor = Color.White;
                    b.ForeColor = Color.Black;
                }
            }

            btnActivo.BackColor = Color.FromArgb(0, 190, 204);
            btnActivo.ForeColor = Color.White;
        }

        private void Abrir(Form formHijo)
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

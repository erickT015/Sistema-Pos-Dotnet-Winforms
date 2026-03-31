using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaPOS.Helpers
{
    public static  class UIStyles
    {
        // 🎨 COLORES GLOBALES
        public static Color Primary = Color.FromArgb(37, 99, 235);     // 🔵 Azul moderno (tipo Tailwind)
        public static Color PrimaryDark = Color.FromArgb(29, 78, 216);

        public static Color Success = Color.FromArgb(22, 163, 74);     // 🟢 Verde más sobrio
        public static Color Danger = Color.FromArgb(220, 38, 38);      // 🔴 Rojo fuerte pero elegante

        public static Color Neutral = Color.FromArgb(229, 231, 235);   // ⚪ Gris claro (NO oscuro)
        public static Color NeutralText = Color.FromArgb(55, 65, 81);  // Texto oscuro para botones claros

        // 🔘 BOTÓN BASE (EL CSS GLOBAL REAL)
        public static void AplicarBoton(Button btn, Color color, bool textoBlanco = true)
        {
            btn.BackColor = color;
            btn.ForeColor = textoBlanco ? Color.White : NeutralText;

            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;

            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(color, 0.1f);
            btn.MouseLeave += (s, e) => btn.BackColor = color;
        }

        // 🎯 VARIANTES (como CSS classes)

        public static void BotonPrimary(Button btn)
        {
            AplicarBoton(btn, Primary, true);
        }

        public static void BotonSuccess(Button btn)
        {
            AplicarBoton(btn, Success, true);
        }

        public static void BotonDanger(Button btn)
        {
            AplicarBoton(btn, Danger, true);
        }

        public static void BotonNeutral(Button btn)
        {
            AplicarBoton(btn, Neutral, false); // 🔥 TEXTO OSCURO
        }
    }
}

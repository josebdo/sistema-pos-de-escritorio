using System.Drawing;
using System.Windows.Forms;

namespace SistemaCelulares.App.Common;

public static class UITheme
{
    // Paleta de Colores Moderna (Tema Oscuro Profesional / Slate & Indigo)
    public static readonly Color Primary = Color.FromArgb(79, 70, 229);      // Indigo 600
    public static readonly Color PrimaryHover = Color.FromArgb(67, 56, 202); // Indigo 700
    public static readonly Color PrimaryLight = Color.FromArgb(238, 242, 255);// Indigo 50

    public static readonly Color Success = Color.FromArgb(16, 185, 129);     // Emerald 500
    public static readonly Color SuccessHover = Color.FromArgb(5, 150, 105);
    public static readonly Color Danger = Color.FromArgb(239, 68, 68);       // Rose 500
    public static readonly Color DangerHover = Color.FromArgb(220, 38, 38);
    public static readonly Color Warning = Color.FromArgb(245, 158, 11);     // Amber 500

    public static readonly Color DarkBg = Color.FromArgb(15, 23, 42);        // Slate 900
    public static readonly Color SidebarBg = Color.FromArgb(30, 41, 59);     // Slate 800
    public static readonly Color CardBg = Color.White;
    public static readonly Color AppBg = Color.FromArgb(241, 245, 249);      // Slate 100
    public static readonly Color BorderColor = Color.FromArgb(226, 232, 240);// Slate 200

    public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);   // Slate 900
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);  // Slate 500
    public static readonly Color TextLight = Color.FromArgb(248, 250, 252);  // Slate 50

    // Tipografías
    public static readonly Font TitleFont = new("Segoe UI", 16F, FontStyle.Bold);
    public static readonly Font SubtitleFont = new("Segoe UI", 12F, FontStyle.Bold);
    public static readonly Font SectionFont = new("Segoe UI", 10.5F, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 9.5F, FontStyle.Regular);
    public static readonly Font SmallFont = new("Segoe UI", 8.5F, FontStyle.Regular);
    public static readonly Font BadgeFont = new("Segoe UI", 8F, FontStyle.Bold);

    public static void AplicarBotonPrimario(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = Primary;
        btn.ForeColor = Color.White;
        btn.Font = SectionFont;
        btn.Cursor = Cursors.Hand;
        btn.MouseEnter += (s, e) => btn.BackColor = PrimaryHover;
        btn.MouseLeave += (s, e) => btn.BackColor = Primary;
    }

    public static void AplicarBotonSecundario(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = BorderColor;
        btn.FlatAppearance.BorderSize = 1;
        btn.BackColor = Color.White;
        btn.ForeColor = TextPrimary;
        btn.Font = BodyFont;
        btn.Cursor = Cursors.Hand;
        btn.MouseEnter += (s, e) => btn.BackColor = AppBg;
        btn.MouseLeave += (s, e) => btn.BackColor = Color.White;
    }

    public static void AplicarBotonPeligro(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = Danger;
        btn.ForeColor = Color.White;
        btn.Font = BodyFont;
        btn.Cursor = Cursors.Hand;
        btn.MouseEnter += (s, e) => btn.BackColor = DangerHover;
        btn.MouseLeave += (s, e) => btn.BackColor = Danger;
    }

    public static void EstilizarDataGridView(DataGridView grid)
    {
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = BorderColor;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowTemplate.Height = 40;
        grid.Font = BodyFont;

        // Cabecera
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        grid.ColumnHeadersDefaultCellStyle.Font = SectionFont;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.ColumnHeadersHeight = 42;

        // Filas
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = PrimaryLight;
        grid.DefaultCellStyle.SelectionForeColor = Primary;
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
    }
}

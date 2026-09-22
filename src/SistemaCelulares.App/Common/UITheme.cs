using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SistemaCelulares.App.Common;

public static class UITheme
{
    // ==========================================
    // Paleta de Colores Moderna (Mockup CellCenter RD)
    // ==========================================
    public static readonly Color Surface1 = Color.FromArgb(255, 255, 255);       // #ffffff (Blanco puro)
    public static readonly Color Surface2 = Color.FromArgb(246, 247, 249);       // #f6f7f9 (Gris suave fondos)
    public static readonly Color AppBg = Color.FromArgb(236, 238, 241);          // #eceef1 (Fondo exterior)
    
    public static readonly Color Border = Color.FromArgb(229, 231, 235);         // #e5e7eb (Bordes suaves)
    public static readonly Color BorderStrong = Color.FromArgb(209, 213, 219);   // #d1d5db (Bordes marcados)
    public static readonly Color BorderAccent = Color.FromArgb(147, 197, 253);   // #93c5fd
    public static readonly Color BorderDanger = Color.FromArgb(254, 202, 202);   // #fecaca

    public static readonly Color TextPrimary = Color.FromArgb(17, 24, 39);       // #111827 (Texto principal oscuro)
    public static readonly Color TextSecondary = Color.FromArgb(107, 114, 128);  // #6b7280 (Texto secundario gris)
    public static readonly Color TextMuted = Color.FromArgb(156, 163, 175);      // #9ca3af (Texto atenuado)

    public static readonly Color Primary = Color.FromArgb(37, 99, 235);          // #2563eb (Azul Real Accent)
    public static readonly Color PrimaryHover = Color.FromArgb(29, 78, 216);     // #1d4ed8
    public static readonly Color Pro = Color.FromArgb(124, 58, 237);             // #7c3aed (Púrpura Pro / Técnico)
    public static readonly Color ProHover = Color.FromArgb(109, 40, 217);        // #6d28d9
    public static readonly Color Success = Color.FromArgb(21, 128, 61);          // #15803d (Verde éxito)
    public static readonly Color Warning = Color.FromArgb(180, 83, 9);           // #b45309 (Ámbar / Naranja)
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);           // #dc2626 (Rojo peligro)
    public static readonly Color DangerHover = Color.FromArgb(185, 28, 28);      // #b91c1c

    public static readonly Color BgAccent = Color.FromArgb(239, 246, 255);       // #eff6ff (Fondo azul claro)
    public static readonly Color BgPro = Color.FromArgb(245, 243, 255);          // #f5f3ff (Fondo púrpura claro)
    public static readonly Color BgSuccess = Color.FromArgb(240, 253, 244);      // #f0fdf4 (Fondo verde claro)
    public static readonly Color BgWarning = Color.FromArgb(255, 251, 235);      // #fffbeb (Fondo amarillo claro)
    public static readonly Color BgDanger = Color.FromArgb(254, 242, 242);       // #fef2f2 (Fondo rojo claro)

    // Aliases para compatibilidad con código existente
    public static readonly Color DarkBg = TextPrimary;
    public static readonly Color PrimaryLight = BgAccent;
    public static readonly Color SidebarBg = Surface1;
    public static readonly Color CardBg = Surface1;
    public static readonly Color BorderColor = Border;
    public static readonly Color SuccessHover = Color.FromArgb(16, 185, 129);
    public static readonly Color TextLight = Surface1;

    // ==========================================
    // Tipografías
    // ==========================================
    public static readonly Font TitleFont = new("Segoe UI", 15F, FontStyle.Bold);
    public static readonly Font SubtitleFont = new("Segoe UI", 11.5F, FontStyle.Bold);
    public static readonly Font SectionFont = new("Segoe UI", 10F, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 9.5F, FontStyle.Regular);
    public static readonly Font BodyBoldFont = new("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font SmallFont = new("Segoe UI", 8.5F, FontStyle.Regular);
    public static readonly Font SmallBoldFont = new("Segoe UI", 8.5F, FontStyle.Bold);
    public static readonly Font BigNumberFont = new("Segoe UI", 18F, FontStyle.Bold);
    public static readonly Font BadgeFont = new("Segoe UI", 8F, FontStyle.Bold);

    // ==========================================
    // Helpers de Estilizado
    // ==========================================
    public static void AplicarBotonPrimario(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = Primary;
        btn.ForeColor = Color.White;
        btn.Font = BodyBoldFont;
        btn.Cursor = Cursors.Hand;
        btn.MouseEnter += (s, e) => btn.BackColor = PrimaryHover;
        btn.MouseLeave += (s, e) => btn.BackColor = Primary;
    }

    public static void AplicarBotonPro(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = Pro;
        btn.ForeColor = Color.White;
        btn.Font = BodyBoldFont;
        btn.Cursor = Cursors.Hand;
        btn.MouseEnter += (s, e) => btn.BackColor = ProHover;
        btn.MouseLeave += (s, e) => btn.BackColor = Pro;
    }

    public static void AplicarBotonSecundario(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = Border;
        btn.FlatAppearance.BorderSize = 1;
        btn.BackColor = Surface1;
        btn.ForeColor = TextPrimary;
        btn.Font = BodyFont;
        btn.Cursor = Cursors.Hand;
        btn.MouseEnter += (s, e) => btn.BackColor = Surface2;
        btn.MouseLeave += (s, e) => btn.BackColor = Surface1;
    }

    public static void AplicarBotonPeligro(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = BorderDanger;
        btn.FlatAppearance.BorderSize = 1;
        btn.BackColor = BgDanger;
        btn.ForeColor = Danger;
        btn.Font = BodyFont;
        btn.Cursor = Cursors.Hand;
        btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(254, 226, 226);
        btn.MouseLeave += (s, e) => btn.BackColor = BgDanger;
    }

    public static void EstilizarDataGridView(DataGridView grid)
    {
        grid.BackgroundColor = Surface1;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Border;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowTemplate.Height = 36;
        grid.Font = BodyFont;

        // Cabecera
        grid.ColumnHeadersVisible = true;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Surface2;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.Font = SectionFont;
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Surface2;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextSecondary;

        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 38;

        // Filas
        grid.DefaultCellStyle.BackColor = Surface1;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = BgAccent;
        grid.DefaultCellStyle.SelectionForeColor = Primary;
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Surface2;
    }
}

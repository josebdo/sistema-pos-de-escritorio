using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using SistemaCelulares.App.Common;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Forms;

public class VerificadorPrecioModalForm : Form
{
    private readonly IProductoService _productoService;
    private readonly ICategoriaService _categoriaService;
    private readonly SesionUsuario _sesion;

    private TextBox _txtCodigoEscaneo = null!;
    private Button _btnBuscar = null!;

    // Tarjeta de Información del Producto
    private Panel _panelResultado = null!;
    private Label _lblNombreProducto = null!;
    private Label _lblCategoriaSku = null!;
    private Label _lblPrecioVenta = null!;
    private Label _lblStockInfo = null!;
    private Label _lblBadgeStock = null!;
    private Label _lblCodigoBarras = null!;
    private Label _lblPrecioCosto = null!;

    // Mensaje de no encontrado
    private Panel _panelNoEncontrado = null!;
    private Label _lblMensajeNoEncontrado = null!;
    private Button _btnRegistrarNuevo = null!;

    private string _ultimoCodigoEscaneado = string.Empty;

    public VerificadorPrecioModalForm(
        IProductoService productoService,
        ICategoriaService categoriaService,
        SesionUsuario sesion)
    {
        _productoService = productoService;
        _categoriaService = categoriaService;
        _sesion = sesion;

        InitializeCustomComponents();
        Shown += (s, e) => _txtCodigoEscaneo.Focus();
    }

    private void InitializeCustomComponents()
    {
        Text = "Consultor de Precios y Disponibilidad en Tiempo Real";
        Size = new Size(680, 580);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.AppBg;
        Font = UITheme.BodyFont;

        // Top Scanner Input Card
        var scanCard = new Panel
        {
            Location = new Point(24, 18),
            Size = new Size(616, 115),
            BackColor = Color.White,
            Padding = new Padding(20)
        };
        Controls.Add(scanCard);

        var lblInstruccion = new Label
        {
            Text = "📸 Escanee con el lector USB o digite el Código de Barras / SKU:",
            Font = UITheme.SectionFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(20, 15),
            AutoSize = true
        };
        scanCard.Controls.Add(lblInstruccion);

        _txtCodigoEscaneo = new TextBox
        {
            Location = new Point(20, 48),
            Size = new Size(420, 36),
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            PlaceholderText = "Esperando lectura de código de barras..."
        };
        scanCard.Controls.Add(_txtCodigoEscaneo);

        _btnBuscar = new Button
        {
            Text = "🔍 Buscar",
            Location = new Point(450, 47),
            Size = new Size(145, 38)
        };
        UITheme.AplicarBotonPrimario(_btnBuscar);
        _btnBuscar.Click += async (s, e) => await ProcesarLecturaAsync(_txtCodigoEscaneo.Text);
        scanCard.Controls.Add(_btnBuscar);

        // Configurar helper para escaneo rápido con tecla Enter
        BarcodeScannerHelper.ConfigurarParaEscaneo(_txtCodigoEscaneo, async (codigo) =>
        {
            await ProcesarLecturaAsync(codigo);
        });

        // Contenedor de Resultado (Encontrado)
        _panelResultado = new Panel
        {
            Location = new Point(24, 145),
            Size = new Size(616, 320),
            BackColor = Color.White,
            Padding = new Padding(25),
            Visible = false
        };
        Controls.Add(_panelResultado);

        var panelHeaderProd = new Panel { Dock = DockStyle.Top, Height = 70 };
        _lblNombreProducto = new Label
        {
            Text = "Nombre del Producto",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UITheme.DarkBg,
            Dock = DockStyle.Top,
            AutoSize = true
        };
        panelHeaderProd.Controls.Add(_lblNombreProducto);

        _lblCategoriaSku = new Label
        {
            Text = "Categoría: Smartphone | SKU: CEL-0001",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(0, 38),
            AutoSize = true
        };
        panelHeaderProd.Controls.Add(_lblCategoriaSku);
        _panelResultado.Controls.Add(panelHeaderProd);

        // Precios
        var panelPrecios = new Panel
        {
            Location = new Point(25, 95),
            Size = new Size(565, 110),
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(15)
        };
        _panelResultado.Controls.Add(panelPrecios);

        var lblTitPrecio = new Label
        {
            Text = "PRECIO DE VENTA AL PÚBLICO",
            Font = UITheme.SmallFont,
            ForeColor = Color.Gray,
            Location = new Point(15, 10),
            AutoSize = true
        };
        panelPrecios.Controls.Add(lblTitPrecio);

        _lblPrecioVenta = new Label
        {
            Text = "RD$ 0.00",
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            ForeColor = UITheme.Success,
            Location = new Point(12, 32),
            AutoSize = true
        };
        panelPrecios.Controls.Add(_lblPrecioVenta);

        _lblPrecioCosto = new Label
        {
            Text = "Costo: RD$ 0.00",
            Font = UITheme.SmallFont,
            ForeColor = UITheme.TextMuted,
            Location = new Point(360, 48),
            AutoSize = true,
            Visible = _sesion.EsAdmin
        };
        panelPrecios.Controls.Add(_lblPrecioCosto);

        // Stock y Código de barras
        var panelStock = new Panel
        {
            Location = new Point(25, 215),
            Size = new Size(565, 80),
            Padding = new Padding(5)
        };
        _panelResultado.Controls.Add(panelStock);

        _lblStockInfo = new Label
        {
            Text = "Stock disponible: 8 unidades (Mínimo: 2)",
            Font = UITheme.SectionFont,
            ForeColor = UITheme.DarkBg,
            Location = new Point(10, 10),
            AutoSize = true
        };
        panelStock.Controls.Add(_lblStockInfo);

        _lblBadgeStock = new Label
        {
            Text = " DISPONIBLE ",
            Font = UITheme.BadgeFont,
            ForeColor = Color.White,
            BackColor = UITheme.Success,
            Location = new Point(10, 40),
            AutoSize = true,
            Padding = new Padding(4, 2, 4, 2)
        };
        panelStock.Controls.Add(_lblBadgeStock);

        _lblCodigoBarras = new Label
        {
            Text = "Código de barras: 7421001234567",
            Font = UITheme.SmallFont,
            ForeColor = Color.Gray,
            Location = new Point(260, 40),
            AutoSize = true
        };
        panelStock.Controls.Add(_lblCodigoBarras);

        // Panel de No Encontrado
        _panelNoEncontrado = new Panel
        {
            Location = new Point(24, 145),
            Size = new Size(616, 260),
            BackColor = Color.White,
            Padding = new Padding(30),
            Visible = false
        };
        Controls.Add(_panelNoEncontrado);

        var lblIconoError = new Label
        {
            Text = "⚠️",
            Font = new Font("Segoe UI", 32F),
            Location = new Point(265, 20),
            Size = new Size(80, 60),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _panelNoEncontrado.Controls.Add(lblIconoError);

        _lblMensajeNoEncontrado = new Label
        {
            Text = "El código de barras escaneado no coincide con ningún producto del inventario.",
            Font = UITheme.SectionFont,
            ForeColor = UITheme.Danger,
            Location = new Point(30, 90),
            Size = new Size(550, 45),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _panelNoEncontrado.Controls.Add(_lblMensajeNoEncontrado);

        _btnRegistrarNuevo = new Button
        {
            Text = "➕ Registrar Nuevo Producto con este Código",
            Location = new Point(130, 155),
            Size = new Size(350, 42)
        };
        UITheme.AplicarBotonPrimario(_btnRegistrarNuevo);
        _btnRegistrarNuevo.Click += async (s, e) => await AbrirRegistroConCodigoAsync();
        _panelNoEncontrado.Controls.Add(_btnRegistrarNuevo);

        // Botón Cerrar
        var btnCerrar = new Button
        {
            Text = "Cerrar",
            Location = new Point(540, 480),
            Size = new Size(100, 34),
            DialogResult = DialogResult.OK
        };
        UITheme.AplicarBotonSecundario(btnCerrar);
        Controls.Add(btnCerrar);
    }

    private async Task ProcesarLecturaAsync(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return;

        _ultimoCodigoEscaneado = codigo.Trim();

        try
        {
            var producto = await _productoService.BuscarPorCodigoBarrasOSkuAsync(_ultimoCodigoEscaneado);

            if (producto != null)
            {
                MostrarProducto(producto);
            }
            else
            {
                MostrarNoEncontrado(_ultimoCodigoEscaneado);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al buscar producto: {ex.Message}", "Error de Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _txtCodigoEscaneo.SelectAll();
            _txtCodigoEscaneo.Focus();
        }
    }

    private void MostrarProducto(Producto p)
    {
        _panelNoEncontrado.Visible = false;
        _panelResultado.Visible = true;

        _lblNombreProducto.Text = p.Nombre;
        _lblCategoriaSku.Text = $"Categoría: {p.Categoria?.Nombre ?? "General"} | SKU: {p.Sku}";
        _lblPrecioVenta.Text = p.PrecioVenta.ToString("C2", new CultureInfo("es-DO"));
        _lblPrecioCosto.Text = $"Costo: {p.PrecioCosto.ToString("C2", new CultureInfo("es-DO"))}";
        _lblCodigoBarras.Text = $"Código de barras: {p.CodigoBarras ?? "No asignado"}";

        _lblStockInfo.Text = $"Stock disponible: {p.StockActual} unidades (Mínimo: {p.CantidadMinima})";

        if (p.StockActual == 0)
        {
            _lblBadgeStock.Text = " AGOTADO ";
            _lblBadgeStock.BackColor = UITheme.Danger;
        }
        else if (p.StockActual <= p.CantidadMinima)
        {
            _lblBadgeStock.Text = " STOCK BAJO ";
            _lblBadgeStock.BackColor = UITheme.Warning;
        }
        else
        {
            _lblBadgeStock.Text = " DISPONIBLE ";
            _lblBadgeStock.BackColor = UITheme.Success;
        }
    }

    private void MostrarNoEncontrado(string codigo)
    {
        _panelResultado.Visible = false;
        _panelNoEncontrado.Visible = true;
        _lblMensajeNoEncontrado.Text = $"No se encontró ningún producto con el código o SKU: '{codigo}'.";
        _btnRegistrarNuevo.Visible = _sesion.TienePermiso(Permisos.ProductosCrear);
    }

    private async Task AbrirRegistroConCodigoAsync()
    {
        using var modal = new ProductoModalForm(_productoService, _categoriaService, productoId: null, codigoBarrasInicial: _ultimoCodigoEscaneado);
        if (modal.ShowDialog(this) == DialogResult.OK)
        {
            // Volver a consultar el producto recién creado
            await ProcesarLecturaAsync(_ultimoCodigoEscaneado);
        }
    }
}

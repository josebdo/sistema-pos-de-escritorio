using System.Drawing;
using System.Drawing.Printing;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.App.Common;

public static class TicketRenderer
{
    public static Bitmap RenderizarTicketBitmap(TicketVentaDto ticket, int anchoPixels = 400)
    {
        // Calcular altura estimada
        int alturaEstimada = 450 + (ticket.Items.Count * 30);
        var bmp = new Bitmap(anchoPixels, alturaEstimada);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);

        using var fontTitulo = new Font("Segoe UI", 12, FontStyle.Bold);
        using var fontHeader = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var fontTotal = new Font("Segoe UI", 14, FontStyle.Bold);
        using var fontPequena = new Font("Segoe UI", 7.5f, FontStyle.Italic);

        using var brush = new SolidBrush(Color.Black);
        using var pen = new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

        float y = 15;
        var centerFormat = new StringFormat { Alignment = StringAlignment.Center };
        var rightFormat = new StringFormat { Alignment = StringAlignment.Far };

        // Encabezado
        g.DrawString(ticket.NombreEmpresa, fontTitulo, brush, new RectangleF(0, y, anchoPixels, 25), centerFormat);
        y += 24;
        g.DrawString($"RNC: {ticket.RncEmpresa}", fontBold, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);
        y += 18;
        g.DrawString(ticket.DireccionEmpresa, fontHeader, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);
        y += 18;
        g.DrawString($"Tel: {ticket.TelefonoEmpresa}", fontHeader, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);
        y += 24;

        g.DrawLine(pen, 15, y, anchoPixels - 15, y);
        y += 8;

        // Tipo de Comprobante y NCF
        g.DrawString(ticket.TipoComprobanteDescripcion, fontBold, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);
        y += 18;

        if (!string.IsNullOrEmpty(ticket.Ncf))
        {
            g.DrawString($"NCF: {ticket.Ncf}", fontBold, brush, new RectangleF(0, y, anchoPixels, 20), centerFormat);
            y += 20;
        }

        g.DrawString($"Factura No: {ticket.NumeroFactura}", fontBold, brush, 15, y);
        g.DrawString($"Fecha: {ticket.Fecha:dd/MM/yyyy HH:mm}", fontHeader, brush, new RectangleF(0, y, anchoPixels - 15, 18), rightFormat);
        y += 18;

        g.DrawString($"Cajero: {ticket.CajeroNombre} (Caja {ticket.CajaId})", fontHeader, brush, 15, y);
        y += 18;

        if (!string.IsNullOrEmpty(ticket.ClienteNombre))
        {
            g.DrawString($"Cliente: {ticket.ClienteNombre}", fontHeader, brush, 15, y);
            y += 16;
        }

        if (!string.IsNullOrEmpty(ticket.ClienteRnc))
        {
            g.DrawString($"RNC/Cédula: {ticket.ClienteRnc}", fontHeader, brush, 15, y);
            y += 16;
        }

        y += 6;
        g.DrawLine(pen, 15, y, anchoPixels - 15, y);
        y += 8;

        // Cabecera de Items
        g.DrawString("CANT  DESCRIPCIÓN", fontBold, brush, 15, y);
        g.DrawString("TOTAL", fontBold, brush, new RectangleF(0, y, anchoPixels - 15, 18), rightFormat);
        y += 20;

        foreach (var it in ticket.Items)
        {
            string lineaDesc = $"{it.Cantidad}x  {it.NombreProducto}";
            if (lineaDesc.Length > 28) lineaDesc = lineaDesc.Substring(0, 25) + "...";

            g.DrawString(lineaDesc, fontHeader, brush, 15, y);
            g.DrawString($"RD$ {it.TotalBruto:N2}", fontHeader, brush, new RectangleF(0, y, anchoPixels - 15, 18), rightFormat);
            y += 18;
        }

        y += 6;
        g.DrawLine(pen, 15, y, anchoPixels - 15, y);
        y += 8;

        // Totales
        g.DrawString("Subtotal:", fontHeader, brush, 15, y);
        g.DrawString($"RD$ {ticket.Subtotal:N2}", fontHeader, brush, new RectangleF(0, y, anchoPixels - 15, 18), rightFormat);
        y += 18;

        g.DrawString("ITBIS (18%):", fontHeader, brush, 15, y);
        g.DrawString($"RD$ {ticket.Itbis:N2}", fontHeader, brush, new RectangleF(0, y, anchoPixels - 15, 18), rightFormat);
        y += 18;

        if (ticket.Descuento > 0)
        {
            g.DrawString("Descuento:", fontHeader, brush, 15, y);
            g.DrawString($"-RD$ {ticket.Descuento:N2}", fontHeader, brush, new RectangleF(0, y, anchoPixels - 15, 18), rightFormat);
            y += 18;
        }

        g.DrawString("TOTAL A PAGAR:", fontTotal, brush, 15, y);
        g.DrawString($"RD$ {ticket.Total:N2}", fontTotal, brush, new RectangleF(0, y, anchoPixels - 15, 26), rightFormat);
        y += 30;

        g.DrawLine(pen, 15, y, anchoPixels - 15, y);
        y += 8;

        // Pagos y Vuelto
        g.DrawString("Método de Pago:", fontBold, brush, 15, y);
        g.DrawString(ticket.MetodosPagoTexto, fontHeader, brush, 15, y + 16);
        y += 34;

        g.DrawString("Monto Entregado:", fontHeader, brush, 15, y);
        g.DrawString($"RD$ {ticket.MontoEntregado:N2}", fontHeader, brush, new RectangleF(0, y, anchoPixels - 15, 18), rightFormat);
        y += 18;

        g.DrawString("Devuelta / Cambio:", fontBold, brush, 15, y);
        g.DrawString($"RD$ {ticket.MontoVuelto:N2}", fontBold, brush, new RectangleF(0, y, anchoPixels - 15, 18), rightFormat);
        y += 28;

        // Pie
        g.DrawLine(pen, 15, y, anchoPixels - 15, y);
        y += 12;
        g.DrawString("¡Gracias por su compra!", fontBold, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);
        y += 18;
        g.DrawString("Conserve este ticket para fines de garantía o cambio.", fontPequena, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);

        return bmp;
    }

    public static Bitmap RenderizarTicketReparacionBitmap(TicketReparacionDto ticket, int anchoPixels = 400)
    {
        int alturaEstimada = 550;
        var bmp = new Bitmap(anchoPixels, alturaEstimada);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);

        using var fontTitulo = new Font("Segoe UI", 12, FontStyle.Bold);
        using var fontHeader = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var fontTotal = new Font("Segoe UI", 13, FontStyle.Bold);
        using var fontPequena = new Font("Segoe UI", 7.5f, FontStyle.Italic);

        using var brush = new SolidBrush(Color.Black);
        using var pen = new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

        float y = 15;
        var centerFormat = new StringFormat { Alignment = StringAlignment.Center };
        var rightFormat = new StringFormat { Alignment = StringAlignment.Far };

        // Encabezado
        g.DrawString(ticket.NombreEmpresa, fontTitulo, brush, new RectangleF(0, y, anchoPixels, 24), centerFormat);
        y += 22;
        g.DrawString($"RNC: {ticket.RncEmpresa}", fontBold, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);
        y += 18;
        g.DrawString(ticket.DireccionEmpresa, fontHeader, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);
        y += 18;
        g.DrawString($"Tel: {ticket.TelefonoEmpresa}", fontHeader, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);
        y += 24;

        g.DrawLine(pen, 15, y, anchoPixels - 15, y);
        y += 8;

        // Título del Comprobante
        g.DrawString("FACTURA / COMPROBANTE DE REPARACIÓN", fontBold, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);
        y += 20;

        g.DrawString($"Orden No: {ticket.NumeroOrden}", fontBold, brush, 15, y);
        g.DrawString($"Fecha: {ticket.Fecha:dd/MM/yyyy HH:mm}", fontHeader, brush, new RectangleF(0, y, anchoPixels - 15, 18), rightFormat);
        y += 18;

        g.DrawString($"Técnico / Entrega: {ticket.TecnicoEntrega}", fontHeader, brush, 15, y);
        y += 18;

        if (!string.IsNullOrEmpty(ticket.ClienteNombre))
        {
            g.DrawString($"Cliente: {ticket.ClienteNombre}", fontHeader, brush, 15, y);
            y += 16;
        }

        if (!string.IsNullOrEmpty(ticket.ClienteTelefono))
        {
            g.DrawString($"Teléfono: {ticket.ClienteTelefono}", fontHeader, brush, 15, y);
            y += 16;
        }

        y += 4;
        g.DrawLine(pen, 15, y, anchoPixels - 15, y);
        y += 8;

        // Datos del Equipo
        g.DrawString("DATOS DEL EQUIPO:", fontBold, brush, 15, y);
        y += 18;
        g.DrawString($"Equipo: {ticket.EquipoMarcaModelo}", fontHeader, brush, 15, y);
        y += 16;
        g.DrawString($"IMEI / Serie: {ticket.ImeiOSerie}", fontHeader, brush, 15, y);
        y += 16;
        g.DrawString($"Problema: {ticket.DescripcionProblema}", fontHeader, brush, 15, y);
        y += 16;

        if (!string.IsNullOrEmpty(ticket.DiagnosticoSolucion))
        {
            g.DrawString($"Diagnóstico: {ticket.DiagnosticoSolucion}", fontHeader, brush, 15, y);
            y += 16;
        }

        y += 6;
        g.DrawLine(pen, 15, y, anchoPixels - 15, y);
        y += 8;

        // Totales y Cobro
        g.DrawString("TOTAL SERVICIO:", fontTotal, brush, 15, y);
        g.DrawString($"RD$ {ticket.MontoTotal:N2}", fontTotal, brush, new RectangleF(0, y, anchoPixels - 15, 24), rightFormat);
        y += 28;

        g.DrawString("Método de Pago:", fontBold, brush, 15, y);
        g.DrawString(ticket.MetodosPagoTexto, fontHeader, brush, 15, y + 16);
        y += 34;

        if (ticket.MontoEntregado > 0)
        {
            g.DrawString("Monto Recibido:", fontHeader, brush, 15, y);
            g.DrawString($"RD$ {ticket.MontoEntregado:N2}", fontHeader, brush, new RectangleF(0, y, anchoPixels - 15, 18), rightFormat);
            y += 18;
        }

        if (ticket.MontoVuelto > 0)
        {
            g.DrawString("Devuelta / Cambio:", fontBold, brush, 15, y);
            g.DrawString($"RD$ {ticket.MontoVuelto:N2}", fontBold, brush, new RectangleF(0, y, anchoPixels - 15, 18), rightFormat);
            y += 22;
        }

        // Pie de página y garantía
        g.DrawLine(pen, 15, y, anchoPixels - 15, y);
        y += 10;
        g.DrawString("Estado: " + ticket.EstadoOrden, fontBold, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);
        y += 18;
        g.DrawString("¡Gracias por confiar en nuestros servicios!", fontBold, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);
        y += 18;
        g.DrawString("Garantía de 30 días en mano de obra. No cubre humedad ni golpes.", fontPequena, brush, new RectangleF(0, y, anchoPixels, 18), centerFormat);

        return bmp;
    }

    public static void ImprimirTicket(TicketVentaDto ticket)
    {
        var pd = new PrintDocument();
        pd.PrintPage += (s, ev) =>
        {
            using var bmp = RenderizarTicketBitmap(ticket, 380);
            ev.Graphics?.DrawImage(bmp, 0, 0);
        };

        try
        {
            pd.Print();
        }
        catch
        {
            // Manejo silencioso si no hay impresora física conectada
        }
    }

    public static void ImprimirTicketReparacion(TicketReparacionDto ticket)
    {
        var pd = new PrintDocument();
        pd.PrintPage += (s, ev) =>
        {
            using var bmp = RenderizarTicketReparacionBitmap(ticket, 380);
            ev.Graphics?.DrawImage(bmp, 0, 0);
        };

        try
        {
            pd.Print();
        }
        catch
        {
            // Manejo silencioso si no hay impresora física conectada
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TallerCapricornio.Models
{
    public class PresupuestoPdf : IDocument
    {
        private readonly Presupuesto _presupuesto;
        private readonly byte[]? _logo;
        private readonly byte[]? _firma;

        // Paleta tomada del diseño.
        private const string Negro = "#1F2024";
        private const string NegroClaro = "#2B2C31";
        private const string Rojo = "#B5121B";
        private const string RojoOscuro = "#7A0C12";
        private const string GrisClaro = "#E6E7EA";
        private const string Borde = "#C8CACF";
        private const string GrisTexto = "#555555";

        private const float Margen = 24;

        private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-AR");

        // Íconos simples en SVG (blancos, para el cuadrado negro de cada tarjeta).
        private const string IconoCliente =
            "<svg viewBox='0 0 24 24' xmlns='http://www.w3.org/2000/svg'>" +
            "<circle cx='12' cy='8' r='4.5' fill='white'/>" +
            "<path d='M3.5 22c0-5.5 4-8 8.5-8s8.5 2.5 8.5 8z' fill='white'/></svg>";

        private const string IconoAuto =
            "<svg viewBox='0 0 24 24' xmlns='http://www.w3.org/2000/svg'>" +
            "<path d='M2 15l2.2-6.5C4.6 7.5 5.3 7 6.3 7h11.4c1 0 1.7.5 2.1 1.5L22 15v4H2z' fill='white'/>" +
            "<circle cx='7' cy='18.5' r='2.4' fill='white' stroke='#1F2024' stroke-width='1.2'/>" +
            "<circle cx='17' cy='18.5' r='2.4' fill='white' stroke='#1F2024' stroke-width='1.2'/></svg>";

        private const string IconoLlave =
            "<svg viewBox='0 0 24 24' xmlns='http://www.w3.org/2000/svg'>" +
            "<path d='M5 19L14 10' stroke='white' stroke-width='3.6' stroke-linecap='round'/>" +
            "<circle cx='16.5' cy='7.5' r='4.8' fill='white'/>" +
            "<path d='M16.5 7.5L21 3' stroke='#1F2024' stroke-width='2.6'/></svg>";

        private const string IconoCaja =
            "<svg viewBox='0 0 24 24' xmlns='http://www.w3.org/2000/svg'>" +
            "<path d='M12 2l9 5v10l-9 5-9-5V7z' fill='none' stroke='white' stroke-width='2' stroke-linejoin='round'/>" +
            "<path d='M3 7l9 5 9-5M12 12v10' fill='none' stroke='white' stroke-width='2' stroke-linejoin='round'/></svg>";

        private const string IconoEngranaje =
            "<svg viewBox='0 0 24 24' xmlns='http://www.w3.org/2000/svg'>" +
            "<circle cx='12' cy='12' r='8.5' fill='none' stroke='white' stroke-width='4' stroke-dasharray='3.3 3.3'/>" +
            "<circle cx='12' cy='12' r='6.5' fill='white'/>" +
            "<circle cx='12' cy='12' r='2.6' fill='#1F2024'/></svg>";

        public PresupuestoPdf(
            Presupuesto presupuesto,
            byte[]? logo = null,
            byte[]? firma = null)
        {
            _presupuesto = presupuesto;
            _logo = logo;
            _firma = firma;

            QuestPDF.Settings.License = LicenseType.Community;
        }

        public DocumentMetadata GetMetadata()
        {
            return new DocumentMetadata
            {
                Title = $"Presupuesto {_presupuesto.Numero}",
                Author = "Taller Capricornio",
                Subject = "Presupuesto de reparación de vehículo",
                Creator = "Taller Capricornio"
            };
        }

        // Genera el PDF y devuelve sus bytes para descargarlo.
        public byte[] GenerarPdf()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return this.GeneratePdf();
        }

        public void Compose(IDocumentContainer container)
        {
            // ===== Primera página: presupuesto =====
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0); // el encabezado y el pie van de borde a borde

                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Negro));

                // Contenido: encabezado, título y secciones que crecen con los datos.
                page.Content().Column(col =>
                {
                    col.Item().Element(Encabezado);
                    col.Item().Element(FranjaTitulo);

                    col.Item()
                        .PaddingHorizontal(Margen)
                        .PaddingTop(8)
                        .Column(cuerpo =>
                        {
                            cuerpo.Spacing(8);

                            cuerpo.Item().Element(FechaYFactura);
                            cuerpo.Item().Element(TarjetaCliente);
                            cuerpo.Item().Element(TarjetaVehiculo);

                            cuerpo.Item().Element(c => Tarjeta(
                                c,
                                "DETALLE DE REPARACIÓN Y/O PROVISIÓN",
                                IconoLlave,
                                body => ListaVinetas(body, _presupuesto.DetalleReparacion)));

                            cuerpo.Item().Element(c => Tarjeta(
                                c,
                                "REPUESTOS A PROVEER",
                                IconoCaja,
                                body => ListaVinetas(body, _presupuesto.RepuestosAProveer)));
                        });
                });

                // Pie: importes, total, condiciones y firma siempre abajo de la hoja.
                page.Footer().Column(pie =>
                {
                    pie.Item()
                        .PaddingHorizontal(Margen)
                        .PaddingBottom(10)
                        .Element(BloqueInferior);

                    pie.Item().Element(PieNegro);
                });
            });

            // ===== Páginas siguientes: una fotografía por página =====
            if (_presupuesto.Imagenes != null)
            {
                var imagenes = _presupuesto.Imagenes
                    .Where(i =>
                        i.Contenido != null &&
                        i.Contenido.Length > 0)
                    .OrderBy(i => i.Tipo)
                    .ThenBy(i => i.Id)
                    .ToList();

                foreach (var imagen in imagenes)
                {
                    var contenidoImagen = imagen.Contenido;
                    var tipoImagen = imagen.Tipo;

                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(15);

                        page.DefaultTextStyle(x =>
                            x.FontSize(10).FontColor(Negro));

                        page.Content().Column(col =>
                        {
                            col.Spacing(10);

                            col.Item()
                                .Background(Negro)
                                .Padding(12)
                                .Row(row =>
                                {
                                    row.RelativeItem().Text(
                                        $"PRESUPUESTO N.º {_presupuesto.Numero}")
                                        .FontSize(14)
                                        .Bold()
                                        .FontColor("#FFFFFF");

                                    row.ConstantItem(125)
                                        .AlignRight()
                                        .Text(NombreTipoImagen(tipoImagen))
                                        .FontSize(9)
                                        .Bold()
                                        .FontColor("#FFFFFF");
                                });

                            col.Item()
                                .AlignCenter()
                                .Text(
                                    $"{_presupuesto.ApellidoNombre} - " +
                                    $"{_presupuesto.Vehiculo} - " +
                                    $"{_presupuesto.Patente}")
                                .FontSize(10)
                                .FontColor(GrisTexto);

                            col.Item()
                                .Extend()
                                .AlignCenter()
                                .AlignMiddle()
                                .Image(contenidoImagen)
                                .FitArea();
                        });
                    });
                }
            }
        }

        // ------------------------------------------------------------------
        // ENCABEZADO Y TÍTULO
        // ------------------------------------------------------------------

        private void Encabezado(IContainer contenedor)
        {
            contenedor.Column(columna =>
            {
                columna.Item()
                    .Background(Negro)
                    .Row(row =>
                    {
                        // Logo pegado al borde izquierdo y a todo el alto del encabezado
                        // (solo en esta primera página). Usar logo-capricornio-encabezado.png:
                        // el borde derecho está difuminado para fundirse con el fondo.
                        row.ConstantItem(330)
                            .Height(96)
                            .Element(c =>
                            {
                                if (_logo != null && _logo.Length > 0)
                                {
                                    c.Image(_logo).FitArea();
                                }
                                else
                                {
                                    c.Text("CAPRICORNIO")
                                        .FontSize(22)
                                        .Bold()
                                        .FontColor("#FFFFFF");
                                }
                            });

                        // Franja roja vertical decorativa
                        row.ConstantItem(5).Background(Rojo);

                        row.ConstantItem(14);

                        row.RelativeItem()
                            .PaddingRight(Margen)
                            .AlignMiddle()
                            .Column(info =>
                            {
                                info.Spacing(3);

                                info.Item()
                                    .BorderBottom(0.6f)
                                    .BorderColor("#8A8B90")
                                    .PaddingBottom(3)
                                    .Text("Gabriel Vidussi Torres")
                                    .FontSize(13)
                                    .Bold()
                                    .FontColor("#FFFFFF");

                                info.Item()
                                    .Text("Joaquín Ferro 1392 · Santa Rosa, La Pampa")
                                    .FontColor("#FFFFFF");

                                info.Item()
                                    .Text("Tel. 02954-388573 · Cel. 2954-517291")
                                    .FontColor("#FFFFFF");

                                info.Item()
                                    .Text("capricorniot50@gmail.com")
                                    .FontColor("#FFFFFF");
                            });
                    });

                // Línea roja bajo el encabezado
                columna.Item().Height(4).Background(Rojo);
            });
        }

        private void FranjaTitulo(IContainer contenedor)
        {
            contenedor
                .PaddingTop(4)
                .Background(NegroClaro)
                .Row(row =>
                {
                    row.RelativeItem()
                        .PaddingLeft(Margen)
                        .PaddingVertical(7)
                        .AlignMiddle()
                        .Text("PRESUPUESTO")
                        .FontSize(24)
                        .Bold()
                        .FontColor("#FFFFFF");

                    row.ConstantItem(200)
                        .Background(Rojo)
                        .AlignMiddle()
                        .AlignCenter()
                        .Text($"Nº {_presupuesto.Numero:D6}")
                        .FontSize(20)
                        .Bold()
                        .FontColor("#FFFFFF");
                });
        }

        private void FechaYFactura(IContainer contenedor)
        {
            contenedor.Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Fecha: ").Bold();
                    t.Span(_presupuesto.FechaCreacion.ToString(
                        "dd / MM / yyyy",
                        CultureInfo.InvariantCulture));
                });

                row.RelativeItem()
                    .AlignRight()
                    .Text("Documento no válido como factura")
                    .FontSize(8);
            });
        }

        // ------------------------------------------------------------------
        // TARJETAS (ícono negro + recuadro con título gris)
        // ------------------------------------------------------------------

        private void Tarjeta(
            IContainer contenedor,
            string titulo,
            string icono,
            Action<IContainer> cuerpo)
        {
            contenedor.Row(row =>
            {
                row.ConstantItem(32)
                    .Height(32)
                    .Background(Negro)
                    .CornerRadius(6)
                    .Padding(7)
                    .Svg(icono);

                row.ConstantItem(6);

                row.RelativeItem()
                    .Border(1)
                    .BorderColor(Borde)
                    .CornerRadius(6)
                    .Column(card =>
                    {
                        card.Item()
                            .Background(GrisClaro)
                            .PaddingVertical(6)
                            .PaddingHorizontal(10)
                            .Text(titulo)
                            .FontSize(9)
                            .Bold();

                        card.Item()
                            .Padding(9)
                            .Element(cuerpo);
                    });
            });
        }

        private void TarjetaCliente(IContainer contenedor)
        {
            Tarjeta(contenedor, "DATOS DEL CLIENTE", IconoCliente, body =>
            {
                body.Column(col =>
                {
                    col.Spacing(5);

                    col.Item().Element(c => CampoLinea(
                        c, "Apellido y Nombre:", _presupuesto.ApellidoNombre, 100));

                    col.Item().Element(c => CampoLinea(
                        c, "Domicilio:", _presupuesto.Domicilio, 100));

                    col.Item().Row(row =>
                    {
                        row.RelativeItem(3).Element(c => CampoLinea(
                            c, "Localidad:", _presupuesto.Localidad, 100));

                        row.ConstantItem(14);

                        row.RelativeItem(2).Element(c => CampoLinea(
                            c, "Tel.:", _presupuesto.Telefono, 28));
                    });
                });
            });
        }

        private void TarjetaVehiculo(IContainer contenedor)
        {
            Tarjeta(contenedor, "DATOS DEL VEHÍCULO", IconoAuto, body =>
            {
                body.Column(col =>
                {
                    col.Spacing(5);

                    col.Item().Element(c => CampoLinea(
                        c, "Vehículo:", _presupuesto.Vehiculo, 100));

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => CampoLinea(
                            c, "Color:", _presupuesto.Color, 100));

                        row.ConstantItem(14);

                        row.RelativeItem().Element(c => CampoLinea(
                            c, "Patente:", _presupuesto.Patente, 50));
                    });
                });
            });
        }

        // Campo con etiqueta a la izquierda y línea inferior (como la planilla).
        private void CampoLinea(
            IContainer contenedor,
            string etiqueta,
            string? valor,
            float anchoEtiqueta)
        {
            contenedor.Row(row =>
            {
                row.ConstantItem(anchoEtiqueta)
                    .AlignBottom()
                    .Text(etiqueta)
                    .FontColor(GrisTexto);

                row.RelativeItem()
                    .BorderBottom(0.7f)
                    .BorderColor(Borde)
                    .PaddingBottom(1)
                    .PaddingLeft(4)
                    .MinHeight(14)
                    .AlignBottom()
                    .Text(valor ?? string.Empty)
                    .FontSize(9.5f)
                    .Bold();
            });
        }

        // ------------------------------------------------------------------
        // LISTAS CON VIÑETAS ROJAS (soportan todas las filas que se carguen)
        // ------------------------------------------------------------------

        // Cada renglón del texto cargado en el formulario es un ítem.
        // Los renglones que terminan en ":" (ej. "PINTURA:") se muestran como subtítulo.
        // Los ítems se acomodan en dos columnas, de izquierda a derecha.
        private void ListaVinetas(IContainer contenedor, string? texto)
        {
            var lineas = Lineas(texto);

            if (lineas.Count == 0)
            {
                contenedor.Text("—").FontColor(GrisTexto);
                return;
            }

            contenedor.Column(col =>
            {
                col.Spacing(4);

                var pendientes = new List<string>();

                void Vaciar()
                {
                    for (int i = 0; i < pendientes.Count; i += 2)
                    {
                        var izquierda = pendientes[i];
                        var derecha = i + 1 < pendientes.Count
                            ? pendientes[i + 1]
                            : null;

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => Vineta(c, izquierda));
                            row.ConstantItem(12);
                            row.RelativeItem().Element(c =>
                            {
                                if (derecha != null)
                                {
                                    Vineta(c, derecha);
                                }
                            });
                        });
                    }

                    pendientes.Clear();
                }

                foreach (var linea in lineas)
                {
                    if (linea.EndsWith(":"))
                    {
                        Vaciar();

                        col.Item()
                            .Text(linea)
                            .Bold()
                            .FontSize(9);
                    }
                    else
                    {
                        pendientes.Add(linea);
                    }
                }

                Vaciar();
            });
        }

        private void Vineta(IContainer contenedor, string texto)
        {
            contenedor.Row(row =>
            {
                row.ConstantItem(11)
                    .PaddingTop(2.5f)
                    .Height(8)
                    .Width(6)
                    .Background(Rojo);

                row.RelativeItem().Text(texto);
            });
        }

        private static List<string> Lineas(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return new List<string>();
            }

            return texto
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().TrimStart('-', '•', '*', ' '))
                .Where(l => l.Length > 0)
                .ToList();
        }

        // ------------------------------------------------------------------
        // BLOQUE INFERIOR: importes + TOTAL + condiciones + firma
        // ------------------------------------------------------------------

        private void BloqueInferior(IContainer contenedor)
        {
            contenedor.Column(col =>
            {
                col.Spacing(10);

                col.Item().Row(row =>
                {
                    row.RelativeItem(1.15f).Element(c => Tarjeta(
                        c,
                        "PARA USO DEL TALLER OPERANTE",
                        IconoEngranaje,
                        body => DetalleImportes(body)));

                    row.ConstantItem(12);

                    row.RelativeItem(1f).Element(CajaTotal);
                });

                col.Item().Element(CondicionesYFirma);
            });
        }

        private void DetalleImportes(IContainer contenedor)
        {
            contenedor.Column(l =>
            {
                l.Spacing(3);

                LineaImporte(l, "Impuestos", _presupuesto.Impuestos);
                LineaImporte(l, "Mano de obra chapa", _presupuesto.ManoObraChapa);
                LineaImporte(l, "Mano de obra pintura", _presupuesto.ManoObraPintura);
                LineaImporte(l, "Bancada", _presupuesto.Bancada);

                if ((_presupuesto.OtrosConceptos ?? 0m) > 0m)
                {
                    LineaImporte(l, "Otros conceptos", _presupuesto.OtrosConceptos);
                }

                LineaImporte(l, "Subtotal", _presupuesto.Subtotal, negrita: true);

                if (_presupuesto.IncluirIVA)
                {
                    LineaImporte(l, "IVA 21%", _presupuesto.ImporteIVA);
                }
                else
                {
                    LineaImporte(l, "IVA", null, textoDerecha: "No incluido");
                }
            });
        }

        private void LineaImporte(
            ColumnDescriptor columna,
            string concepto,
            decimal? importe,
            bool negrita = false,
            string? textoDerecha = null)
        {
            columna.Item()
                .BorderBottom(0.6f)
                .BorderColor(Borde)
                .PaddingBottom(1.5f)
                .DefaultTextStyle(x => negrita ? x.Bold() : x)
                .Row(row =>
                {
                    row.RelativeItem().Text(concepto);

                    if (textoDerecha != null)
                    {
                        row.ConstantItem(90)
                            .AlignRight()
                            .Text(textoDerecha);
                        return;
                    }

                    row.ConstantItem(12).Text("$");

                    row.ConstantItem(78)
                        .AlignRight()
                        .Text(importe.HasValue && (importe.Value > 0m || negrita)
                            ? Numero(importe.Value)
                            : string.Empty);
                });
        }

        private void CajaTotal(IContainer contenedor)
        {
            contenedor.Column(col =>
            {
                col.Item()
                    .Background(Rojo)
                    .Padding(12)
                    .Column(t =>
                    {
                        t.Spacing(8);

                        t.Item()
                            .AlignCenter()
                            .Text("TOTAL")
                            .FontSize(22)
                            .Bold()
                            .FontColor("#FFFFFF");

                        t.Item()
                            .Background("#FFFFFF")
                            .CornerRadius(6)
                            .PaddingVertical(8)
                            .PaddingHorizontal(8)
                            .Text(x =>
                            {
                                x.AlignCenter();

                                x.Span("$ ")
                                    .FontSize(22)
                                    .Bold()
                                    .FontColor(Rojo);

                                x.Span(Numero(_presupuesto.Total))
                                    .FontSize(28)
                                    .Bold()
                                    .FontColor("#000000");
                            });
                    });

                col.Item().Height(16).Background(RojoOscuro);
            });
        }

        private void CondicionesYFirma(IContainer contenedor)
        {
            contenedor.Row(row =>
            {
                row.RelativeItem()
                    .Column(c =>
                    {
                        c.Spacing(2);

                        c.Item()
                            .Text("CONDICIONES")
                            .FontSize(9.5f)
                            .Bold()
                            .FontColor(Rojo);

                        c.Item().Text("Plazo de mantenimiento de este presupuesto: 15 días.");
                        c.Item().Text("Todo imprevisto se cotiza por separado.");
                        c.Item().Text("Este presupuesto no es válido para juicios.").Bold();
                    });

                row.ConstantItem(24);

                row.ConstantItem(210)
                    .Column(f =>
                    {
                        f.Item()
                            .Height(40)
                            .AlignCenter()
                            .AlignBottom()
                            .Element(c =>
                            {
                                if (_firma != null && _firma.Length > 0)
                                {
                                    c.Image(_firma).FitArea();
                                }
                            });

                        f.Item().LineHorizontal(0.8f).LineColor(GrisTexto);

                        f.Item()
                            .PaddingTop(2)
                            .Text("FIRMA:")
                            .FontSize(8)
                            .Bold();
                    });
            });
        }

        private void PieNegro(IContainer contenedor)
        {
            contenedor.Column(col =>
            {
                col.Item().Height(3).Background(Rojo);

                col.Item()
                    .Height(26)
                    .Background(Negro)
                    .AlignMiddle()
                    .AlignCenter()
                    .Text("Documento no válido como factura")
                    .FontSize(8)
                    .FontColor("#FFFFFF");
            });
        }

        // ------------------------------------------------------------------
        // UTILIDADES
        // ------------------------------------------------------------------

        // 2650000 -> "2.650.000" (con decimales solo si los tiene)
        private static string Numero(decimal importe)
        {
            var formato = importe % 1m == 0m ? "#,##0" : "#,##0.00";
            return importe.ToString(formato, Es);
        }

        private static string NombreTipoImagen(int tipo)
        {
            return tipo switch
            {
                1 => "Seguro - Frente",
                2 => "Seguro - Dorso",
                3 => "Estado del vehículo",
                _ => "Fotografía"
            };
        }
    }
}

using System;
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

        private const string Negro = "#202020";
        private const string Rojo = "#B5121B";
        private const string Gris = "#F0F0F0";
        private const string GrisTexto = "#555555";

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
            // Primera página: presupuesto.
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x =>
                    x.FontSize(9).FontColor(Negro));

                page.Content().Column(col =>
                {
                    col.Spacing(9);

                    col.Item().Element(Encabezado);
                    col.Item().Element(FranjaTitulo);
                    col.Item().Element(FechaYFactura);

                    col.Item().Element(c => Seccion(
                        c,
                        "DATOS DEL CLIENTE",
                        new[]
                        {
                            ("Apellido y nombre", _presupuesto.ApellidoNombre),
                            ("Domicilio", _presupuesto.Domicilio),
                            ("Localidad", _presupuesto.Localidad),
                            ("Teléfono", _presupuesto.Telefono)
                        }));

                    col.Item().Element(c => Seccion(
                        c,
                        "DATOS DEL VEHÍCULO",
                        new[]
                        {
                            ("Vehículo", _presupuesto.Vehiculo),
                            ("Color", _presupuesto.Color),
                            ("Patente", _presupuesto.Patente)
                        }));

                    col.Item().Element(c => SeccionTexto(
                        c,
                        "DETALLE DE REPARACIÓN",
                        _presupuesto.DetalleReparacion));

                    col.Item().Element(c => SeccionTexto(
                        c,
                        "REPUESTOS A PROVEER",
                        _presupuesto.RepuestosAProveer));

                    col.Item().Element(c => SeccionTexto(
                        c,
                        "OBSERVACIONES",
                        _presupuesto.Observaciones));

                    col.Item().Element(Importes);
                    col.Item().Element(CondicionesYFirma);
                });
            });

            // Páginas siguientes: una fotografía por página.
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

        private void Encabezado(IContainer contenedor)
        {
            contenedor
                .Background(Negro)
                .Padding(12)
                .Row(row =>
                {
                    row.ConstantItem(125)
                        .Height(80)
                        .AlignCenter()
                        .AlignMiddle()
                        .Element(c =>
                        {
                            if (_logo != null && _logo.Length > 0)
                            {
                                c.Image(_logo).FitArea();
                            }
                            else
                            {
                                c.AlignCenter()
                                    .AlignMiddle()
                                    .Text("CAPRICORNIO")
                                    .FontSize(14)
                                    .Bold()
                                    .FontColor("#FFFFFF");
                            }
                        });

                    row.ConstantItem(12);

                    row.RelativeItem()
                        .AlignMiddle()
                        .Column(col =>
                        {
                            col.Spacing(4);

                            col.Item()
                                .Text("TALLER CAPRICORNIO")
                                .FontSize(16)
                                .Bold()
                                .FontColor("#FFFFFF");

                            col.Item()
                                .Text("Gabriel Vidussi Torres")
                                .FontSize(10)
                                .FontColor("#FFFFFF");

                            col.Item()
                                .Text("Joaquín Ferro 1392 - Santa Rosa")
                                .FontSize(9)
                                .FontColor("#FFFFFF");

                            col.Item()
                                .Text("Tel.: 02954-388573 / 2954-517291")
                                .FontSize(9)
                                .FontColor("#FFFFFF");

                            col.Item()
                                .Text("capricorniot50@gmail.com")
                                .FontSize(9)
                                .FontColor("#FFFFFF");
                        });
                });
        }

        private void FranjaTitulo(IContainer contenedor)
        {
            contenedor.Row(row =>
            {
                row.RelativeItem()
                    .Background(Gris)
                    .Padding(10)
                    .AlignMiddle()
                    .Text("PRESUPUESTO")
                    .FontSize(20)
                    .Bold()
                    .FontColor(Negro);

                row.ConstantItem(120)
                    .Background(Rojo)
                    .Padding(10)
                    .AlignCenter()
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Item()
                            .Text("N.º")
                            .FontSize(10)
                            .FontColor("#FFFFFF");

                        col.Item()
                            .Text(_presupuesto.Numero.ToString())
                            .FontSize(22)
                            .Bold()
                            .FontColor("#FFFFFF");
                    });
            });
        }

        private void FechaYFactura(IContainer contenedor)
        {
            contenedor.Row(row =>
            {
                row.RelativeItem()
                    .AlignMiddle()
                    .Text($"Fecha: {_presupuesto.FechaCreacion:dd/MM/yyyy}")
                    .FontSize(10)
                    .Bold();

                row.RelativeItem()
                    .AlignRight()
                    .AlignMiddle()
                    .Text("Documento no válido como factura")
                    .FontSize(8)
                    .Italic()
                    .FontColor(GrisTexto);
            });
        }

        private void Seccion(
            IContainer contenedor,
            string titulo,
            (string Etiqueta, string Valor)[] campos)
        {
            contenedor.Column(col =>
            {
                col.Spacing(5);

                col.Item()
                    .Background(Negro)
                    .PaddingVertical(6)
                    .PaddingHorizontal(9)
                    .Text(titulo)
                    .FontSize(10)
                    .Bold()
                    .FontColor("#FFFFFF");

                col.Item()
                    .Border(1)
                    .BorderColor("#D0D0D0")
                    .Padding(8)
                    .Column(datos =>
                    {
                        datos.Spacing(6);

                        foreach (var campo in campos)
                        {
                            datos.Item().Row(row =>
                            {
                                row.ConstantItem(105)
                                    .Text(campo.Etiqueta + ":")
                                    .Bold()
                                    .FontColor(GrisTexto);

                                row.RelativeItem()
                                    .Text(campo.Valor ?? "—");
                            });
                        }
                    });
            });
        }

        private void SeccionTexto(
            IContainer contenedor,
            string titulo,
            string? contenido)
        {
            contenedor.Column(col =>
            {
                col.Spacing(4);

                col.Item()
                    .Background(Negro)
                    .PaddingVertical(6)
                    .PaddingHorizontal(9)
                    .Text(titulo)
                    .FontSize(10)
                    .Bold()
                    .FontColor("#FFFFFF");

                col.Item()
                    .Border(1)
                    .BorderColor("#D0D0D0")
                    .Padding(8)
                    .MinHeight(30)
                    .Text(string.IsNullOrWhiteSpace(contenido)
                        ? "—"
                        : contenido)
                    .FontSize(9);
            });
        }

        private void Importes(IContainer contenedor)
        {
            contenedor.Column(col =>
            {
                col.Spacing(5);

                col.Item()
                    .Background(Negro)
                    .PaddingVertical(6)
                    .PaddingHorizontal(9)
                    .Text("USO EXCLUSIVO DEL TALLER")
                    .FontSize(10)
                    .Bold()
                    .FontColor("#FFFFFF");

                col.Item().Row(row =>
                {
                    row.RelativeItem(1.3f)
                        .Border(1)
                        .BorderColor("#D0D0D0")
                        .Padding(8)
                        .Column(detalle =>
                        {
                            detalle.Spacing(7);

                            LineaImporte(
                                detalle, "Impuestos",
                                _presupuesto.Impuestos);

                            LineaImporte(
                                detalle, "Mano de obra de chapa",
                                _presupuesto.ManoObraChapa);

                            LineaImporte(
                                detalle, "Mano de obra de pintura",
                                _presupuesto.ManoObraPintura);

                            LineaImporte(
                                detalle, "Bancada",
                                _presupuesto.Bancada);

                            LineaImporte(
                                detalle, "Otros conceptos",
                                _presupuesto.OtrosConceptos);

                            detalle.Item()
                                .LineHorizontal(1)
                                .LineColor("#D0D0D0");

                            detalle.Item().Row(r =>
                            {
                                r.RelativeItem()
                                    .Text("Subtotal")
                                    .Bold();

                                r.ConstantItem(100)
                                    .AlignRight()
                                    .Text(Formatear(_presupuesto.Subtotal))
                                    .Bold();
                            });

                            if (_presupuesto.IncluirIVA)
                            {
                                detalle.Item().Row(r =>
                                {
                                    r.RelativeItem()
                                        .Text("IVA (21%)");

                                    r.ConstantItem(100)
                                        .AlignRight()
                                        .Text(Formatear(
                                            _presupuesto.ImporteIVA));
                                });
                            }
                        });

                    row.ConstantItem(10);

                    row.RelativeItem()
                        .Background(Rojo)
                        .Padding(12)
                        .AlignCenter()
                        .AlignMiddle()
                        .Column(total =>
                        {
                            total.Spacing(8);

                            total.Item()
                                .AlignCenter()
                                .Text("TOTAL")
                                .FontSize(13)
                                .Bold()
                                .FontColor("#FFFFFF");

                            total.Item()
                                .AlignCenter()
                                .Text(Formatear(_presupuesto.Total))
                                .FontSize(19)
                                .Bold()
                                .FontColor("#FFFFFF");
                        });
                });
            });
        }

        private void LineaImporte(
            ColumnDescriptor columna,
            string concepto,
            decimal? importe)
        {
            columna.Item().Row(row =>
            {
                row.RelativeItem()
                    .Text(concepto);

                row.ConstantItem(100)
                    .AlignRight()
                    .Text(Formatear(importe ?? 0m));
            });
        }

        private void CondicionesYFirma(IContainer contenedor)
        {
            contenedor.Column(col =>
            {
                col.Spacing(7);

                col.Item()
                    .Background(Gris)
                    .Padding(8)
                    .Column(condiciones =>
                    {
                        condiciones.Spacing(4);

                        condiciones.Item()
                            .Text("CONDICIONES")
                            .FontSize(9)
                            .Bold();

                        condiciones.Item()
                            .Text(
                                "El presente presupuesto queda sujeto a la " +
                                "revisión del vehículo y a los trabajos " +
                                "detallados. Cualquier reparación adicional " +
                                "será informada al cliente.")
                            .FontSize(8)
                            .FontColor(GrisTexto);
                    });

                col.Item().Row(row =>
                {
                    row.RelativeItem()
                        .AlignMiddle()
                        .Column(datos =>
                        {
                            datos.Spacing(4);

                            datos.Item()
                                .Text("Firma del cliente: ____________________")
                                .FontSize(8);

                            datos.Item()
                                .Text("Aclaración: __________________________")
                                .FontSize(8);
                        });

                    row.ConstantItem(140)
                        .Height(65)
                        .AlignCenter()
                        .AlignMiddle()
                        .Element(c =>
                        {
                            if (_firma != null && _firma.Length > 0)
                            {
                                c.Image(_firma).FitArea();
                            }
                            else
                            {
                                c.BorderBottom(1)
                                    .BorderColor(Negro)
                                    .AlignCenter()
                                    .AlignBottom()
                                    .PaddingBottom(5)
                                    .Text("Firma y sello del taller")
                                    .FontSize(8);
                            }
                        });
                });
            });
        }

        private static string Formatear(decimal importe)
        {
            return importe.ToString(
                "$ #,##0.00",
                CultureInfo.GetCultureInfo("es-AR"));
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
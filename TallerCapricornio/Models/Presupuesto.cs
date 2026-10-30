using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallerCapricornio.Models;

[Table("Presupuestos")]
public class Presupuesto
{
    [Key]
    public int Id { get; set; }

    public int Numero { get; set; }

    public DateTime FechaCreacion { get; set; }

    [Required]
    public string ApellidoNombre { get; set; } = string.Empty;

    [Required]
    public string Domicilio { get; set; } = string.Empty;

    [Required]
    public string Localidad { get; set; } = string.Empty;

    [Required]
    public string Telefono { get; set; } = string.Empty;

    [Required]
    public string Vehiculo { get; set; } = string.Empty;

    [Required]
    public string Color { get; set; } = string.Empty;

    [Required]
    public string Patente { get; set; } = string.Empty;

    public string? DetalleReparacion { get; set; }

    public string? RepuestosAProveer { get; set; }

    public string? Observaciones { get; set; }

    // Importes del taller
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Impuestos { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ManoObraChapa { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ManoObraPintura { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Bancada { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? OtrosConceptos { get; set; }

    public bool IncluirIVA { get; set; }

    // Importes calculados, no almacenados en la base
    [NotMapped]
    public decimal Subtotal =>
        (Impuestos ?? 0m) +
        (ManoObraChapa ?? 0m) +
        (ManoObraPintura ?? 0m) +
        (Bancada ?? 0m) +
        (OtrosConceptos ?? 0m);

    [NotMapped]
    public decimal ImporteIVA =>
        IncluirIVA ? Math.Round(Subtotal * 0.21m, 2) : 0m;

    [NotMapped]
    public decimal Total => Subtotal + ImporteIVA;

    // Fotos asociadas al presupuesto
    public ICollection<Imagen> Imagenes { get; set; } =
        new List<Imagen>();
}
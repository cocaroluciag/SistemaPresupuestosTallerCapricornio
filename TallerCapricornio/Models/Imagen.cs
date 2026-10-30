using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallerCapricornio.Models;

[Table("Imagenes")]
public class Imagen
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int PresupuestoId { get; set; }

    public int Tipo { get; set; }

    [Required]
    public string NombreArchivo { get; set; } = string.Empty;

    [Required]
    public byte[] Contenido { get; set; } = Array.Empty<byte>();

    [Required]
    public string ContentType { get; set; } = string.Empty;

    [ForeignKey(nameof(PresupuestoId))]
    public Presupuesto? Presupuesto { get; set; }
}
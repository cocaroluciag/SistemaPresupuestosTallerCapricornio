using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using TallerCapricornio.Models;

namespace TallerCapricornio.Controllers;

public class PresupuestosController : Controller
{
    private readonly TallerDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public PresupuestosController(
        TallerDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? buscar)
    {
        var consulta = _context.Presupuestos
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            buscar = buscar.Trim();

            consulta = consulta.Where(p =>
                p.ApellidoNombre.Contains(buscar) ||
                p.Vehiculo.Contains(buscar) ||
                p.Patente.Contains(buscar) ||
                p.Numero.ToString().Contains(buscar));
        }

        ViewBag.Buscar = buscar;

        var presupuestos = await consulta
            .OrderByDescending(p => p.Numero)
            .ToListAsync();

        return View(presupuestos);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var presupuesto = await _context.Presupuestos
            .Include(p => p.Imagenes)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (presupuesto == null)
            return NotFound();

        return View(presupuesto);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Presupuesto
        {
            FechaCreacion = DateTime.Today,
            IncluirIVA = false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Presupuesto presupuesto,
        IFormFile? SeguroFrente,
        IFormFile? SeguroDorso,
        List<IFormFile>? FotosVehiculo)
    {
        ModelState.Remove(nameof(Presupuesto.Imagenes));

        if (!ModelState.IsValid)
            return View(presupuesto);

        var archivos = ObtenerArchivos(
            SeguroFrente, SeguroDorso, FotosVehiculo);

        if (!ValidarFotos(archivos))
            return View(presupuesto);

        await using var transaccion =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var ultimoNumero = await _context.Presupuestos
                .MaxAsync(p => (int?)p.Numero) ?? 0;

            presupuesto.Numero = ultimoNumero + 1;

            if (presupuesto.FechaCreacion == default)
                presupuesto.FechaCreacion = DateTime.Today;

            _context.Presupuestos.Add(presupuesto);
            await _context.SaveChangesAsync();

            await GuardarImagenAsync(
                presupuesto.Id, SeguroFrente, 1);

            await GuardarImagenAsync(
                presupuesto.Id, SeguroDorso, 2);

            if (FotosVehiculo != null)
            {
                foreach (var foto in FotosVehiculo.Where(
                    f => f != null && f.Length > 0))
                {
                    await GuardarImagenAsync(
                        presupuesto.Id, foto, 3);
                }
            }

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return RedirectToAction(
                nameof(Details), new { id = presupuesto.Id });
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var presupuesto = await _context.Presupuestos
            .FirstOrDefaultAsync(p => p.Id == id);

        if (presupuesto == null)
            return NotFound();

        return View(presupuesto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, Presupuesto presupuesto)
    {
        if (id != presupuesto.Id)
            return BadRequest();

        ModelState.Remove(nameof(Presupuesto.Imagenes));

        if (!ModelState.IsValid)
            return View(presupuesto);

        var existente = await _context.Presupuestos
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existente == null)
            return NotFound();

        existente.ApellidoNombre = presupuesto.ApellidoNombre;
        existente.Domicilio = presupuesto.Domicilio;
        existente.Localidad = presupuesto.Localidad;
        existente.Telefono = presupuesto.Telefono;
        existente.Vehiculo = presupuesto.Vehiculo;
        existente.Color = presupuesto.Color;
        existente.Patente = presupuesto.Patente;
        existente.DetalleReparacion = presupuesto.DetalleReparacion;
        existente.RepuestosAProveer = presupuesto.RepuestosAProveer;
        existente.Observaciones = presupuesto.Observaciones;
        existente.Impuestos = presupuesto.Impuestos;
        existente.ManoObraChapa = presupuesto.ManoObraChapa;
        existente.ManoObraPintura = presupuesto.ManoObraPintura;
        existente.Bancada = presupuesto.Bancada;
        existente.OtrosConceptos = presupuesto.OtrosConceptos;
        existente.IncluirIVA = presupuesto.IncluirIVA;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirImagenes(
        int presupuestoId,
        IFormFile? SeguroFrente,
        IFormFile? SeguroDorso,
        List<IFormFile>? FotosVehiculo)
    {
        var presupuestoExiste = await _context.Presupuestos
            .AnyAsync(p => p.Id == presupuestoId);

        if (!presupuestoExiste)
            return NotFound();

        var archivos = ObtenerArchivos(
            SeguroFrente, SeguroDorso, FotosVehiculo);

        if (!ValidarFotos(archivos))
        {
            return RedirectToAction(
                nameof(Details), new { id = presupuestoId });
        }

        await GuardarImagenAsync(
            presupuestoId, SeguroFrente, 1);

        await GuardarImagenAsync(
            presupuestoId, SeguroDorso, 2);

        if (FotosVehiculo != null)
        {
            foreach (var foto in FotosVehiculo.Where(
                f => f != null && f.Length > 0))
            {
                await GuardarImagenAsync(
                    presupuestoId, foto, 3);
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(Details), new { id = presupuestoId });
    }

    [HttpGet]
    public async Task<IActionResult> Imagen(int id)
    {
        var imagen = await _context.Imagenes
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (imagen == null)
            return NotFound();

        return File(imagen.Contenido, imagen.ContentType);
    }

    [HttpGet]
    public async Task<IActionResult> Pdf(int id)
    {
        var presupuesto = await _context.Presupuestos
            .Include(p => p.Imagenes)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (presupuesto == null)
            return NotFound();

        var carpetaImagenes = Path.Combine(
            _environment.WebRootPath ?? "wwwroot", "images");

        var rutaLogo = Path.Combine(carpetaImagenes, "logo.jpg");
        var rutaFirma = Path.Combine(carpetaImagenes, "firma.png");

        byte[]? logo = System.IO.File.Exists(rutaLogo)
            ? await System.IO.File.ReadAllBytesAsync(rutaLogo)
            : null;

        byte[]? firma = System.IO.File.Exists(rutaFirma)
            ? await System.IO.File.ReadAllBytesAsync(rutaFirma)
            : null;

        var documento = new PresupuestoPdf(
            presupuesto, logo, firma);

        // Usa el método definido en PresupuestoPdf.cs.
        var contenidoPdf = documento.GenerarPdf();

        return File(
            contenidoPdf,
            "application/pdf",
            $"Presupuesto-{presupuesto.Numero}.pdf");
    }

    private static List<IFormFile> ObtenerArchivos(
        IFormFile? seguroFrente,
        IFormFile? seguroDorso,
        List<IFormFile>? fotosVehiculo)
    {
        var archivos = new List<IFormFile>();

        if (seguroFrente is { Length: > 0 })
            archivos.Add(seguroFrente);

        if (seguroDorso is { Length: > 0 })
            archivos.Add(seguroDorso);

        if (fotosVehiculo != null)
        {
            archivos.AddRange(
                fotosVehiculo.Where(f => f != null && f.Length > 0));
        }

        return archivos;
    }

    private bool ValidarFotos(List<IFormFile> archivos)
    {
        if (archivos.Count > 22)
        {
            ModelState.AddModelError(
                string.Empty,
                "Podés subir hasta 20 fotos del vehículo, más las dos del seguro.");
            return false;
        }

        foreach (var archivo in archivos)
        {
            if (archivo.Length > 15 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"El archivo {archivo.FileName} supera el límite de 15 MB.");
                return false;
            }

            var extension = Path.GetExtension(archivo.FileName)
                .ToLowerInvariant();

            if (extension is not ".jpg" and not ".jpeg"
                and not ".png" and not ".webp")
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"El archivo {archivo.FileName} no tiene un formato permitido.");
                return false;
            }
        }

        return true;
    }

    private async Task GuardarImagenAsync(
        int presupuestoId,
        IFormFile? archivo,
        int tipo)
    {
        if (archivo == null || archivo.Length == 0)
            return;

        await using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria);

        var imagen = new Imagen
        {
            PresupuestoId = presupuestoId,
            Tipo = tipo,
            NombreArchivo = Path.GetFileName(archivo.FileName),
            Contenido = memoria.ToArray(),
            ContentType = ObtenerContentType(archivo.FileName)
        };

        _context.Imagenes.Add(imagen);
    }

    private static string ObtenerContentType(string nombreArchivo)
    {
        return Path.GetExtension(nombreArchivo).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
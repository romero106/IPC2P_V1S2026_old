using ControlAcademicoMvc.Models;
using Microsoft.AspNetCore.Mvc;

namespace ControlAcademicoMvc.Controllers;

public class EstudianteController : Controller
{
    private static readonly List<Estudiante> _baseDatosMemoria = new()
    {
        new Estudiante { Carne = 2026012, Nombre = "Fernando Velasquez", Promedio = 91.5 },
        new Estudiante { Carne = 2026045, Nombre = "Maria Mercedes", Promedio = 69.3 },
        new Estudiante { Carne = 202504020, Nombre = "Patricio Romero", Promedio = 77.5}
    };

    public IActionResult Listar()
    {
        return View(_baseDatosMemoria);
    }

    public IActionResult Historial(int id)
    {
        var estudiante = _baseDatosMemoria.FirstOrDefault(e => e.Carne == id);
        return estudiante is null ? NotFound() : View(estudiante);
    }

    [HttpPost]
    public IActionResult Registrar([FromBody] Estudiante nuevoEstudiante)
    {
        if (nuevoEstudiante.Carne <= 0 || string.IsNullOrWhiteSpace(nuevoEstudiante.Nombre))
        {
            return BadRequest(new { mensaje = "Datos del estudiante invalidos." });
        }

        _baseDatosMemoria.Add(nuevoEstudiante);
        return Created($"/Estudiante/Historial/{nuevoEstudiante.Carne}", nuevoEstudiante);
    }
}

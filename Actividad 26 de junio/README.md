# Actividad 26 de junio de 2026

## Interoperabilidad y Carga Masiva de Datos

## Parte 1: Evaluación Conceptual y Buenas Prácticas

### 1. Formatos de Intercambio

| Formato | Ventajas | Desventajas |
| ------- | -------- | ----------- |
| **CSV** | Extremadamente ligero y fácil de generar desde Excel. | No soporta jerarquías complejas, solo datos planos. |
| **XML** | Estructurado, soporta tipos de datos y jerarquías. | Verboso, archivos más pesados que JSON o CSV. |

### 2. Diferenciación de Procesos: Serialización vs Deserialización

**Serialización:** Es el proceso de convertir un objeto de C# en una cadena JSON para poder almacenarlo o transmitirlo. Se utiliza el método `JsonSerializer.Serialize(objeto)` de la librería nativa `System.Text.Json`.

**Deserialización:** Es el proceso inverso: convertir una cadena JSON recibida (por ejemplo, desde una API) en un objeto tipificado de C#. Se utiliza `JsonSerializer.Deserialize<T>(json, opciones)` donde `opciones` puede incluir `PropertyNameCaseInsensitive = true` para ignorar mayúsculas/minúsculas.

**En resumen:** Serializar es convertir de **Objeto C# → JSON**; Deserializar es convertir de **JSON → Objeto C#**.

### 3. El Antipatrón del Rendimiento: Error "N+1"

El error **N+1** ocurre cuando se realiza una llamada a la base de datos por cada fila del archivo masivo. Por ejemplo, si procesas 10,000 registros y llamas a `SaveChangesAsync()` dentro del bucle `foreach`, estás haciendo 10,000 operaciones individuales en lugar de una sola transacción. Esto destruye el rendimiento del sistema.

**Estrategia de optimización por lotes (Batching):**
La solución es agrupar todos los registros en una **lista intermedia** y realizar una **única transacción** al final del procesamiento. Se usa `AddRange(lista)` para agregar todos los objetos al contexto de Entity Framework Core, y luego se llama a `SaveChangesAsync()` **una sola vez** fuera del bucle. Esto permite que EF Core optimice el comando SQL para insertar todos los registros de una vez.

**Ejemplo del patrón correcto:**
```csharp
foreach (var item in listaGigante)
{
    _context.Alumnos.Add(item);
}
// EF Core optimiza el comando SQL para insertar todos de una vez
await _context.SaveChangesAsync();
```

---

## Parte 2: Implementación Práctica en C#

### Desafío 1: Consumo de Endpoints y Deserialización

```csharp
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class Alumno
{
    public int Carne { get; set; }
    public string Nombre { get; set; }
    public string Carrera { get; set; }
}

public class AlumnoService
{
    private static readonly HttpClient _client = new HttpClient();

    public async Task<Alumno> ObtenerAlumnoAsync()
    {
        try
        {
            // Realizar petición GET segura
            HttpResponseMessage respuesta = await _client.GetAsync("https://api.usac.edu/v1/alumnos");
            
            // Validar código de estado (lanza excepción si no es 2xx)
            respuesta.EnsureSuccessStatusCode();

            // Leer el contenido JSON de la respuesta
            string json = await respuesta.Content.ReadAsStringAsync();

            // Configurar opciones de deserialización (insensible a mayúsculas/minúsculas)
            var opciones = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Deserializar el payload JSON a un objeto Alumno
            Alumno alumno = JsonSerializer.Deserialize<Alumno>(json, opciones);

            return alumno;
        }
        catch (HttpRequestException ex)
        {
            // Manejo de errores de conexión o estado HTTP
            Console.WriteLine($"Error al consumir la API: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            // Manejo de errores de formato JSON
            Console.WriteLine($"Error al deserializar la respuesta: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            // Manejo de cualquier otra excepción inesperada
            Console.WriteLine($"Error inesperado: {ex.Message}");
            return null;
        }
    }
}
```

### Desafío 2: Endpoint para Carga Masiva CSV

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class AlumnosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AlumnosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("carga-masiva")]
    public async Task<IActionResult> CargarAlumnosMasivo(IFormFile archivo)
    {
        // Validar que se recibió un archivo
        if (archivo == null || archivo.Length == 0)
        {
            return BadRequest("No se proporcionó ningún archivo CSV.");
        }

        // Lista intermedia para almacenar los registros (Batching)
        var alumnosBatch = new List<Alumno>();
        int totalProcesados = 0;

        try
        {
            // Procesar el archivo línea por línea de forma asíncrona
            using (var stream = new StreamReader(archivo.OpenReadStream()))
            {
                // Omitir la primera línea (encabezados del CSV)
                await stream.ReadLineAsync();

                string linea;
                while ((linea = await stream.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(linea)) continue;

                    // Separar la línea por comas
                    string[] columnas = linea.Split(',');

                    if (columnas.Length >= 2)
                    {
                        // Mapear los datos del CSV al objeto Alumno
                        var alumno = new Alumno
                        {
                            Carne = int.Parse(columnas[0].Trim()),
                            Nombre = columnas[1].Trim(),
                            Carrera = columnas.Length > 2 ? columnas[2].Trim() : "Sin asignar"
                        };

                        // Agregar a la lista intermedia
                        alumnosBatch.Add(alumno);
                        totalProcesados++;
                    }
                }
            }

            // Inserción completa en un solo bloque por lotes
            if (alumnosBatch.Count > 0)
            {
                _context.Alumnos.AddRange(alumnosBatch);
                
                // ÚNICA llamada a SaveChangesAsync al finalizar el ciclo
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                mensaje = "Carga masiva completada exitosamente.",
                totalRegistros = totalProcesados
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al procesar el archivo: {ex.Message}");
        }
    }
}
```

---

## Parte 3: Referencias Bibliográficas

Facultad de Ingeniería, USAC. (2026). *Sesión 20: Integración de Datos. Consumo de APIs Externas y Carga Masiva (CSV/XML)*. Laboratorio del curso Introducción a la Programación y Computación 2. Guatemala.
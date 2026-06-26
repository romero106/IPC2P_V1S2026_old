# Actividad 24 de junio de 2026

## De ADO.NET Tradicional a la Automatización con EF Core

## Parte 1: Diagnóstico Técnico y Brecha de Impedancia

### 1. La Brecha de Impedancia
La diferencia conceptual radica en que el **Dominio de Objetos (C#)** se basa en grafos, soporta herencia, polimorfismo y referencias, mientras que el **Dominio Relacional (SQL)** se basa en teoría de conjuntos, tablas planas y normalización, utilizando claves foráneas para relacionar datos. 

Según el mapeador (ORM), las equivalencias son:
*   **Clase Clásica (POCO)** -> Mapea a -> **Tabla**
*   **Propiedad/Atributo** -> Mapea a -> **Columna / Campo**
*   **Instancia de Objeto** -> Mapea a -> **Fila / Registro**

### 2. Mitigación de Vulnerabilidades
*   **Propiedad nativa de EF Core:** EF Core previene la inyección SQL de forma automatizada mediante la **parametrización automática** de las consultas. Cuando usas LINQ, EF Core traduce las expresiones a sentencias SQL donde los valores de las variables se tratan estrictamente como datos (parámetros) y no como código ejecutable.
*   **Comando equivalente en ADO.NET:** En ADO.NET tradicional, mitigábamos este riesgo creando manualmente objetos de tipo **`SqlParameter`** (por ejemplo, usando `cmd.Parameters.AddWithValue("@filtro", valor)`), asegurando que el motor de base de datos interpretara la entrada solo como un literal.

### 3. Optimización de Infraestructura
El método **`.AsNoTracking()`** desactiva el *Change Tracker* (Rastreador de Cambios) de EF Core. Por defecto, cada vez que EF Core lee una entidad de la base de datos, guarda una copia en memoria para detectar si fue modificada y así poder actualizarla después con `SaveChanges()`. 
En flujos de **solo lectura** (como renderizar una lista en una interfaz), este rastreo es un gasto innecesario de ciclos de CPU y memoria RAM. Al apagarlo, liberamos recursos críticos del servidor, demostrando "solidaridad computacional" con el hardware compartido de la universidad, permitiendo que el servidor soporte más peticiones simultáneas.

---

## Parte 2: Desafío de Refactorización de Código

### 1. El Contexto (DbContext)

```csharp
using Microsoft.EntityFrameworkCore;

public class Catedratico
{
    public int Id { get; set; }
    public string Nombre { get; set; }
}

public class UnidadAcademicaContext : DbContext
{
    public DbSet<Catedratico> Catedraticos { get; set; }
}
```

### 2. La Consulta LINQ

```csharp
using Microsoft.EntityFrameworkCore;

public class CatedraticoService
{
    private readonly UnidadAcademicaContext _context;

    public CatedraticoService(UnidadAcademicaContext context)
    {
        _context = context;
    }

    public List<Catedratico> ObtenerCatedraticos()
    {
        return _context.Catedraticos
            .AsNoTracking()
            .Where(c => c.Nombre.StartsWith("Ing."))
            .ToList();
    }
}
```

**Explicación:** el método utiliza LINQ para filtrar los catedráticos cuyo nombre comienza con "Ing.". Además, se emplea `.AsNoTracking()` para desactivar el rastreo de cambios, ya que la consulta es únicamente de lectura, mejorando así el rendimiento y reduciendo el consumo de memoria.

## Referencias Bibliográficas

Facultad de Ingeniería, USAC. (2026). *Sesión 17: Conectividad con SQL Server. Acceso Estructurado a Datos mediante C# y ADO.NET.* Laboratorio de Introducción a la Programación y Computación 2. Guatemala.

Facultad de Ingeniería, USAC. (2026). *Sesión 18: Mapeo de Objetos Relacionales. Persistencia Automatizada con Entity Framework Core.* Laboratorio de Introducción a la Programación y Computación 2. Guatemala.
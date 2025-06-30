
namespace HighRiskEntitySearcherApi.Models;

// Representa un único hallazgo en una de las fuentes de datos.
public class Hit
{
    public required string Source { get; set; }
    public required Dictionary<string, string> Data { get; set; }
}

// El objeto de respuesta completo que la API devolverá.
public class SearchResponse
{
    // El número total de hallazgos en todas las fuentes.
    public int HitsFound { get; set; }

    // La lista de todos los hallazgos.
    public List<Hit> Results { get; set; } = new List<Hit>();
}

namespace NovaPad.Core.Events;

public class EstadoConexion
{
    public bool Conectado { get; set; }
    public string IdMando { get; set; } = string.Empty;
    public string NombreMando { get; set; } = string.Empty;
    public int Bateria { get; set; } = -1;
    public bool Cargando { get; set; }
}

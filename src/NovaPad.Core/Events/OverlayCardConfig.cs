namespace NovaPad.Core.Events;

public class OverlayCardConfig
{
    public string ControllerId { get; set; } = string.Empty;
    public string ColorAcento { get; set; } = "#00BCD4";
    public string ColorFondo { get; set; } = "#222222";
    public string EstiloTarjeta { get; set; } = "";
    public bool VerBateria { get; set; } = true;
    public bool VerLatencia { get; set; } = true;
    public bool VerFrecuencia { get; set; }
    public bool VerPerfil { get; set; } = true;
    public bool VerConexion { get; set; } = true;
    public bool VerTipo { get; set; } = true;
    public string? AnclajeHud { get; set; }
    public double? DesvioX { get; set; }
    public double? DesvioY { get; set; }
    public double? Escala { get; set; }
}

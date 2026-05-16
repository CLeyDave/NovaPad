namespace NovaPad.Core.Events;

public class DatosConfigOverlay
{
    public double Opacidad { get; set; } = 0.8;
    public double Escala { get; set; } = 1.0;
    public double DuracionAviso { get; set; } = 3.0;
    public bool VerAvisos { get; set; } = true;
    public bool VerBateria { get; set; } = true;
    public bool VerLatencia { get; set; } = true;
    public bool VerFrecuencia { get; set; }
    public bool VerPerfil { get; set; } = true;
    public bool VerConexion { get; set; } = true;
    public bool VerFps { get; set; }
    public bool VerReloj { get; set; } = true;
    public bool VerTipo { get; set; } = true;
    public string AnclajeHud { get; set; } = "TopRight";
    public double DesvioX { get; set; } = 20;
    public double DesvioY { get; set; } = 20;
    public string AnclajeAvisos { get; set; } = "BottomRight";
    public double DesvioAvisoX { get; set; } = 20;
    public double DesvioAvisoY { get; set; } = 10;
    public string ColorAcento { get; set; } = "#00BCD4";
    public double TamanoLetra { get; set; } = 1.0;
    public string ColorFondo { get; set; } = "#111111";
    public string EstiloTarjeta { get; set; } = "Clasico";
    public string EstiloAviso { get; set; } = "Clasica";
}

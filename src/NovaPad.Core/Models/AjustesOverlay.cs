using System.Collections.Generic;
using NovaPad.Core.Events;

namespace NovaPad.Core.Models;

public class AjustesOverlay
{
    public bool Activado { get; set; } = true;
    public bool AutoStart { get; set; } = true;
    public double PosX { get; set; } = 20;
    public double PosY { get; set; } = 20;
    public double Opacidad { get; set; } = 0.8;
    public double Escala { get; set; } = 1.0;
    public bool Traspasar { get; set; } = true;
    public bool SiempreArriba { get; set; } = true;
    public bool VerBateria { get; set; } = true;
    public bool VerTipo { get; set; } = true;
    public bool VerLatencia { get; set; } = true;
    public bool VerEntradas { get; set; } = true;
    public bool VerPerfil { get; set; } = true;
    public bool VerConexion { get; set; } = true;
    public bool VerFrecuencia { get; set; }
    public bool VerFps { get; set; }
    public bool VerReloj { get; set; } = true;
    public bool VerSenal { get; set; } = true;
    public string Tema { get; set; } = "Dark";
    public string Anclaje { get; set; } = "TopRight";
    public bool VerAvisos { get; set; } = true;
    public string AnclajeAvisos { get; set; } = "BottomRight";
    public double DesvioAvisoX { get; set; } = 20;
    public double DesvioAvisoY { get; set; } = 10;
    public string ColorAcento { get; set; } = "#00BCD4";
    public double TamanoLetra { get; set; } = 1.0;
    public string ColorFondo { get; set; } = "#111111";
    public string EstiloTarjeta { get; set; } = "Clasico";
    public string EstiloAviso { get; set; } = "Clasica";
    public Dictionary<string, OverlayCardConfig> PorMando { get; set; } = new();
}

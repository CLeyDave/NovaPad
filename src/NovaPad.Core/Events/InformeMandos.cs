namespace NovaPad.Core.Events;

public class InfoMando
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int NivelBateria { get; set; } = -1;
    public bool Cargando { get; set; }
    public bool Conectado { get; set; } = true;
    public double LatenciaMs { get; set; }
    public int Hz { get; set; }
    public double Senal { get; set; } = 1.0;
    public string? PerfilActivo { get; set; }
    public string? TipoMando { get; set; }

    public double Lx { get; set; }
    public double Ly { get; set; }
    public double Rx { get; set; }
    public double Ry { get; set; }
    public double L2 { get; set; }
    public double R2 { get; set; }
    public bool BtnA { get; set; }
    public bool BtnB { get; set; }
    public bool BtnX { get; set; }
    public bool BtnY { get; set; }
    public bool BtnUp { get; set; }
    public bool BtnDown { get; set; }
    public bool BtnLeft { get; set; }
    public bool BtnRight { get; set; }
    public bool BtnLB { get; set; }
    public bool BtnRB { get; set; }
    public bool BtnL3 { get; set; }
    public bool BtnR3 { get; set; }
}

public class InformeMandos
{
    public List<InfoMando> Lista { get; set; } = new();
}

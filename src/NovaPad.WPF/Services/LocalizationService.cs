using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NovaPad.WPF.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static readonly Dictionary<string, Dictionary<string, string>> _strings = new()
    {
        ["en"] = new()
        {
            ["AppTitle"] = "NovaPad",
            ["ControllerManager"] = "Controller Manager",
            ["Search"] = "Search",
            ["Scan"] = "Scan",
            ["ShowNovaPad"] = "Show NovaPad",
            ["Exit"] = "Exit",

            ["Nav_Dashboard"] = "Dashboard",
            ["Nav_Controllers"] = "Controllers",
            ["Nav_Details"] = "Details",
            ["Nav_DeviceInfo"] = "Device Info",
            ["Nav_Battery"] = "Battery",
            ["Nav_RGB"] = "RGB",
            ["Nav_Overlay"] = "Overlay",
            ["Nav_Settings"] = "Settings",
            ["Dashboard_Title"] = "Dashboard",
            ["Dashboard_Controllers"] = "Controllers",
            ["Dashboard_Battery"] = "Battery",
            ["Dashboard_Touchpad"] = "Touchpad",
            ["Dashboard_Gyroscope"] = "Gyroscope",
            ["Dashboard_Vibration"] = "Vibration",
            ["Dashboard_SystemStatus"] = "System Status",
            ["Dashboard_Connected"] = "Connected",
            ["Dashboard_AvgLatency"] = "Avg Latency",
            ["Dashboard_AvgBattery"] = "Avg Battery",
            ["Dashboard_Active"] = "Active",

            ["Controllers_Title"] = "Connected Controllers",

            ["Detail_Controller"] = "CONTROLLER",
            ["Detail_Latency"] = "Lat: ",
            ["Detail_Polling"] = "Poll: ",
            ["Detail_Online"] = "Online",
            ["Detail_Local"] = "Local",

            ["DeviceInfo_Title"] = "CONTROLLER",
            ["DeviceInfo_Identity"] = "IDENTITY",
            ["DeviceInfo_Name"] = "Name",
            ["DeviceInfo_Type"] = "Type",
            ["DeviceInfo_Connection"] = "Connection",
            ["DeviceInfo_Hardware"] = "HARDWARE",
            ["DeviceInfo_VID"] = "VID",
            ["DeviceInfo_PID"] = "PID",
            ["DeviceInfo_HIDPath"] = "HID Path",
            ["DeviceInfo_Firmware"] = "Firmware",
            ["DeviceInfo_Capabilities"] = "Capabilities",
            ["DeviceInfo_Status"] = "STATUS",
            ["DeviceInfo_Battery"] = "Battery",
            ["DeviceInfo_Latency"] = "Latency",
            ["DeviceInfo_Frequency"] = "Frequency",
            ["DeviceInfo_Serial"] = "Serial",
            ["DeviceInfo_Active"] = "Active",
            ["DeviceInfo_Signal"] = "SIGNAL",
            ["DeviceInfo_LatencyTitle"] = "LATENCY",
            ["DeviceInfo_FrequencyTitle"] = "FREQUENCY",
            ["DeviceInfo_Empty"] = "Connect a controller to view device information.",

            ["Battery_Title"] = "CONTROLLER BATTERY",
            ["Battery_Empty"] = "No controllers connected.",

            ["Settings_Title"] = "SETTINGS",
            ["Settings_General"] = "GENERAL",
            ["Settings_StartWithWindows"] = "Start with Windows",
            ["Settings_StartWithWindowsDesc"] = "Open NovaPad on login",
            ["Settings_MinimizeToTray"] = "Minimize to tray",
            ["Settings_MinimizeToTrayDesc"] = "Minimize to tray on close",
            ["Settings_Appearance"] = "APPEARANCE",
            ["Settings_Theme"] = "Theme",
            ["Settings_Dark"] = "Dark",
            ["Settings_Light"] = "Light",
            ["Settings_Language"] = "Language",
            ["Settings_Spanish"] = "Spanish",
            ["Settings_English"] = "English",
            ["Settings_Notifications"] = "NOTIFICATIONS",
            ["Settings_Duration"] = "Duration (seconds)",
            ["Settings_BatteryAlert"] = "Battery alert threshold",
            ["Settings_ConnectionNotifs"] = "Connection notifications",
            ["Settings_BatteryNotifs"] = "Battery notifications",
            ["Settings_About"] = "ABOUT",
            ["Settings_Version"] = "Version",
            ["Settings_Build"] = "Build",

            ["OverlaySettings_Title"] = "Overlay Settings",
            ["OverlaySettings_Subtitle"] = "Overlay configuration",
            ["OverlaySettings_HudScale"] = "HUD Scale",
            ["OverlaySettings_NotifDuration"] = "Notification duration",
            ["OverlaySettings_ShowClock"] = "Show clock",
            ["OverlaySettings_Running"] = "Running",
            ["OverlaySettings_Stopped"] = "Stopped",
            ["OverlaySettings_General"] = "GENERAL",
            ["OverlaySettings_AutoStart"] = "Auto-start Overlay",
            ["OverlaySettings_AutoStartDesc"] = "Start overlay on app launch",
            ["OverlaySettings_Status"] = "Status",
            ["OverlaySettings_Start"] = "Start",
            ["OverlaySettings_Stop"] = "Stop",
            ["OverlaySettings_Appearance"] = "APPEARANCE",
            ["OverlaySettings_Opacity"] = "Opacity",
            ["OverlaySettings_ShowNotifs"] = "Show notifications",
            ["OverlaySettings_HudPosition"] = "HUD POSITION",
            ["OverlaySettings_Anchor"] = "Anchor",
            ["OverlaySettings_OffsetX"] = "Offset X",
            ["OverlaySettings_OffsetY"] = "Offset Y",
            ["OverlaySettings_NotifPosition"] = "NOTIFICATION POSITION",
            ["OverlaySettings_HudElements"] = "HUD ELEMENTS",
            ["OverlaySettings_ShowBattery"] = "Show battery",
            ["OverlaySettings_ShowLatency"] = "Show latency",
            ["OverlaySettings_ShowPollingRate"] = "Show polling rate",
            ["OverlaySettings_ShowProfile"] = "Show active profile",
            ["OverlaySettings_ShowConnection"] = "Show connection status",
            ["OverlaySettings_ShowFps"] = "Show FPS (debug)",
            ["OverlaySettings_ShowControllerType"] = "Show controller type",
            ["OverlaySettings_TestNotification"] = "Test Notification",
            ["OverlaySettings_AccentColor"] = "Accent color",
            ["OverlaySettings_FontSize"] = "Font size",
            ["OverlaySettings_BgColor"] = "Background color",

            ["RGB_Title"] = "RGB Lighting",
            ["RGB_Subtitle"] = "LED control for DualSense and DualShock 4",
            ["RGB_Controller"] = "CONTROLLER",
            ["RGB_NotSupported"] = "This controller does not support RGB lighting",
            ["RGB_Color"] = "COLOR",
            ["RGB_Effect"] = "EFFECT",
            ["RGB_Static"] = "Static",
            ["RGB_Pulse"] = "Pulse",
            ["RGB_Rainbow"] = "Rainbow",
            ["RGB_Battery"] = "Battery",
            ["RGB_Strobe"] = "Strobe",
            ["RGB_Heartbeat"] = "Heartbeat",
            ["RGB_Fire"] = "Fire",
            ["RGB_Disco"] = "Disco",
            ["RGB_Wave"] = "Wave",
            ["RGB_Meteor"] = "Meteor",
            ["RGB_Aurora"] = "Aurora",
            ["RGB_Storm"] = "Storm",
            ["RGB_Stop"] = "Stop",
            ["RGB_Apply"] = "Apply",

            ["Overlay_NoControllers"] = "No controllers",
            ["Expanded_Title"] = "Control Panel",
            ["Expanded_Connected"] = "Connected",
            ["Expanded_Disconnected"] = "Disconnected",

            ["Notif_ControllerConnected"] = "Controller Connected",
            ["Notif_ControllerDisconnected"] = "Controller Disconnected",
            ["Notif_Battery"] = "Battery",
            ["Notif_Charging"] = "Charging",
        },
        ["es"] = new()
        {
            ["AppTitle"] = "NovaPad",
            ["ControllerManager"] = "Gestor de Mandos",
            ["Search"] = "Buscar",
            ["Scan"] = "Escaneo",
            ["ShowNovaPad"] = "Mostrar NovaPad",
            ["Exit"] = "Salir",

            ["Nav_Dashboard"] = "Panel",
            ["Nav_Controllers"] = "Mandos",
            ["Nav_Details"] = "Detalles",
            ["Nav_DeviceInfo"] = "Info Dispositivo",
            ["Nav_Battery"] = "Batería",
            ["Nav_RGB"] = "RGB",
            ["Nav_Overlay"] = "Overlay",
            ["Nav_Settings"] = "Configuración",
            ["Dashboard_Title"] = "Panel",
            ["Dashboard_Controllers"] = "Mandos",
            ["Dashboard_Battery"] = "Batería",
            ["Dashboard_Touchpad"] = "Touchpad",
            ["Dashboard_Gyroscope"] = "Giroscopio",
            ["Dashboard_Vibration"] = "Vibración",
            ["Dashboard_SystemStatus"] = "Estado del Sistema",
            ["Dashboard_Connected"] = "Conectados",
            ["Dashboard_AvgLatency"] = "Latencia Prom",
            ["Dashboard_AvgBattery"] = "Batería Prom",
            ["Dashboard_Active"] = "Activo",

            ["Controllers_Title"] = "Mandos Conectados",

            ["Detail_Controller"] = "MANDO",
            ["Detail_Latency"] = "Lat: ",
            ["Detail_Polling"] = "Poll: ",
            ["Detail_Online"] = "Online",
            ["Detail_Local"] = "Local",

            ["DeviceInfo_Title"] = "MANDO",
            ["DeviceInfo_Identity"] = "IDENTIDAD",
            ["DeviceInfo_Name"] = "Nombre",
            ["DeviceInfo_Type"] = "Tipo",
            ["DeviceInfo_Connection"] = "Conexión",
            ["DeviceInfo_Hardware"] = "HARDWARE",
            ["DeviceInfo_VID"] = "VID",
            ["DeviceInfo_PID"] = "PID",
            ["DeviceInfo_HIDPath"] = "Ruta HID",
            ["DeviceInfo_Firmware"] = "Firmware",
            ["DeviceInfo_Capabilities"] = "Capacidades",
            ["DeviceInfo_Status"] = "ESTADO",
            ["DeviceInfo_Battery"] = "Batería",
            ["DeviceInfo_Latency"] = "Latencia",
            ["DeviceInfo_Frequency"] = "Frecuencia",
            ["DeviceInfo_Serial"] = "Serial",
            ["DeviceInfo_Active"] = "Activo",
            ["DeviceInfo_Signal"] = "SEÑAL",
            ["DeviceInfo_LatencyTitle"] = "LATENCIA",
            ["DeviceInfo_FrequencyTitle"] = "FRECUENCIA",
            ["DeviceInfo_Empty"] = "Conecta un mando para ver información del dispositivo.",

            ["Battery_Title"] = "BATERÍA DE MANDOS",
            ["Battery_Empty"] = "No hay mandos conectados.",

            ["Settings_Title"] = "CONFIGURACIÓN",
            ["Settings_General"] = "GENERAL",
            ["Settings_StartWithWindows"] = "Iniciar con Windows",
            ["Settings_StartWithWindowsDesc"] = "Abrir NovaPad al iniciar sesión",
            ["Settings_MinimizeToTray"] = "Minimizar a bandeja",
            ["Settings_MinimizeToTrayDesc"] = "Minimizar al cerrar ventana",
            ["Settings_Appearance"] = "APARIENCIA",
            ["Settings_Theme"] = "Tema",
            ["Settings_Dark"] = "Oscuro",
            ["Settings_Light"] = "Claro",
            ["Settings_Language"] = "Idioma",
            ["Settings_Spanish"] = "Español",
            ["Settings_English"] = "Inglés",
            ["Settings_Notifications"] = "NOTIFICACIONES",
            ["Settings_Duration"] = "Duración (segundos)",
            ["Settings_BatteryAlert"] = "Umbral alerta batería",
            ["Settings_ConnectionNotifs"] = "Notificaciones de conexión",
            ["Settings_BatteryNotifs"] = "Notificaciones de batería",
            ["Settings_About"] = "ACERCA DE",
            ["Settings_Version"] = "Versión",
            ["Settings_Build"] = "Compilación",

            ["OverlaySettings_Title"] = "Configuración de la superposición",
            ["OverlaySettings_Subtitle"] = "Configuración de la superposición",
            ["OverlaySettings_HudScale"] = "Escala del HUD",
            ["OverlaySettings_NotifDuration"] = "Duración notificación",
            ["OverlaySettings_ShowClock"] = "Mostrar reloj",
            ["OverlaySettings_Running"] = "En ejecución",
            ["OverlaySettings_Stopped"] = "Detenido",
            ["OverlaySettings_General"] = "GENERAL",
            ["OverlaySettings_AutoStart"] = "Auto-iniciar Overlay",
            ["OverlaySettings_AutoStartDesc"] = "Iniciar overlay al abrir la app",
            ["OverlaySettings_Status"] = "Estado",
            ["OverlaySettings_Start"] = "Iniciar",
            ["OverlaySettings_Stop"] = "Detener",
            ["OverlaySettings_Appearance"] = "APARIENCIA",
            ["OverlaySettings_Opacity"] = "Opacidad",
            ["OverlaySettings_ShowNotifs"] = "Mostrar notificaciones",
            ["OverlaySettings_HudPosition"] = "POSICIÓN DEL HUD",
            ["OverlaySettings_Anchor"] = "Anclaje",
            ["OverlaySettings_OffsetX"] = "Desplazamiento X",
            ["OverlaySettings_OffsetY"] = "Desplazamiento Y",
            ["OverlaySettings_NotifPosition"] = "POSICIÓN DE NOTIFICACIONES",
            ["OverlaySettings_HudElements"] = "ELEMENTOS DEL HUD",
            ["OverlaySettings_ShowBattery"] = "Mostrar batería",
            ["OverlaySettings_ShowLatency"] = "Mostrar latencia",
            ["OverlaySettings_ShowPollingRate"] = "Mostrar polling rate",
            ["OverlaySettings_ShowProfile"] = "Mostrar perfil activo",
            ["OverlaySettings_ShowConnection"] = "Mostrar estado de conexión",
            ["OverlaySettings_ShowFps"] = "Mostrar FPS (debug)",
            ["OverlaySettings_ShowControllerType"] = "Mostrar tipo de mando",
            ["OverlaySettings_TestNotification"] = "Probar Notificación",
            ["OverlaySettings_AccentColor"] = "Color de acento",
            ["OverlaySettings_FontSize"] = "Tamaño de fuente",
            ["OverlaySettings_BgColor"] = "Color de fondo",

            ["RGB_Title"] = "Iluminación RGB",
            ["RGB_Subtitle"] = "Control de LEDs para DualSense y DualShock 4",
            ["RGB_Controller"] = "MANDO",
            ["RGB_NotSupported"] = "Este mando no es compatible con iluminación RGB",
            ["RGB_Color"] = "COLOR",
            ["RGB_Effect"] = "EFECTO",
            ["RGB_Static"] = "Estático",
            ["RGB_Pulse"] = "Pulso",
            ["RGB_Rainbow"] = "Arcoíris",
            ["RGB_Battery"] = "Batería",
            ["RGB_Strobe"] = "Estroboscópico",
            ["RGB_Heartbeat"] = "Latido",
            ["RGB_Fire"] = "Fuego",
            ["RGB_Disco"] = "Discoteca",
            ["RGB_Wave"] = "Ola",
            ["RGB_Meteor"] = "Meteoro",
            ["RGB_Aurora"] = "Aurora",
            ["RGB_Storm"] = "Tormenta",
            ["RGB_Stop"] = "Apagar",
            ["RGB_Apply"] = "Aplicar",

            ["Overlay_NoControllers"] = "Sin mandos",
            ["Expanded_Title"] = "Panel de Control",
            ["Expanded_Connected"] = "Conectado",
            ["Expanded_Disconnected"] = "Desconectado",

            ["Notif_ControllerConnected"] = "Mando Conectado",
            ["Notif_ControllerDisconnected"] = "Mando Desconectado",
            ["Notif_Battery"] = "Batería",
            ["Notif_Charging"] = "Cargando",
        }
    };

    private string _currentLang = "es";

    public static LocalizationService Instance { get; } = new();

    public string CurrentLang
    {
        get => _currentLang;
        set
        {
            if (_currentLang == value) return;
            _currentLang = value;
            NotifyAllProperties();
        }
    }

    public string this[string key] => Get(key);

    public string Get(string key) =>
        _strings.TryGetValue(_currentLang, out var lang)
        && lang.TryGetValue(key, out var val)
            ? val
            : _strings["en"].GetValueOrDefault(key, $"[{key}]");

    public bool IsEnglish
    {
        get => _currentLang == "en";
        set => CurrentLang = value ? "en" : "es";
    }

    private void NotifyAllProperties()
    {
        foreach (var key in _strings["en"].Keys)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsEnglish"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

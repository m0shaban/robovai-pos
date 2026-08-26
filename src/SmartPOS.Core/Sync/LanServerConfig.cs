namespace SmartPOS.Core.Sync;

public class LanServerConfig
{
    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 5050;
    public string BindAddress { get; set; } = "0.0.0.0";
    public string JwtSecret { get; set; } = "RobovAI_LAN_Secret_Key_Change_In_Production_32Bytes!";
    public int ConnectionTimeoutMs { get; set; } = 30000;
    public string[] CorsAllowedOrigins { get; set; } = new[] { "*" };

    public LanServerConfig Clone() => new()
    {
        Enabled = Enabled,
        Port = Port,
        BindAddress = BindAddress,
        JwtSecret = JwtSecret,
        ConnectionTimeoutMs = ConnectionTimeoutMs,
        CorsAllowedOrigins = CorsAllowedOrigins != null ? (string[])CorsAllowedOrigins.Clone() : new[] { "*" }
    };
}

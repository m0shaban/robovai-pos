namespace SmartPOS.Core.Sync;

public class SyncConfig
{
    public SyncMode Mode { get; set; } = SyncMode.Hybrid;
    public string DeviceId { get; set; } = Guid.NewGuid().ToString();
    public string DeviceName { get; set; } = Environment.MachineName;
    public string BranchCode { get; set; } = "BR-MAIN-01";
    public int BranchId { get; set; } = 1;

    public LanServerConfig LanServer { get; set; } = new();
    public CloudServerConfig CloudServer { get; set; } = new();
    public SyncEngineParams EngineParams { get; set; } = new();

    public SyncConfig Clone() => new()
    {
        Mode = Mode,
        DeviceId = DeviceId,
        DeviceName = DeviceName,
        BranchCode = BranchCode,
        BranchId = BranchId,
        LanServer = LanServer != null ? LanServer.Clone() : new(),
        CloudServer = CloudServer != null ? CloudServer.Clone() : new(),
        EngineParams = EngineParams != null ? EngineParams.Clone() : new()
    };
}

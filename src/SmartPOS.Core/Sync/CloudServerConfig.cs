namespace SmartPOS.Core.Sync;

public class CloudServerConfig
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "Firebase"; // "Firebase" | "REST" | "GraphQL"
    public string BaseUrl { get; set; } = "https://api.robovai.tech/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string ProjectId { get; set; } = "robovai-pos-prod";
    public string TenantId { get; set; } = "tenant_default";
    public string AuthToken { get; set; } = string.Empty;

    public CloudServerConfig Clone() => new()
    {
        Enabled = Enabled,
        Provider = Provider,
        BaseUrl = BaseUrl,
        ApiKey = ApiKey,
        ProjectId = ProjectId,
        TenantId = TenantId,
        AuthToken = AuthToken
    };
}

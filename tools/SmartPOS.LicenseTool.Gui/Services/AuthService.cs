namespace SmartPOS.LicenseTool.Gui.Services;

internal static class AuthService
{
    // Simple offline protection (per request). Not a security boundary.
    private const string AdminEmail = "adminpos";
    private const string AdminPassword = "adminpos123";

    public static bool Validate(string email, string password)
    {
        return string.Equals(email?.Trim(), AdminEmail, StringComparison.Ordinal)
            && string.Equals(password ?? string.Empty, AdminPassword, StringComparison.Ordinal);
    }
}

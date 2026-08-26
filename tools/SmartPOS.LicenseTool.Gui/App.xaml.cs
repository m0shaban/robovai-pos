namespace SmartPOS.LicenseTool.Gui;

public partial class App : System.Windows.Application
{
	protected override void OnStartup(System.Windows.StartupEventArgs e)
	{
		// Avoid GPU/driver issues on some client machines (WPF hardware acceleration).
		System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
		base.OnStartup(e);
	}
}

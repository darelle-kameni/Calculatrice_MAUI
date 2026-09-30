namespace CalculatriceMaui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// L'interface de la calculatrice est conçue en thème sombre.
		UserAppTheme = AppTheme.Dark;
	}

	protected override Window CreateWindow(IActivationState? activationState) =>
		new(new AppShell())
		{
			Title = "Calculatrice"
		};
}

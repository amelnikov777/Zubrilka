using Microsoft.Extensions.Logging;
using Zubrilka.Data;
using Zubrilka.Services;

namespace Zubrilka;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// --- Data layer (Phase 1) ---
		// Single shared database connection for the whole app.
		builder.Services.AddSingleton<AppDatabase>();
		// Repositories are stateless wrappers over AppDatabase, so singletons are fine.
		builder.Services.AddSingleton<ICardRepository, CardRepository>();
		builder.Services.AddSingleton<IBlockRepository, BlockRepository>();
		builder.Services.AddSingleton<ISettingsRepository, SettingsRepository>();

		// --- Services (Phase 2) ---
		// xlsx import; returns a Block the repository can persist.
		builder.Services.AddSingleton<IBlockImporter, XlsxBlockImporter>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

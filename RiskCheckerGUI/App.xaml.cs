using System.Windows;
using RiskCheckerGUI.ViewModels;

namespace RiskCheckerGUI
{
    public partial class App : Application
    {
        private MainViewModel _mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create and assign the main view model
            _mainViewModel = new MainViewModel();
            
            // Create main window
            var mainWindow = new Views.MainWindow
            {
                DataContext = _mainViewModel
            };
            
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up resources
            _mainViewModel.Cleanup();
            
            base.OnExit(e);
        }
    }
}
using System;
using System.Windows;
using System.Windows.Threading;
using RiskCheckerGUI.ViewModels;
using RiskCheckerGUI.Views;

namespace RiskCheckerGUI
{
    public partial class App : Application
    {
        private MainViewModel _mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Dodaj globalnego handlera wyjątków
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                base.OnStartup(e);

                // Create and assign the main view model
                _mainViewModel = new MainViewModel();
                
                // Create main window
                var mainWindow = new MainWindow
                {
                    DataContext = _mainViewModel
                };
                
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas inicjalizacji aplikacji: {ex.Message}\n\n{ex.StackTrace}", 
                    "Błąd krytyczny", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Nieobsługiwany wyjątek: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Oznacz wyjątek jako obsłużony
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"Krytyczny nieobsługiwany wyjątek: {ex?.Message}\n\n{ex?.StackTrace}", "Błąd krytyczny", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up resources
            _mainViewModel?.Cleanup();
            
            base.OnExit(e);
        }
    }
}
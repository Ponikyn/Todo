using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO;

namespace Todo
{
    public partial class App : Application
    {
        public App()
        {
            // Subscribe to global exceptions to surface startup/runtime errors
            AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleGlobalException(e.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException");
            TaskScheduler.UnobservedTaskException += (s, e) => HandleGlobalException(e.Exception, "TaskScheduler.UnobservedTaskException");

            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // ensure user sees an error page even if Application.Current is not yet initialized
                try
                {
                    var msg = $"[App.InitializeComponent] {ex}";
                    Debug.WriteLine(msg);

                    var errorPage = new ContentPage
                    {
                        Title = "Ошибка запуска",
                        Content = new ScrollView
                        {
                            Content = new Label { Text = msg, TextColor = Colors.Red }
                        }
                    };

                    // Try to set MainPage directly so the error is visible on startup
                    this.MainPage = errorPage;
                }
                catch { }

                HandleGlobalException(ex, "App.InitializeComponent");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                return new Window(new AppShell());
            }
            catch (Exception ex)
            {
                HandleGlobalException(ex, "CreateWindow");

                // Fallback to a simple window showing error
                var errorPage = new ContentPage
                {
                    Title = "Ошибка запуска",
                    Content = new ScrollView
                    {
                        Content = new Label { Text = ex?.ToString() ?? "Неизвестная ошибка", TextColor = Colors.Red }
                    }
                };
                return new Window(errorPage);
            }
        }

        void HandleGlobalException(Exception? ex, string source)
        {
            try
            {
                var msg = $"[{source}] " + (ex?.ToString() ?? "null");
                Debug.WriteLine(msg);

                // Write error to a file for diagnostics
                try
                {
                    var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var path = Path.Combine(folder, "todo_startup_error.txt");
                    File.WriteAllText(path, msg);
                }
                catch { }

                // If app is already initialized, show a page with error details
                if (Application.Current != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            Application.Current.MainPage = new ContentPage
                            {
                                Title = "Ошибка",
                                Content = new ScrollView
                                {
                                    Content = new Label { Text = msg, TextColor = Colors.Red }
                                }
                            };
                        }
                        catch { }
                    });
                }
            }
            catch { }
        }
    }
}
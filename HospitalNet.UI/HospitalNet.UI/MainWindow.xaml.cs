using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HospitalNet.UI.Views;

namespace HospitalNet.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, Func<UserControl>> _views;
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            _views = new Dictionary<string, Func<UserControl>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Dashboard"] = () => new DashboardView(),
                ["Patients"] = () => new PatientsView(),
                ["Appointments"] = () => new AppointmentsView(),
                ["Doctors"] = () => new DoctorsView(),
                ["Analytics"] = () => new AnalyticsView(),
                ["Settings"] = () => new SettingsView(),
            };

            // Show default view
            ShowView("Dashboard");

            // Timestamp update
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => TimestampTextBlock.Text = DateTime.Now.ToString("g");
            _timer.Start();
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                PageTitleTextBlock.Text = tag;
                ShowView(tag);
            }
        }

        private void ShowView(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                ContentFrame.Content = null;
                return;
            }

            if (_views.TryGetValue(tag, out var factory))
            {
                try
                {
                    ContentFrame.Content = factory();
                }
                catch (Exception ex)
                {
                    ContentFrame.Content = new TextBlock
                    {
                        Text = $"View could not be loaded: {ex.Message}",
                        Margin = new Thickness(16)
                    };
                }
            }
            else
            {
                ContentFrame.Content = new TextBlock
                {
                    Text = $"{tag} view not found.",
                    Margin = new Thickness(16)
                };
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}

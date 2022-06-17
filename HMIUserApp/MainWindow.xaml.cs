using HMIUserApp.Helpers;
using HMIUserApp.Pages;
using HMIUserApp.Service;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;


namespace HMIUserApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _allowDirectNavigation = true;
        private NavigatingCancelEventArgs _navArgs = null;
        public string csvFileName;
        public bool isUploadCsv = true;
        public bool isSaveCsv = true;

        public AppSettings appSettings;
        public InsightService insightService;
        public Page MainPage;
        public Page InsightPage;
        public MainWindow()
        {
            InitializeComponent();
            appSettings = ConfigurationHelper.LoadJson();
            insightService = new InsightService(appSettings);
            MainPage = new MainPage();
            InsightPage = new InsightPage();
            MainFrame.Content = MainPage;
        }
        #region Frame Naviation Processing
        private void frame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (Content != null && !_allowDirectNavigation)
            {
                e.Cancel = true;

                _navArgs = e;

                DoubleAnimation animation0 = new DoubleAnimation();
                animation0.From = 1;
                animation0.To = 0.5;
                animation0.Duration = new Duration(TimeSpan.FromMilliseconds(500));
                animation0.Completed += SlideCompleted;

                MainFrame.BeginAnimation(OpacityProperty, animation0);
            }
            _allowDirectNavigation = false;
        }
        private void SlideCompleted(object sender, EventArgs e)
        {
            _allowDirectNavigation = true;
            switch (_navArgs.NavigationMode)
            {
                case NavigationMode.New:
                    if (_navArgs.Uri == null)
                        MainFrame.Navigate(_navArgs.Content);
                    else
                        MainFrame.Navigate(_navArgs.Uri);
                    break;
                case NavigationMode.Back:
                    MainFrame.GoBack();
                    break;
                case NavigationMode.Forward:
                    MainFrame.GoForward();
                    break;
                case NavigationMode.Refresh:
                    MainFrame.Refresh();
                    break;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (ThreadStart)delegate ()
            {
                DoubleAnimation animation0 = new DoubleAnimation();
                animation0.From = 0.3;
                animation0.To = 1;
                animation0.Duration = new Duration(TimeSpan.FromMilliseconds(500));
                MainFrame.BeginAnimation(OpacityProperty, animation0);
            });
        }
        #endregion
    }
}

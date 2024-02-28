using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace sztu_xk
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.NavigationControl.SelectedItem = this.Home;
            this.rootFrame.Navigate(typeof(LoginPage));
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                if (this.rootFrame.CurrentSourcePageType != typeof(SettingPage))
                    this.rootFrame.Navigate(typeof(SettingPage));
            } else
            {
                if (args.SelectedItemContainer == this.Home)
                {
                    if (this.rootFrame.CurrentSourcePageType != typeof(HomePage))
                        this.rootFrame.Navigate(typeof(HomePage));
                }
            }
        }
    }
    class NavLink
    {
        public string Label { get; set; }
        public Symbol Symbol { get; set; }
        public Type DestPage { get; set; }
    }
}

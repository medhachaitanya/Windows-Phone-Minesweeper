using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Controls.Primitives;
using Languages.Resources;

namespace Minesweeper
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
           
            BackKeyPress += PageBackKeyPress;
        }

        private void PageBackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
#if WP8
                if (MessageBox.Show(AppResources.ExitQuestion, AppResources.exit, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    Data data = ((App)App.Current).data;
                    Data.Save(data);
                    Application.Current.Terminate();
                }
                else
                {
                    e.Cancel = true;
                }
#endif
        }

        // Simple button Click event handler to take us to the second page
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Data data = ((App)App.Current).data;
                data.NewGame();
                NavigationService.Navigate(new Uri("/GamePage.xaml", UriKind.Relative));
            }
            catch (Error)
            {
                MessageBox.Show(AppResources.WrongParamsMessage);
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            Data data = ((App)App.Current).data;
            LayoutRoot.DataContext = data.settings;

            data.page = Data.Page.Main;

            string value;

            if (NavigationContext.QueryString.TryGetValue("show_champions", out value))            
                mainPivot.SelectedIndex = 1;
            
            base.OnNavigatedTo(e);
        }
    }
}
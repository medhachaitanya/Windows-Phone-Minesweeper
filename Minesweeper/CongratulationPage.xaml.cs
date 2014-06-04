using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Languages.Resources;

namespace Minesweeper
{
    public partial class CongratulationPage : PhoneApplicationPage
    {
        public CongratulationPage()
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

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            Data data = ((App)App.Current).data;

            if (data.championIdx != -1)
            {
                string name = textName.Text;

                if (name == "")
                {
                    MessageBox.Show(AppResources.EnterNamePhrase);
                }
                else
                {
                    data.settings.champions[data.championIdx].name = name;
                    data.settings.champions[data.championIdx].seconds = (int)data.time / 1000;
                    NavigationService.Navigate(new Uri("/MainPage.xaml?show_champions", UriKind.Relative));
                    data.championIdx = -1;
                    NavigationService.RemoveBackEntry();
                }
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            ((App)App.Current).data.page = Data.Page.Congratulation;
            base.OnNavigatedTo(e);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WindowsPhone;


namespace Minesweeper
{
    public partial class GamePage : PhoneApplicationPage
    {
        private Game1 _game;

        public static GamePage Instance = null;

        public int screenHeight, screenWidth;

        // Constructor
        public GamePage()
        {
            InitializeComponent();
            
            Instance = this;
            screenWidth = 480;
            screenHeight = ResolutionHelper.CurrentResolution == Resolutions.HD ? 853 : 800;
            
            _game = XamlGame<Game1>.Create("", this);

            Unloaded += (s, e) => Dispatcher.BeginInvoke(() => _game.Dispose());
            BackKeyPress += (s, e) => e.Cancel = false;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            ((App)App.Current).data.page = Data.Page.Game;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }
    }
}
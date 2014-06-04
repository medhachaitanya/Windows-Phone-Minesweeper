using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Audio;
using Languages.Resources;

namespace Minesweeper
{
    public partial class GamePage : PhoneApplicationPage
    {
        GameTimer timer;
        ContentManager contentManager;
        SpriteBatch spriteBatch;
        int screenHeight 
        {
            get {
                if (Orientation == PageOrientation.Portrait || Orientation == PageOrientation.PortraitDown || Orientation == PageOrientation.PortraitUp)
                    return 800;
                else
                    return 480;
            }
        }

        int screenWidth
        {
            get
            {
                if (Orientation == PageOrientation.Portrait || Orientation == PageOrientation.PortraitDown || Orientation == PageOrientation.PortraitUp)
                    return 480;
                else
                    return 800;
            }
        }
        
        GameScreen gs;        

        public GamePage()
        {
            // Get the content manager from the application
            contentManager = (Application.Current as App).Content;

            // Create a timer for this page
            timer = new GameTimer();
            timer.UpdateInterval = TimeSpan.FromTicks(333333);
            timer.Update += OnUpdate;
            timer.Draw += OnDraw;         

            InitializeComponent();
            gs = new GameScreen();
        }

        protected void OrientationChangedHandler(Object o, EventArgs arguments)
        {
            gs.OnOrientationChanged((float)screenHeight, (float)screenWidth, Orientation);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ((App)App.Current).data.page = Data.Page.Game;

            // Set the sharing mode of the graphics device to turn on XNA rendering
            SharedGraphicsDeviceManager.Current.GraphicsDevice.SetSharingMode(true);
            // Start the timer
            timer.Start();
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(SharedGraphicsDeviceManager.Current.GraphicsDevice);

            gs.LoadContent(contentManager);
            gs.OnOrientationChanged((float)screenHeight, (float)screenWidth, Orientation);

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Stop the timer
            timer.Stop();
            // Set the sharing mode of the graphics device to turn off XNA rendering
            SharedGraphicsDeviceManager.Current.GraphicsDevice.SetSharingMode(false);
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Allows the page to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        private void OnUpdate(object sender, GameTimerEventArgs e)
        {
            gs.OnUpdate((float)e.TotalTime.TotalMilliseconds, (float)timer.UpdateInterval.Milliseconds);
        }

       
        /// <summary>
        /// Allows the page to draw itself.
        /// </summary>
        private void OnDraw(object sender, GameTimerEventArgs e)
        {
            SharedGraphicsDeviceManager.Current.GraphicsDevice.Clear(Color.White);
            gs.OnDraw(spriteBatch);
        }
    }
}
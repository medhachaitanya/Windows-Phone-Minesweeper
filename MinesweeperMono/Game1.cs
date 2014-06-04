using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Windows;
using Microsoft.Devices.Sensors;
using System.Windows.Threading;
using System.Threading;

namespace Minesweeper
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game
    {
        Accelerometer accelSensor;
        PageOrientation currentOrientation = PageOrientation.None;
        PageOrientation desiredOrientation = PageOrientation.None;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;        
        RenderTarget2D landscapeRenderTarget;
        bool useLandscapeRenderTarget = false;
        float renderRoatation;
        Vector2 renderOffset;
        Matrix fullTrans, rotateTrans;
        AccelerometerReadingEventArgs accelerometerReadingEventArgs;
        Mutex mut_accelerometerReadingEventArgs = new Mutex();
        long milliseconds_since_last_stable_position = 0;
        
        GameScreen gs;


        public int ScreenHeight
        {
            get
            {
                return GamePage.Instance.screenHeight;
            }
        }

        public int ScreenWidth
        {
            get
            {
                return GamePage.Instance.screenWidth;
            }
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            gs = new GameScreen();
            gs.transformGestureSample = TransformGestureSample;
            gs.transformTouchLocation = TransformTouchLocation;

            renderOffset = new Vector2();

            SetupScreenAutoScaling();

            accelSensor = new Accelerometer();
            accelSensor.ReadingChanged += new EventHandler<AccelerometerReadingEventArgs>(AccelerometerReadingChanged);
        }

        private void AccelerometerReadingChanged(object sender, AccelerometerReadingEventArgs e)
        {
            mut_accelerometerReadingEventArgs.WaitOne();
            accelerometerReadingEventArgs = e;
            mut_accelerometerReadingEventArgs.ReleaseMutex();
        }

        private void SetupScreenAutoScaling()
        {

            // Get the screen’s WVGA ''ScaleFactor'' via reflection.  scaleFactor will be 100 (WVGA), 160 (WXGA), or 150 (WXGA).
            int? scaleFactor = null;
            var content = App.Current.Host.Content;
            var scaleFactorProperty = content.GetType().GetProperty("ScaleFactor");

            if (scaleFactorProperty != null)
                scaleFactor = scaleFactorProperty.GetValue(content, null) as int?;

            if (scaleFactor == null)
                scaleFactor = 100; // 100% WVGA resolution

            double scale = ((double)scaleFactor) / 100.0; //scale from WVGA to Viewport Bounds

            // For 720P, we will scale to 150% WVGA resolution, resulting in a 1200x720 viewport inside a 1280x720 screen,
            // which is why letterboxing will be required.  By centering the viewport on the screen, it will be less noticeable.

            if (scaleFactor == 150)
            {
                // Centered letterboxing - move Margin.Left to the right by ((1280-1200)/2)/scale
                //GamePage.Instance.XnaSurface.Margin = new System.Windows.Thickness(40 / scale, 0, 0, 0);
            }

            // Scale the XnaSurface: 

            System.Windows.Media.ScaleTransform scaleTransform = new System.Windows.Media.ScaleTransform();
            scaleTransform.ScaleX = scaleTransform.ScaleY = scale;
            GamePage.Instance.XnaSurface.RenderTransform = scaleTransform;
        }

        private TouchLocation TransformTouchLocation(TouchLocation src)
        {
            return new TouchLocation(src.Id, src.State, Vector2.Transform(src.Position, fullTrans));
        }

        private GestureSample TransformGestureSample(GestureSample src)
        {                      
            return new GestureSample(src.GestureType, src.Timestamp,
                Vector2.Transform(src.Position, fullTrans),
                Vector2.Transform(src.Position2, fullTrans),
                Vector2.Transform(src.Delta, rotateTrans),
                Vector2.Transform(src.Delta2, rotateTrans));
        }

        public void SetOrientation(PageOrientation orientation)
        {
            if (orientation == PageOrientation.Landscape || orientation == PageOrientation.LandscapeLeft || orientation == PageOrientation.LandscapeRight)
            {
                gs.OnOrientationChanged(ScreenWidth, ScreenHeight, PageOrientation.Landscape);

                if (orientation == PageOrientation.LandscapeLeft)
                {
                    renderOffset = new Vector2(landscapeRenderTarget.Bounds.Height, 0);
                    renderRoatation = MathHelper.PiOver2;
                }
                else
                {
                    renderOffset = new Vector2(0, landscapeRenderTarget.Bounds.Width);
                    renderRoatation = -MathHelper.PiOver2;
                }
                
                useLandscapeRenderTarget = true;
                fullTrans = Matrix.Multiply(Matrix.CreateTranslation(-renderOffset.X, -renderOffset.Y, 0), Matrix.CreateRotationZ(-renderRoatation));
                rotateTrans = Matrix.CreateRotationZ(-renderRoatation);
            }
            else
            {
                gs.OnOrientationChanged(ScreenHeight, ScreenWidth, PageOrientation.Portrait);
                useLandscapeRenderTarget = false;
                fullTrans = Matrix.Identity;
                rotateTrans = Matrix.Identity;
            }

            currentOrientation = orientation;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            landscapeRenderTarget = new RenderTarget2D(graphics.GraphicsDevice, ScreenHeight, ScreenWidth);
            TouchPanel.DisplayOrientation = DisplayOrientation.LandscapeLeft;
            TouchPanel.DisplayWidth = ScreenHeight;
            TouchPanel.DisplayHeight = ScreenWidth;

            base.Initialize();
        }        

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            gs.LoadContent(Content);
            SetOrientation(PageOrientation.Portrait);

            try
            {
                accelSensor.Start();
            }
            catch (AccelerometerFailedException e)
            {
                
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            mut_accelerometerReadingEventArgs.WaitOne();

            if (accelerometerReadingEventArgs != null)
            {
                milliseconds_since_last_stable_position += (long)gameTime.ElapsedGameTime.TotalMilliseconds;

                double angle = Math.Atan2(-accelerometerReadingEventArgs.X, accelerometerReadingEventArgs.Y) * 180.0 / Math.PI;

                if (accelerometerReadingEventArgs.Z < -0.9)
                    milliseconds_since_last_stable_position = 0;

                if (angle > -45 && angle < 45 && desiredOrientation != PageOrientation.PortraitDown)
                {
                    milliseconds_since_last_stable_position = 0;
                    desiredOrientation = PageOrientation.PortraitDown;
                }
                if (angle > 45 && angle < 135 && desiredOrientation != PageOrientation.LandscapeLeft)
                {
                    milliseconds_since_last_stable_position = 0;
                    desiredOrientation = PageOrientation.LandscapeLeft;
                }
                if (angle > -135 && angle < -45 && desiredOrientation != PageOrientation.LandscapeRight)
                {
                    milliseconds_since_last_stable_position = 0;
                    desiredOrientation = PageOrientation.LandscapeRight;
                }
                if (((angle >= -180 && angle < -135) || (angle > 135 && angle <= 180)) && desiredOrientation != PageOrientation.PortraitUp)
                {
                    milliseconds_since_last_stable_position = 0;
                    desiredOrientation = PageOrientation.PortraitUp;
                }

                if (milliseconds_since_last_stable_position > 700 && desiredOrientation != currentOrientation)
                {
                    SetOrientation(desiredOrientation);
                }
            }

            mut_accelerometerReadingEventArgs.ReleaseMutex();

            // TODO: Add your update logic here
            gs.OnUpdate((float)gameTime.TotalGameTime.TotalMilliseconds, (float)gameTime.ElapsedGameTime.Milliseconds);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {

            if (useLandscapeRenderTarget)
                GraphicsDevice.SetRenderTarget(landscapeRenderTarget);

            graphics.GraphicsDevice.Clear(Color.White);
            gs.OnDraw(spriteBatch);

            if (useLandscapeRenderTarget)
            {
                GraphicsDevice.SetRenderTarget(null);
                spriteBatch.Begin();
                spriteBatch.Draw(landscapeRenderTarget, renderOffset, null, Color.White, renderRoatation, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}

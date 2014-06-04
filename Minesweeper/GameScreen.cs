using Languages.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Minesweeper
{
    class GameScreen
    {
        private float minScale = -1, maxScale = -1;
        private Microsoft.Xna.Framework.Rectangle flagsRect, timeRect, gameRect, barRect, tempRect;                
        private Vector2 pos;
        private XNAButton btnBack, btnFace, btnHelp;
        private AnimatedTexture[] textures;
        private Explosion explosion;
        private Texture2D topBackground, gameBackground;
        private SpriteFont font1;
        private SoundEffect explosionSound, flagSound, openSound, detectSound, winningSound;
        private const int duration_of_winning_sound = 5146;
        private int championIdx = -1;
        private DispatcherTimer timer;

        #region Variables for hold gesture recognition
       
        private const int acceptable_move = 5;
        private const int duration_of_hold_gesture = 700;
        private int current_pressed_time = 0;
        private bool perform_time_measurement = false;
        private Vector2 initial_pressd_position;
        
        #endregion
       
        private int CheckChampion()
        {
            Data data = ((App)App.Current).data;
            if (data.actionResult == ActionResult.Win)
            {
                foreach (Champion ch in data.settings.champions)
                {
                    if (ch.type == data.settings.GetChampionType())
                    {
                        if (ch.seconds > data.time / 1000)
                            return data.settings.champions.IndexOf(ch);
                    }
                }
            }
            return -1;
        }
        /// <summary>
        /// Draws the given string as large as possible inside the boundaries Rectangle without going
        /// outside of it.  This is accomplished by scaling the string (since the SpriteFont has a specific
        /// size).
        /// 
        /// If the string is not a perfect match inside of the boundaries (which it would rarely be), then
        /// the string will be absolutely-centered inside of the boundaries.
        /// </summary>
        /// <param name="font"></param>
        /// <param name="strToDraw"></param>
        /// <param name="boundaries"></param>
        private void DrawString(SpriteBatch spriteBatch, SpriteFont font, string strToDraw, Microsoft.Xna.Framework.Rectangle boundaries, Color color)
        {
            Vector2 size = font.MeasureString(strToDraw);

            float xScale = (boundaries.Width / size.X);
            float yScale = (boundaries.Height / size.Y);

            // Taking the smaller scaling value will result in the text always fitting in the boundaires.
            float scale = Math.Min(xScale, yScale);

            // Figure out the location to absolutely-center it in the boundaries rectangle.
            int strWidth = (int)Math.Round(size.X * scale);
            int strHeight = (int)Math.Round(size.Y * scale);
            Vector2 position = new Vector2();
            position.X = (((boundaries.Width - strWidth) / 2) + boundaries.X);
            position.Y = (((boundaries.Height - strHeight) / 2) + boundaries.Y);

            // A bunch of settings where we just want to use reasonable defaults.
            float rotation = 0.0f;
            Vector2 spriteOrigin = new Vector2(0, 0);
            float spriteLayer = 0.0f; // all the way in the front
            SpriteEffects spriteEffects = new SpriteEffects();

            // Draw the string to the sprite batch!
            spriteBatch.DrawString(font, strToDraw, position, color, rotation, spriteOrigin, scale, spriteEffects, spriteLayer);
        }
        private void DarwBackground(SpriteBatch spriteBatch, Texture2D texture, Microsoft.Xna.Framework.Rectangle rect)
        {
            int w = texture.Width,
                h = texture.Height;

            tempRect.X = 0;
            tempRect.Y = 0;

            for (int i = 0; h * i < rect.Height; i++)
            {
                for (int j = 0; w * j < rect.Width; j++)
                {
                    pos.X = rect.X + w * j;
                    pos.Y = rect.Y + h * i;

                    tempRect.Height = h;
                    if (h * (i + 1) > rect.Height)
                        tempRect.Height = rect.Height - h * i;

                    tempRect.Width = w;
                    if (w * (j + 1) > rect.Width)
                        tempRect.Width = rect.Width - w * j;

                    spriteBatch.Draw(texture, pos, tempRect, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
                }
            }
        }
        private void buttonBack_Click()
        { 
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            });
        }
        private void buttonFace_Click()
        {
            Data data = ((App)App.Current).data;
            data.NewGame();
            btnFace.SelectActiveTexture((int)data.actionResult);
        }
        private void buttonHelp_Click()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {

                MessageBox.Show(
                    AppResources.HelpMessage_line1 + "\n\n" +
                    AppResources.HelpMessage_line2 + "\n\n" +
                    AppResources.HelpMessage_line3 + "\n\n" +
                    AppResources.HelpMessage_line4 + "\n\n" +
                    AppResources.HelpMessage_line5 + "\n\n" +
                    AppResources.HelpMessage_line6, AppResources.help, MessageBoxButton.OK);
            });
        }

        public GameScreen()
        {
            //Initializing of fields           
            textures = new AnimatedTexture[(int)CellContent.Count];
            pos = new Vector2(0, 0);            
            tempRect = new Microsoft.Xna.Framework.Rectangle();
            
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, duration_of_winning_sound / 1000, duration_of_winning_sound % 1000);
            timer.Tick += timer_Tick;
        }

        public delegate GestureSample TransformGestureSample(GestureSample src);
        public TransformGestureSample transformGestureSample;
        public TransformTouchLocation transformTouchLocation;
        
        public void LoadContent(ContentManager contentManager)
        {
            Data data = ((App)App.Current).data;

            textures[(int)CellContent.FiredMine] = new AnimatedTexture(contentManager, "fired_mine", 3, 1);
            textures[(int)CellContent.Mine] = new AnimatedTexture(contentManager, "mine");
            textures[(int)CellContent.Unknown] = new AnimatedTexture(contentManager, "unknown");
            textures[(int)CellContent.Flag] = new AnimatedTexture(contentManager, "flag");
            textures[(int)CellContent.WrongFlag] = new AnimatedTexture(contentManager, "wrong_flag", 6, 6);
            textures[(int)CellContent.Number + 0] = new AnimatedTexture(contentManager, "cell_0");
            textures[(int)CellContent.Number + 1] = new AnimatedTexture(contentManager, "cell_1");
            textures[(int)CellContent.Number + 2] = new AnimatedTexture(contentManager, "cell_2");
            textures[(int)CellContent.Number + 3] = new AnimatedTexture(contentManager, "cell_3");
            textures[(int)CellContent.Number + 4] = new AnimatedTexture(contentManager, "cell_4");
            textures[(int)CellContent.Number + 5] = new AnimatedTexture(contentManager, "cell_5");
            textures[(int)CellContent.Number + 6] = new AnimatedTexture(contentManager, "cell_6");
            textures[(int)CellContent.Number + 7] = new AnimatedTexture(contentManager, "cell_7");
            textures[(int)CellContent.Number + 8] = new AnimatedTexture(contentManager, "cell_8");

            explosion = new Explosion(contentManager, "explosion");
            explosionSound = contentManager.Load<SoundEffect>("explosionSound");
            flagSound = contentManager.Load<SoundEffect>("flagSound");
            detectSound = contentManager.Load<SoundEffect>("detectSound");
            openSound = contentManager.Load<SoundEffect>("openSound");
            winningSound = contentManager.Load<SoundEffect>("winningSound");


            btnBack = new XNAButton(contentManager, "back", buttonBack_Click);
            btnBack.transformTouchLocation = transformTouchLocation;
            btnHelp = new XNAButton(contentManager, "help", buttonHelp_Click);
            btnHelp.transformTouchLocation = transformTouchLocation;

            btnFace = new XNAButton(contentManager, "ordinary_face", buttonFace_Click);
            btnFace.transformTouchLocation = transformTouchLocation;
            btnFace.AddTexture(contentManager, "winner_face");
            btnFace.AddTexture(contentManager, "loser_face");
            btnFace.SelectActiveTexture((int)data.actionResult);

            font1 = contentManager.Load<SpriteFont>("SpriteFont1");

            topBackground = contentManager.Load<Texture2D>("topBackground");
            gameBackground = contentManager.Load<Texture2D>("gameBackground");

            TouchPanel.EnabledGestures = GestureType.Tap | GestureType.FreeDrag | GestureType.Pinch;
        }

        public void OnOrientationChanged(float h, float w, PageOrientation orientation)
        {
            Data data = ((App)App.Current).data;

            if (orientation == PageOrientation.Portrait || orientation == PageOrientation.PortraitUp || orientation == PageOrientation.PortraitDown)
            {
                float topShift = h / 8;
                float emptySpace = topShift / 10;
                float btnSize = topShift - 2 * emptySpace;

                float rectWidth = (w - 3 * btnSize - 6 * emptySpace) / 2;

                flagsRect = new Microsoft.Xna.Framework.Rectangle((int)emptySpace, (int)emptySpace, (int)rectWidth, (int)(topShift - 2 * emptySpace));
                timeRect = new Microsoft.Xna.Framework.Rectangle((int)(w - emptySpace - rectWidth), (int)emptySpace, (int)rectWidth, (int)(topShift - 2 * emptySpace));
                gameRect = new Microsoft.Xna.Framework.Rectangle(0, (int)topShift, (int)w, (int)(h - topShift));
                barRect = new Microsoft.Xna.Framework.Rectangle(0, 0, (int)w, (int)topShift - 1);

                btnBack.rect = new Microsoft.Xna.Framework.Rectangle((int)(w / 2 - btnSize / 2 - emptySpace - btnSize), (int)emptySpace, (int)btnSize, (int)btnSize);
                btnFace.rect = new Microsoft.Xna.Framework.Rectangle((int)(w / 2 - btnSize / 2), (int)emptySpace, (int)btnSize, (int)btnSize);
                btnHelp.rect = new Microsoft.Xna.Framework.Rectangle((int)(w / 2 + btnSize / 2 + emptySpace), (int)emptySpace, (int)btnSize, (int)btnSize);
            }

            if (orientation == PageOrientation.Landscape || orientation == PageOrientation.LandscapeLeft || orientation == PageOrientation.LandscapeRight)
            {
                float leftShift = w / 8;
                float emptySpace = leftShift / 10;
                float btnSize = leftShift - 2 * emptySpace;

                float rectHeight = (h - 3 * btnSize - 6 * emptySpace) / 2;

                flagsRect = new Microsoft.Xna.Framework.Rectangle((int)emptySpace, (int)emptySpace, (int)(leftShift - 2 * emptySpace), (int)rectHeight);
                timeRect = new Microsoft.Xna.Framework.Rectangle((int)emptySpace, (int)(h - emptySpace - rectHeight), (int)(leftShift - 2 * emptySpace), (int)rectHeight);
                gameRect = new Microsoft.Xna.Framework.Rectangle((int)leftShift, 0, (int)(w - leftShift), (int)h);
                barRect = new Microsoft.Xna.Framework.Rectangle(0, 0, (int)leftShift - 1, (int)h);

                btnBack.rect = new Microsoft.Xna.Framework.Rectangle((int)emptySpace, (int)(h / 2 - btnSize / 2 - emptySpace - btnSize), (int)btnSize, (int)btnSize);
                btnFace.rect = new Microsoft.Xna.Framework.Rectangle((int)emptySpace, (int)(h / 2 - btnSize / 2), (int)btnSize, (int)btnSize);
                btnHelp.rect = new Microsoft.Xna.Framework.Rectangle((int)emptySpace, (int)(h / 2 + btnSize / 2 + emptySpace), (int)btnSize, (int)btnSize);
            }

            if (Math.Abs(data.scale - minScale) < 0.001)
                data.scale = -1;

            float scaleX = (float)gameRect.Width / (float)(textures[(int)CellContent.Unknown].Width * data.game.width),
                  scaleY = (float)gameRect.Height / (float)(textures[(int)CellContent.Unknown].Height * data.game.height);

            minScale = (float)scaleX < scaleY ? scaleX : scaleY;
            maxScale = (float)(w / (textures[(int)CellContent.Unknown].Width * 3));
        }        
        public void OnUpdate(float total_milliseconds, float elapsed_milliseconds)
        {
            Data data = ((App)App.Current).data;

            if (data.actionResult == ActionResult.Continuation && data.game.wasInitialized)
                data.time += (int)elapsed_milliseconds;

            foreach (AnimatedTexture at in textures)
                at.UpdateFrame((float)(elapsed_milliseconds / 1000.0));

            explosion.Update(total_milliseconds);

            TouchCollection tc = TouchPanel.GetState();

            #region Hold gesture recognition

            if (tc.Count == 1 && tc[0].State == TouchLocationState.Pressed)
            {
                perform_time_measurement = true;
                current_pressed_time = 0;
                initial_pressd_position = tc[0].Position;
            }

            if (tc.Count >= 1 && tc[0].State != TouchLocationState.Pressed)
            {
                float dx = Math.Abs(tc[0].Position.X - initial_pressd_position.X),
                      dy = Math.Abs(tc[0].Position.Y - initial_pressd_position.Y);

                if (dx > acceptable_move || dy > acceptable_move)
                    perform_time_measurement = false;
            }

            if (perform_time_measurement)
                current_pressed_time += (int)elapsed_milliseconds;                                       

            #endregion            

            #region Control buttons manage

            btnBack.Update(tc);
            btnFace.Update(tc);
            btnHelp.Update(tc);

            #endregion

            float displayed_cell_size = textures[(int)CellContent.Unknown].Width * data.scale;

            while (TouchPanel.IsGestureAvailable || current_pressed_time > duration_of_hold_gesture)
            {
                GestureSample gesture, originalGesture;

                if (current_pressed_time > duration_of_hold_gesture)
                {
                    originalGesture = new GestureSample(GestureType.Hold, TimeSpan.Zero, initial_pressd_position, Vector2.Zero, Vector2.Zero, Vector2.Zero);                    
                }
                else
                {
                    originalGesture = TouchPanel.ReadGesture();
                }

                gesture = transformGestureSample == null ? originalGesture : transformGestureSample(originalGesture);

                perform_time_measurement = false;
                current_pressed_time = 0;

                #region Screen view manage
                if (gesture.GestureType == GestureType.FreeDrag || gesture.GestureType == GestureType.Pinch)
                {
                    float dx = 0, dy = 0, ds = 1;

                    if (gesture.GestureType == GestureType.FreeDrag)
                    {
                        dx = gesture.Delta.X;
                        dy = gesture.Delta.Y;
                    }

                    if (gesture.GestureType == GestureType.Pinch)
                    {
                        float ex0 = gesture.Position.X,
                              ey0 = gesture.Position.Y,
                              ex1 = gesture.Position2.X,
                              ey1 = gesture.Position2.Y;

                        float bx0 = ex0 - gesture.Delta.X,
                              by0 = ey0 - gesture.Delta.Y,
                              bx1 = ex1 - gesture.Delta2.X,
                              by1 = ey1 - gesture.Delta2.Y;

                        float sqr_begin_diag = (bx1 - bx0) * (bx1 - bx0) + (by1 - by0) * (by1 - by0);

                        float sqr_end_diag = (ex1 - ex0) * (ex1 - ex0) + (ey1 - ey0) * (ey1 - ey0);

                        ds = (float)Math.Sqrt(sqr_end_diag / sqr_begin_diag);

                        //Correction of scale
                        if (data.scale * ds < minScale) ds = minScale / data.scale;
                        if (data.scale * ds > maxScale) ds = maxScale / data.scale;

                        if (Math.Abs(ds - 1.0) > 1e-5)
                        {
                            float bcx = (bx0 + bx1) / 2,
                                  bcy = (by0 + by1) / 2;

                            dx = (bcx - data.shift_x) * (1 - ds);
                            dy = (bcy - data.shift_y) * (1 - ds);
                        }
                    }

                    //Correction of shifts
                    float x0 = gameRect.X + data.shift_x + dx,
                          y0 = gameRect.Y + data.shift_y + dy,
                          x1 = gameRect.X + data.shift_x + data.game.width * displayed_cell_size * ds + dx,
                          y1 = gameRect.Y + data.shift_y + data.game.height * displayed_cell_size * ds + dy;

                    if (x1 - x0 > gameRect.Width)
                    {
                        if (x0 > gameRect.X) dx -= x0 - gameRect.X;
                        if (x1 < gameRect.X + gameRect.Width) dx += (float)(gameRect.X + gameRect.Width) - x1;
                    }
                    else
                    {
                        if (x0 < gameRect.X) dx -= x0 - gameRect.X;
                        if (x1 > gameRect.X + gameRect.Width) dx += (float)(gameRect.X + gameRect.Width) - x1;
                    }

                    if (y1 - y0 > gameRect.Height/* ActualHeight - topShift*/)
                    {
                        if (y0 > gameRect.Y/*topShift*/) dy -= y0 - gameRect.Y/*topShift*/;
                        if (y1 < gameRect.Y + gameRect.Height/*ActualHeight*/) dy += (float)(gameRect.Y + gameRect.Height)/*ActualHeight*/ - y1;
                    }
                    else
                    {
                        if (y0 < gameRect.Y/*topShift*/) dy -= y0 - gameRect.Y/*topShift*/;
                        if (y1 > gameRect.Y + gameRect.Height/*ActualHeight*/) dy += (float)(gameRect.Y + gameRect.Height)/*ActualHeight*/ - y1;
                    }

                    //Applaying of changes
                    data.shift_x += dx;
                    data.shift_y += dy;
                    data.scale *= ds;
                }

                #endregion

                #region Game process manage
                if (gameRect.Contains((int)gesture.Position.X, (int)gesture.Position.Y))
                {
                    if ((gesture.GestureType == GestureType.Tap || gesture.GestureType == GestureType.Hold) && data.actionResult == ActionResult.Continuation)
                    {
                        int selected_j = (int)((gesture.Position.X - gameRect.X - data.shift_x) / displayed_cell_size);
                        int selected_i = (int)((gesture.Position.Y - gameRect.Y - data.shift_y) / displayed_cell_size);

                        PerformedAction performedAction;
                        data.actionResult = data.game.TakeAction(selected_i, selected_j, gesture.GestureType, out performedAction);
                        //data.actionResult = ActionResult.Win;
                        btnFace.SelectActiveTexture((int)data.actionResult);

                        if (data.actionResult == ActionResult.Continuation && data.settings.soundEffects)
                        {
                            switch (performedAction)
                            {
                                case PerformedAction.Falg:
                                    flagSound.Play(0.25f, 0f, 0f);
                                    break;
                                case PerformedAction.Open:
                                    openSound.Play();
                                    break;
                                case PerformedAction.MineDetector:
                                    detectSound.Play(0.25f, 0f, 0f);
                                    break;
                            }
                        }

                        if (data.actionResult == ActionResult.Losing)
                        {
                            explosion.Create(gesture.Position, 30, displayed_cell_size, 1000.0f, total_milliseconds);
                            if (data.settings.soundEffects)
                                explosionSound.Play();
                        }

                        if (data.actionResult == ActionResult.Win)
                        {
                            if (data.settings.soundEffects)
                                winningSound.Play();

                            data.championIdx = CheckChampion();

                            if (data.championIdx != -1)
                            {
                                Deployment.Current.Dispatcher.BeginInvoke(() =>
                                {
                                    timer.Start();
                                });
                            }
                        }
                    }
                }
                #endregion
            }

        }

        private void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/CongratulationPage.xaml", UriKind.Relative));
        }

        public void OnDraw(SpriteBatch spriteBatch)
        {
            Data data = ((App)App.Current).data;

            if (data.scale < 0)
            {
                data.scale = minScale;
                data.shift_x = 0;
                data.shift_y = 0;
            }
            
            spriteBatch.Begin();

            DarwBackground(spriteBatch, gameBackground, gameRect);

            for (int i = 0; i < data.game.height; i++)
            {
                for (int j = 0; j < data.game.width; j++)
                {
                    pos.X = (float)(gameRect.X + data.shift_x + j * textures[(int)CellContent.Unknown].Width * data.scale);
                    pos.Y = (float)(gameRect.Y + data.shift_y + i * textures[(int)CellContent.Unknown].Height * data.scale);

                    if (pos.X + textures[(int)CellContent.Unknown].Width * data.scale < gameRect.X ||
                        pos.Y + textures[(int)CellContent.Unknown].Height * data.scale < gameRect.Y ||
                        pos.X > gameRect.X + gameRect.Width ||
                        pos.Y > gameRect.Y + gameRect.Height)
                        continue;

                    int number = 0;
                    CellContent cellContent = data.game.GetCellContent(i, j, out number);

                    textures[(int)cellContent + number].DrawFrame(spriteBatch, pos, Color.White, 0, Vector2.Zero, data.scale, SpriteEffects.None, 1);
                }
            }

            DarwBackground(spriteBatch, topBackground, barRect);

            btnBack.Draw(spriteBatch);
            btnFace.Draw(spriteBatch);
            btnHelp.Draw(spriteBatch);

            DrawString(spriteBatch, font1, (data.settings.number_of_landmines - data.game.number_of_flags).ToString(), flagsRect, Color.Red);
            int seconds = (int)data.time / 1000;
            DrawString(spriteBatch, font1, seconds.ToString(), timeRect, Color.Red);

            spriteBatch.End();

            explosion.Draw(spriteBatch);
        }
    }
}

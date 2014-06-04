using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Minesweeper
{
    public delegate TouchLocation TransformTouchLocation(TouchLocation src);

    class XNAButton
    {
        public delegate void ClickProc();
        public TransformTouchLocation transformTouchLocation;
        
        List<Texture2D> texture;
        Texture2D activeTexture;
        ClickProc clickProc;
        Vector2 pos, scale;
        bool pressed;
        Rectangle leftRect, rightRect;
        
        public Rectangle rect;

        public XNAButton(ContentManager contentManager, string textureName, ClickProc clickProc)
        {
            texture = new List<Texture2D>();
            texture.Add(contentManager.Load<Texture2D>(textureName));
            this.clickProc = clickProc;
            TouchPanel.EnabledGestures |= GestureType.Tap;

            rect = new Rectangle();
            pos = new Vector2();
            scale = new Vector2();

            pressed = false;
            SelectActiveTexture(0);
        }

        public void AddTexture(ContentManager contentManager, string textureName)
        {
            texture.Add(contentManager.Load<Texture2D>(textureName));
        }

        public void SelectActiveTexture(int index)
        {
            activeTexture = texture[index];
            leftRect = new Rectangle(0, 0, activeTexture.Width / 2, activeTexture.Height);
            rightRect = new Rectangle(activeTexture.Width / 2, 0, activeTexture.Width / 2, activeTexture.Height);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            scale.X = (float)rect.Width / ((float)activeTexture.Width / 2);
            scale.Y = (float)rect.Height / (float)activeTexture.Height;
            pos.X = rect.X;
            pos.Y = rect.Y;

            if(!pressed)
                spriteBatch.Draw(activeTexture, pos, leftRect, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 1);
            else
                spriteBatch.Draw(activeTexture, pos, rightRect, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 1);
        }

        public void Update(TouchCollection tc)
        {
            if (tc.Count == 1)
            {
                pressed = false;

                TouchLocation original_tl = tc[0];
                TouchLocation tl = transformTouchLocation == null ? original_tl : transformTouchLocation(original_tl);


                if (rect.Contains((int)tl.Position.X, (int)tl.Position.Y))
                {
                    if (tl.State == TouchLocationState.Pressed || tl.State == TouchLocationState.Moved)
                        pressed = true;

                    if (tl.State == TouchLocationState.Released)
                        clickProc();
                }
            }
        }
    }
}

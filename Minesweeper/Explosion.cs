using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Minesweeper
{
    public class Explosion
    {
        struct ParticleData
        {
           public float BirthTime;
           public float MaxAge;
           public Vector2 OrginalPosition;
           public Vector2 Accelaration;
           public Vector2 Direction;
           public Vector2 Position;
           public float Scaling;
           public Color ModColor;
        }

        private List<ParticleData> particleList;
        private Texture2D texture;
        private Random randomizer;

        public Explosion(ContentManager content, string asset)
        {
            texture = content.Load<Texture2D>(asset);
            particleList = new List<ParticleData>();
            randomizer = new Random();           
        }

        public void Create(Vector2 explosionPos, int numberOfParticles, float size, float maxAge, float milliseconds)
        {
            particleList.Clear();

            for (int i = 0; i < numberOfParticles; i++)
            {
                ParticleData particle = new ParticleData();

                particle.OrginalPosition = explosionPos;
                particle.Position = particle.OrginalPosition;

                particle.BirthTime = milliseconds;
                particle.MaxAge = maxAge;
                particle.Scaling = 0.25f;
                particle.ModColor = Color.White;

                float particleDistance = (float)randomizer.NextDouble() * size;
                Vector2 displacement = new Vector2(particleDistance, 0);
                float angle = MathHelper.ToRadians(randomizer.Next(360));
                displacement = Vector2.Transform(displacement, Matrix.CreateRotationZ(angle));

                particle.Direction = displacement;
                particle.Accelaration = 3.0f * particle.Direction;

                particleList.Add(particle);
            }
        }

        public void Update(float milliseconds)
        {
            if (particleList.Count == 0)
                return;

            float now = milliseconds;
            for (int i = particleList.Count - 1; i >= 0; i--)
            {
                ParticleData particle = particleList[i];
                float timeAlive = now - particle.BirthTime;

                if (timeAlive > particle.MaxAge)
                {
                    particleList.RemoveAt(i);
                }
                else
                {
                    float relAge = timeAlive / particle.MaxAge;
                    particle.Position = 0.5f * particle.Accelaration * relAge * relAge + particle.Direction * relAge + particle.OrginalPosition;

                    float invAge = 1.0f - relAge;
                    particle.ModColor = new Color(new Vector4(invAge, invAge, invAge, invAge));

                    Vector2 positionFromCenter = particle.Position - particle.OrginalPosition;
                    float distance = positionFromCenter.Length();
                    particle.Scaling = (50.0f + distance) / 200.0f;

                    particleList[i] = particle;
                }
            }
        }
        
        public void Draw(SpriteBatch batch)
        {
            if (particleList.Count == 0) 
                return;

            batch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            for (int i = 0; i < particleList.Count; i++)
            {
                ParticleData particle = particleList[i];
                batch.Draw(texture, particle.Position, null, particle.ModColor, i, new Vector2(256, 256), particle.Scaling, SpriteEffects.None, 1);
            }

            batch.End();
        }
    }
}

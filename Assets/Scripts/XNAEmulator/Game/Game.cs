using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework
{
    public class Game : IDisposable
	{
		private GameComponentCollection _components;
        Content.ContentManager content;
		GraphicsDevice graphicsDevice;
		DrawQueue drawQueue;
        long totalTicks = 0;
        private bool INTERNAL_isMouseVisible;
        private bool INTERNAL_isActive;
        private bool isDisposed;

		public DrawQueue DrawQueue {
			get {
				return this.drawQueue;
			}
			set {
				drawQueue = value;
			}
		}
        public ContentManager Content
        {
            get { return this.content; }
            set { this.content = value; }
        }

        public bool IsMouseVisible
        {
	        get
	        {
		        return this.INTERNAL_isMouseVisible;
	        }
	        set
	        {
		        if (this.INTERNAL_isMouseVisible == value)
			        return;
		        this.INTERNAL_isMouseVisible = value;
		        //FNAPlatform.OnIsMouseVisibleChanged(value);
	        }
        }

        public bool IsActive
        {
	        get
	        {
		        return this.INTERNAL_isActive;
	        }
	        internal set
	        {
		        if (this.INTERNAL_isActive == value)
			        return;
		        this.INTERNAL_isActive = value;
		        if (this.INTERNAL_isActive)
			        this.OnActivated((object) this, EventArgs.Empty);
		        else
			        this.OnDeactivated((object) this, EventArgs.Empty);
	        }
        }

        public GraphicsDevice GraphicsDevice
        {
            get
            {
				if(graphicsDevice == null)
					graphicsDevice = new GraphicsDevice(DrawQueue);
				
				return graphicsDevice;
            }
        }

        public event EventHandler<EventArgs> Activated;

        public event EventHandler<EventArgs> Deactivated;

        public Game()
        {
            content = new ContentManager(null, "");
			
			_components = new GameComponentCollection();	
        }

        protected virtual void Update(GameTime gameTime)
        {  
        }
		
		public GameComponentCollection Components
		{
			get
			{
				return this._components;
			}
		}

        protected virtual void Draw(GameTime gameTime)
        {
        }
        protected virtual bool BeginDraw()
        {
            return true;
        }
        protected virtual void LoadContent()
        {
        }
        protected virtual void Exit()
        {
        }

        public GameWindow Window
        {
            get
            {
                // TODO
                return new UnityGameWindow();
            }
        }

        public GameServiceContainer Services
        {
            get
            {
                // TODO
                return null;
            }
        }

        internal void Run()
        {
            throw new NotImplementedException();
        }
        protected virtual void Dispose(bool disposing)
        {
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }



        internal void Begin()
        {
            LoadContent();
			// XNA's first update call has a zero elapsed time, so do one now.
			GameTime gameTime = new GameTime(new TimeSpan(0), new TimeSpan(0), new TimeSpan(0, 0, 0, 0, 0), new TimeSpan(0, 0, 0, 0, 0));
			Update(gameTime);
        }

        internal void Tick(float deltaTime)
        {
            long microseconds = (int)(deltaTime * 1000000);
			long ticks = microseconds * 10;
            totalTicks += ticks;
            GameTime gameTime = new GameTime(new TimeSpan(0), new TimeSpan(0), new TimeSpan(totalTicks), new TimeSpan(ticks));
            Update(gameTime);
            Draw(gameTime);
        }

        protected virtual void OnActivated(object sender, EventArgs args)
        {
	        this.AssertNotDisposed();
	        if (this.Activated == null)
		        return;
	        this.Activated((object) this, args);
        }

        protected virtual void OnDeactivated(object sender, EventArgs args)
        {
	        this.AssertNotDisposed();
	        if (this.Deactivated == null)
		        return;
	        this.Deactivated((object) this, args);
        }


        [DebuggerNonUserCode]
        private void AssertNotDisposed()
        {
	        if (this.isDisposed)
	        {
		        string name = this.GetType().Name;
		        throw new ObjectDisposedException(name, string.Format("The {0} object was used after being Disposed.", (object) name));
	        }
        }

        protected virtual void Initialize()
        {
            INTERNAL_isActive = true;
        }

        protected virtual void OnExiting(object sender, EventArgs args)
        {
        }

        public virtual void UnloadContent()
        {
        }       

    }
}

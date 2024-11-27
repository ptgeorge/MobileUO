using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XNAEmulator.Graphics;

namespace Microsoft.Xna.Framework.Graphics
{
    public class GraphicsDevice
    {
        Viewport viewport = new Viewport();
        private DrawQueue drawQueue;

        public DrawQueue DrawQueue
        {
            get { return drawQueue; }
            set { drawQueue = value; }
        }

        public GraphicsDevice(DrawQueue drawQueue)
        {
            // TODO: Complete member initialization
            this.drawQueue = drawQueue;
			
			viewport = new Viewport(0,0,UnityEngine.Screen.width, UnityEngine.Screen.height);
        }

        public Viewport Viewport
        {
            get { return viewport; }
            set { viewport = value; }
        }

        public Rectangle ScissorRectangle { get; set; }
        public Color BlendFactor { get; set; }
        public BlendState BlendState { get; set; }
        public DepthStencilState DepthStencilState { get; set; }
        public RasterizerState RasterizerState { get; set; }
        public Texture2D[] Textures = new Texture2D[3];
        public SamplerStateCollection SamplerStates { get; }
        public IndexBuffer Indices { get; set; }


        internal void Clear(Color color)
        {
            
        }

        public void DrawIndexedPrimitives(PrimitiveType triangleList, int i, int i1, int i2, int i3, int i4)
        {
        }
    }
}

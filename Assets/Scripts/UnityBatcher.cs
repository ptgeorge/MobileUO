using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UnityEngine;
using UnityEngine.Rendering;
using BlendState = Microsoft.Xna.Framework.Graphics.BlendState;
using Color = UnityEngine.Color;
using CompareFunction = Microsoft.Xna.Framework.Graphics.CompareFunction;
using Quaternion = UnityEngine.Quaternion;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using UnityTexture = UnityEngine.Texture;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace ClassicUO.Renderer
{
    public sealed unsafe class UltimaBatcher2D : IDisposable
    {
        private static readonly float[] _cornerOffsetX = new float[] { 0.0f, 1.0f, 0.0f, 1.0f };
        private static readonly float[] _cornerOffsetY = new float[] { 0.0f, 0.0f, 1.0f, 1.0f };

        private const int MAX_SPRITES = 0x800;
        private const int MAX_VERTICES = MAX_SPRITES * 4;
        private const int MAX_INDICES = MAX_SPRITES * 6;
        private BlendState _blendState;
        private int _currentBufferPosition;

        private Effect _customEffect;

        private readonly IndexBuffer _indexBuffer;
        private int _numSprites;
        private Matrix _projectionMatrix = new Matrix
        (
            0f, //(float)( 2.0 / (double)viewport.Width ) is the actual value we will use
            0.0f,
            0.0f,
            0.0f,
            0.0f,
            0f, //(float)( -2.0 / (double)viewport.Height ) is the actual value we will use
            0.0f,
            0.0f,
            0.0f,
            0.0f,
            1.0f,
            0.0f,
            -1.0f,
            1.0f,
            0.0f,
            1.0f
        );
        private readonly RasterizerState _rasterizerState;
        private SamplerState _sampler;
        private bool _started;
        private DepthStencilState _stencil;
        private Matrix _transformMatrix;
        private readonly DynamicVertexBuffer _vertexBuffer;
        private readonly BasicUOEffect _basicUOEffect;
        private Texture2D[] _textureInfo;
        private PositionNormalTextureColor4[] _vertexInfo;

        // MobileUO: Class members
        private Material hueMaterial;
        private Material xbrMaterial;
        private MeshHolder reusedMesh = new MeshHolder(1);

        public float scale = 1;
        
        public bool UseGraphicsDrawTexture;

        private Mesh draw2DMesh;
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int Hue = Shader.PropertyToID("_Hue");
        private static readonly int HueTex1 = Shader.PropertyToID("_HueTex1");
        private static readonly int HueTex2 = Shader.PropertyToID("_HueTex2");
        private static readonly int UvMirrorX = Shader.PropertyToID("_uvMirrorX");
        private static readonly int Scissor = Shader.PropertyToID("_Scissor");
        private static readonly int ScissorRect = Shader.PropertyToID("_ScissorRect");
        private static readonly int TextureSize = Shader.PropertyToID("textureSize");
        // End of MobileUO Class members

        public UltimaBatcher2D(GraphicsDevice device)
        {
            GraphicsDevice = device;
            // MobileUO: Removed texture, vertex, & index buffer config
            /*
            _textureInfo = new Texture2D[MAX_SPRITES];
            _vertexInfo = new PositionNormalTextureColor4[MAX_SPRITES];
            _vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(PositionNormalTextureColor4), MAX_VERTICES, BufferUsage.WriteOnly);
            _indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, MAX_INDICES, BufferUsage.WriteOnly);
            _indexBuffer.SetData(GenerateIndexArray());
            */

            _blendState = BlendState.AlphaBlend;
            _sampler = SamplerState.PointClamp;
            _rasterizerState = new RasterizerState
            {
                CullMode = CullMode.CullCounterClockwiseFace,
                FillMode = FillMode.Solid,
                DepthBias = 0,
                MultiSampleAntiAlias = true,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0,
            };

            _stencil = Stencil;

            _basicUOEffect = new BasicUOEffect(device);

            // MobileUO: Set up hueMaterial and xbrMaterial
            hueMaterial = new Material(UnityEngine.Resources.Load<Shader>("HueShader"));
            xbrMaterial = new Material(UnityEngine.Resources.Load<Shader>("XbrShader"));
        }
        private Matrix TransformMatrix => _transformMatrix;

        private DepthStencilState Stencil { get; } = new DepthStencilState
        {
            StencilEnable = false,
            DepthBufferEnable = false,
            StencilFunction = CompareFunction.NotEqual,
            ReferenceStencil = -1,
            StencilMask = -1,
            StencilFail = StencilOperation.Keep,
            StencilDepthBufferFail = StencilOperation.Keep,
            StencilPass = StencilOperation.Keep
        };

        public GraphicsDevice GraphicsDevice { get; }

        public int TextureSwitches, FlushesDone;



        public void Dispose()
        {
            _vertexInfo = null;
            _basicUOEffect?.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }

        public void SetBrightlight(float f)
        {
            _basicUOEffect.Brighlight.SetValue(f);
        }

        // MobileUO: Switched from Vector3 to XnaVector3
        public void DrawString(SpriteFont spriteFont, ReadOnlySpan<char> text, int x, int y, ref XnaVector3 color)
            => DrawString(spriteFont, text, new Vector2(x, y), color);

        // MobileUO: Switched from Vector2/3 to XnaVector2/3
        public void DrawString(SpriteFont spriteFont, ReadOnlySpan<char> text, ref XnaVector2 position, ref XnaVector3 color)
        {
            if (text.IsEmpty)
            {
                return;
            }

            // MobileUO: Removed EnsureSize
            //EnsureSize();

            Texture2D textureValue = spriteFont.Texture;
            List<Rectangle> glyphData = spriteFont.GlyphData;
            List<Rectangle> croppingData = spriteFont.CroppingData;
            // MobileUO: Switched from Vector3 to XnaVector3
            List<XnaVector3> kerning = spriteFont.Kerning;
            List<char> characterMap = spriteFont.CharacterMap;

            // MobileUO: Switched from Vector2 to XnaVector2
            XnaVector2 curOffset = XnaVector2.Zero;
            bool firstInLine = true;

            // MobileUO: Switched from Vector2 to XnaVector2
            XnaVector2 baseOffset = XnaVector2.Zero;
            float axisDirX = 1;
            float axisDirY = 1;

            foreach (char c in text)
            {
                // Special characters
                if (c == '\r')
                {
                    continue;
                }

                if (c == '\n')
                {
                    curOffset.X = 0.0f;
                    curOffset.Y += spriteFont.LineSpacing;
                    firstInLine = true;

                    continue;
                }

                /* Get the List index from the character map, defaulting to the
				 * DefaultCharacter if it's set.
				 */
                int index = characterMap.IndexOf(c);

                if (index == -1)
                {
                    if (!spriteFont.DefaultCharacter.HasValue)
                    {
                        index = characterMap.IndexOf('?');
                        //throw new ArgumentException(
                        //                            "Text contains characters that cannot be" +
                        //                            " resolved by this SpriteFont.",
                        //                            "text"
                        //                           );
                    }
                    else
                    {
                        index = characterMap.IndexOf(spriteFont.DefaultCharacter.Value);
                    }
                }

                /* For the first character in a line, always push the width
				 * rightward, even if the kerning pushes the character to the
				 * left.
				 */
                // MobileUO: Switched from Vector3 to XnaVector3
                XnaVector3 cKern = kerning[index];

                if (firstInLine)
                {
                    curOffset.X += Math.Abs(cKern.X);
                    firstInLine = false;
                }
                else
                {
                    curOffset.X += spriteFont.Spacing + cKern.X;
                }

                // Calculate the character origin
                Rectangle cCrop = croppingData[index];
                Rectangle cGlyph = glyphData[index];

                float offsetX = baseOffset.X + (
                                    curOffset.X + cCrop.X
                                ) * axisDirX;

                float offsetY = baseOffset.Y + (
                                    curOffset.Y + cCrop.Y
                                ) * axisDirY;


                Draw2D(textureValue,
                    Mathf.RoundToInt((x + (int) offsetX)), Mathf.RoundToInt((y + (int) offsetY)),
                    cGlyph.X, cGlyph.Y, cGlyph.Width, cGlyph.Height,
                    ref color);

                curOffset.X += cKern.Y + cKern.Z;
            }
        }

        private void RenderVertex(PositionTextureColor4 vertex, Texture2D texture, Vector3 hue)
        {
            vertex.Position0 *= scale;
            vertex.Position1 *= scale;
            vertex.Position2 *= scale;
            vertex.Position3 *= scale;

            reusedMesh.Populate(vertex);

            var mat = hueMaterial;
            mat.mainTexture = texture.UnityTexture;
            mat.SetColor(Hue, new Color(hue.x,hue.y,hue.z));
            mat.SetPass(0);

            Graphics.DrawMeshNow(reusedMesh.Mesh, Vector3.zero, Quaternion.identity);
        }

        [MethodImpl(256)]
        public void Begin()
        {
            hueMaterial.SetTexture(HueTex1, GraphicsDevice.Textures[1].UnityTexture);
            hueMaterial.SetTexture(HueTex2, GraphicsDevice.Textures[2].UnityTexture);
        }

        public void Begin(Effect effect)
        {
            CustomEffect = effect;
        }

        [MethodImpl(256)]
        public void End()
        {
            CustomEffect = null;
        }

        //Because XNA's Blend enum starts with 1, we duplicate BlendMode.Zero for 0th index
        //and also for indexes 12-15 where Unity's BlendMode enum doesn't have a match to XNA's Blend enum
        //and we don't need those anyways
        private static readonly BlendMode[] BlendModesMatchingXna =
        {
            BlendMode.Zero,
            BlendMode.Zero,
            BlendMode.One,
            BlendMode.SrcColor,
            BlendMode.OneMinusSrcColor,
            BlendMode.SrcAlpha,
            BlendMode.OneMinusSrcAlpha,
            BlendMode.DstAlpha,
            BlendMode.OneMinusDstAlpha,
            BlendMode.DstColor,
            BlendMode.OneMinusDstColor,
            BlendMode.SrcAlphaSaturate,
            BlendMode.Zero,
            BlendMode.Zero,
            BlendMode.Zero,
            BlendMode.Zero
        };

        private static void SetMaterialBlendState(Material mat, BlendState blendState)
        {
            var src = BlendModesMatchingXna[(int) blendState.ColorSourceBlend];
            var dst = BlendModesMatchingXna[(int) blendState.ColorDestinationBlend];
            SetMaterialBlendState(mat, src, dst);
        }

        private static void SetMaterialBlendState(Material mat, BlendMode src, BlendMode dst)
        {
            mat.SetFloat(SrcBlend, (float) src);
            mat.SetFloat(DstBlend, (float) dst);
        }

        private void ApplyStates()
        {
            // GraphicsDevice.BlendState = _blendState;
            SetMaterialBlendState(hueMaterial, _blendState);

            GraphicsDevice.DepthStencilState = _stencil;

            // GraphicsDevice.RasterizerState = _useScissor ? _rasterizerState : RasterizerState.CullNone;
            hueMaterial.SetFloat(Scissor, _useScissor ? 1 : 0);
            if (_useScissor)
            {
                var scissorRect = GraphicsDevice.ScissorRectangle;
                var scissorVector4 = new Vector4(scissorRect.X * scale,
                    scissorRect.Y * scale,
                    scissorRect.X * scale + scissorRect.Width * scale,
                    scissorRect.Y * scale + scissorRect.Height * scale);
                hueMaterial.SetVector(ScissorRect, scissorVector4);
            }

            DefaultEffect.ApplyStates();
        }

        [MethodImpl(256)]
        public void EnableScissorTest(bool enable)
        {
            if (enable == _useScissor)
                return;

            if (!enable && _useScissor && ScissorStack.HasScissors)
                return;

            _useScissor = enable;
            ApplyStates();
        }

        [MethodImpl(256)]
        public void SetBlendState(BlendState blend)
        {
            _blendState = blend ?? BlendState.AlphaBlend;
            ApplyStates();
        }

        [MethodImpl(256)]
        public void SetStencil(DepthStencilState stencil)
        {
            _stencil = stencil ?? Stencil;
            ApplyStates();
        }

        public void Dispose()
        {
            DefaultEffect?.Dispose();
        }

        private class IsometricEffect : MatrixEffect
        {
            private Vector2 _viewPort;
            private Matrix _matrix = Matrix.Identity;

            public IsometricEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.IsometricEffect)
            {
                WorldMatrix = Parameters["WorldMatrix"];
                Viewport = Parameters["Viewport"];
                //NOTE: Since we don't parse the mojoshader to read the properties, Brightlight doesn't exist as a key in the Parameters dictionary
                Parameters.Add("Brightlight", new EffectParameter());
                Brighlight = Parameters["Brightlight"];

                CurrentTechnique = Techniques["HueTechnique"];
            }

            protected IsometricEffect(Effect cloneSource) : base(cloneSource)
            {
            }


            public EffectParameter WorldMatrix { get; }
            public EffectParameter Viewport { get; }
            public EffectParameter Brighlight { get; }


            public override void ApplyStates()
            {
                WorldMatrix.SetValue(_matrix);

                _viewPort.x = GraphicsDevice.Viewport.Width;
                _viewPort.y = GraphicsDevice.Viewport.Height;
                Viewport.SetValue(_viewPort);

                base.ApplyStates();
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PositionTextureColor4
        {
            public Vector3 Position0;
            public Vector3 TextureCoordinate0;
            public Vector3 Hue0;
            public Vector3 Normal0;

            public Vector3 Position1;
            public Vector3 TextureCoordinate1;
            public Vector3 Hue1;
            public Vector3 Normal1;

            public Vector3 Position2;
            public Vector3 TextureCoordinate2;
            public Vector3 Hue2;
            public Vector3 Normal2;

            public Vector3 Position3;
            public Vector3 TextureCoordinate3;
            public Vector3 Hue3;
            public Vector3 Normal3;
        }
    }
}

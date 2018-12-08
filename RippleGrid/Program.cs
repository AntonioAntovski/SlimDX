using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Core;
using Core.Vertex;
using Buffer = SlimDX.Direct3D11.Buffer;

namespace RippleGrid
{
    public class RippleGrid : D3DApp
    {
        Buffer vertexBuffer;
        Buffer indexBuffer;

        InputLayout inputLayout;

        int gridIndexCount;

        Effect effect;
        EffectTechnique effectTechnique;
        EffectMatrixVariable fxWVP;

        Matrix world;
        Matrix view;
        Matrix projection;

        float theta, phi, radius;

        Point lastMousePosition;

        bool disposed;

        public RippleGrid(IntPtr hInstance) : base(hInstance)
        {
            vertexBuffer = null;
            indexBuffer = null;

            inputLayout = null;

            gridIndexCount = 0;

            effect = null;
            effectTechnique = null;
            fxWVP = null;

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;

            theta = 1.5f * MathF.PI;
            phi = 0.1f * MathF.PI;
            radius = 200.0f;

            lastMousePosition = new Point(0, 0);
        }

        public override void DrawScene()
        {
            base.DrawScene();

            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.SteelBlue);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = inputLayout;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, VertexPC.Stride, 0));
            ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

            var wvp = world * view * projection;
            fxWVP.SetMatrix(wvp);

            for(int i = 0; i < effectTechnique.Description.PassCount; i++)
            {
                effectTechnique.GetPassByIndex(i).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(gridIndexCount, 0, 0);
            }
            SwapChain.Present(0, PresentFlags.None);
        }

        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }
            BuildGeometryBuffers();
            BuildFX();
            BuildVertexLayout();

            return true;
        }

        public override void OnResize()
        {
            base.OnResize();
            projection = Matrix.PerspectiveFovLH(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
        }

        public override void UpdateScene(float dt)
        {
            base.UpdateScene(dt);

            var x = radius * MathF.Sin(phi) * MathF.Cos(theta);
            var z = radius * MathF.Sin(phi) * MathF.Sin(theta);
            var y = radius * MathF.Cos(phi);

            var pos = new Vector3(x, y, z);
            var target = new Vector3(0);
            var up = new Vector3(0, 1, 0);
            view = Matrix.LookAtLH(pos, target, up);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    Util.ReleaseCom(ref vertexBuffer);
                    Util.ReleaseCom(ref indexBuffer);
                    Util.ReleaseCom(ref effect);
                    Util.ReleaseCom(ref inputLayout);
                }
                disposed = true;
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseDown(object sender, MouseEventArgs e)
        {
            lastMousePosition = e.Location;
            Window.Capture = true;
        }

        protected override void OnMouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                var dx = MathF.ToRadians(0.25f * (e.X - lastMousePosition.X));
                var dy = MathF.ToRadians(0.25f * (e.Y - lastMousePosition.Y));

                theta += dx;
                phi += dy;

                phi = MathF.Clamp(phi, 0.1f, MathF.PI - 0.1f);
            }
            else if(e.Button == MouseButtons.Right)
            {
                var dx = 0.2f * (e.X - lastMousePosition.X);
                var dy = 0.2f * (e.Y - lastMousePosition.Y);
                radius += dx - dy;

                radius = MathF.Clamp(radius, 50, 500);
            }
            lastMousePosition = e.Location;
        }

        protected override void OnMouseUp(object sender, MouseEventArgs e)
        {
            Window.Capture = false;
        }

        private void BuildGeometryBuffers()
        {
            var grid = GeometryGenerator.CreateGrid(160, 160, 50, 50);
            var vertices = new List<VertexPC>();
            foreach(var vertex in grid.Vertices)
            {
                var pos = vertex.Position;
                pos.Y = getHeight(pos.X, pos.Z);
                Color color;

                if(pos.Y < -0.6)
                {
                    color = Color.Red;
                }
                else if(pos.Y < -0.2)
                {
                    color = Color.OrangeRed;
                }
                else if(pos.Y < 0.2)
                {
                    color = Color.Orange;
                }
                else if(pos.Y < 0.4)
                {
                    color = Color.Yellow;
                }
                else
                {
                    color = Color.LightGoldenrodYellow;
                }
                vertices.Add(new VertexPC(pos, color));
            }

            var vbd = new BufferDescription(
                VertexPC.Stride * vertices.Count,
                ResourceUsage.Default,
                BindFlags.VertexBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            vertexBuffer = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

            var ibd = new BufferDescription(
                sizeof(int) * grid.Indices.Count,
                ResourceUsage.Default,
                BindFlags.IndexBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            indexBuffer = new Buffer(Device, new DataStream(grid.Indices.ToArray(), false, false), ibd);
            gridIndexCount = grid.Indices.Count;
        }

        private void BuildFX()
        {
            ShaderBytecode compiledBytecode = ShaderBytecode.CompileFromFile("D:\\3Shape\\SlimDX Antonio\\SlimDX Tutorial\\RippleGrid\\rippleGrid.fx", "fx_5_0", ShaderFlags.None, EffectFlags.None);
            effect = new Effect(Device, compiledBytecode);

            effectTechnique = effect.GetTechniqueByName("ColorTech");
            fxWVP = effect.GetVariableByName("gWorldViewProj").AsMatrix();
            Util.ReleaseCom(ref compiledBytecode);
        }

        private void BuildVertexLayout()
        {
            var vertexDesc = new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0, InputClassification.PerVertexData, 0)
            };
            var passDesc = effectTechnique.GetPassByIndex(0).Description;
            inputLayout = new InputLayout(Device, passDesc.Signature, vertexDesc);
        }

        private float getHeight(float x, float y)
        {
            return MathF.Sin(10 * (x * x + y * y)) * 4;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Configuration.EnableObjectTracking = true;
            var app = new RippleGrid(Process.GetCurrentProcess().Handle);
            if (!app.Init())
            {
                return;
            }
            app.Run();
        }
    }
}

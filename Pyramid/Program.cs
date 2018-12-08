using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Core;
using Core.Vertex;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using System.Diagnostics;
using Buffer = SlimDX.Direct3D11.Buffer;
using System.Windows.Forms;

namespace Pyramid
{
    public class PyramidApp : D3DApp
    {
        Buffer vertexBuffer;
        Buffer indexBuffer;

        Effect effect;
        EffectTechnique effectTechnique;
        EffectMatrixVariable fxWVP;

        Matrix world;
        Matrix view;
        Matrix projection;

        float theta, phi, radius;

        InputLayout inputLayout;

        Point lastMousePosition;

        bool disposed;

        public PyramidApp(IntPtr hInstance) : base(hInstance)
        {
            vertexBuffer = null;
            indexBuffer = null;

            effect = null;
            effectTechnique = null;
            fxWVP = null;

            world = Matrix.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;

            theta = 1.5f * MathF.PI;
            phi = 0.25f * MathF.PI;
            radius = 5.0f;

            lastMousePosition = new Point(0, 0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    Util.ReleaseCom(ref vertexBuffer);
                    Util.ReleaseCom(ref indexBuffer);
                    Util.ReleaseCom(ref inputLayout);
                    Util.ReleaseCom(ref effect);
                }
                disposed = true;
            }
            base.Dispose(disposing);
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
                ImmediateContext.DrawIndexed(18, 0, 0);
            }
            SwapChain.Present(0, PresentFlags.None);
        }

        protected override void OnMouseDown(object sender, MouseEventArgs e)
        {
            lastMousePosition = e.Location;
            Window.Capture = true;
        }

        protected override void OnMouseUp(object sender, MouseEventArgs e)
        {
            Window.Capture = false;
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
                var dx = 0.005f * (e.X - lastMousePosition.X);
                var dy = 0.005f * (e.Y - lastMousePosition.Y);
                radius += dx - dy;

                radius = MathF.Clamp(radius, 3, 15);
            }
            lastMousePosition = e.Location;
        }

        private void BuildGeometryBuffers()
        {
            var vertices = new[]
            {
                new VertexPC(new Vector3(-2, -2, 2), Color.Green),
                new VertexPC(new Vector3(2, -2, 2), Color.Green),
                new VertexPC(new Vector3(-2, -2, -2), Color.Green),
                new VertexPC(new Vector3(2, -2, -2), Color.Green),
                new VertexPC(new Vector3(0, 2, 0), Color.Red),
            };

            var vbd = new BufferDescription(
                VertexPC.Stride * vertices.Length,
                ResourceUsage.Default,
                BindFlags.VertexBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            vertexBuffer = new Buffer(Device, new DataStream(vertices, true, true), vbd);

            var indices = new uint[]
            {
                //front
                0, 1, 4,

                //back
                2, 4, 3,

                //left
                0, 4, 2,

                //right
                1, 3, 4,

                //bottom
                0, 2, 1,
                1, 2, 3

                /*0, 1,
                0, 2,
                0, 4,
                1, 2,
                1, 3,
                1, 4,
                2, 3,
                2, 4,
                3, 4*/

            };

            var ibd = new BufferDescription(
                sizeof(uint) * indices.Length,
                ResourceUsage.Default,
                BindFlags.IndexBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            indexBuffer = new Buffer(Device, new DataStream(indices, true, true), ibd);
        }

        private void BuildFX()
        {
            ShaderBytecode compiledBytecode = ShaderBytecode.CompileFromFile(@"D:\3Shape\SlimDX Antonio\SlimDX Tutorial\Pyramid\pyramid.fx", "fx_5_0", ShaderFlags.None, EffectFlags.None);
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
    }

    class Program
    {
        static void Main(string[] args)
        {
            Configuration.EnableObjectTracking = true;
            var app = new PyramidApp(Process.GetCurrentProcess().Handle);
            if (!app.Init())
            {
                return;
            }
            app.Run();
        }
    }
}

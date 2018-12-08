using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using Core;
using Core.Vertex;
using Core.Camera;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Buffer = SlimDX.Direct3D11.Buffer;
using Debug = System.Diagnostics.Debug;

namespace RollingSphere
{
    public class RollingSphereApp : D3DApp
    {
        Buffer vertexBuffer;
        Buffer indexBuffer;

        InputLayout inputLayout;

        Effect effect;
        EffectTechnique effectTechnique;
        EffectMatrixVariable fxWVP;

        Matrix gridWorld;
        Matrix sphereWorld;
        Matrix cylWorld;
        Matrix triangleWorld;
        Matrix view;
        Matrix projection;
        Matrix grid1World;

        int sphereVertexOffset;
        int gridVertexOffset;
        int cylVertexOffset;
        int triangleVertexOffset;
        int grid1VertexOffset;
        int sphereIndexOffset;
        int gridIndexOffset;
        int cylIndexOffset;
        int triangleIndexOffset;
        int grid1IndexOffset;
        int sphereIndexCount;
        int gridIndexCount;
        int cylIndexCount;
        int triangleIndexCount;
        int grid1IndexCount;

        float theta, phi, radius;

        Point lastMousePosition;

        FpsCamera camera;

        int gravity;                                                    //-1 - down; 1 - up
        float step;
        int yOffset;
        float zOffset;
        float landing;

        Material sphereMat;
        ShaderResourceView diffuseMapSphere;
        ShaderResourceView diffuseMapFloor;

        bool disposed;

        public RollingSphereApp(IntPtr hInstance) : base(hInstance)
        {
            vertexBuffer = null;
            indexBuffer = null;

            inputLayout = null;

            effect = null;
            effectTechnique = null;
            fxWVP = null;

            gridWorld = view = projection = Matrix.Identity;
            //sphereWorld = Matrix.Scaling(2, 2, 2) * Matrix.Translation(0, 1.9f, -58);       //Translation
            sphereWorld = Matrix.Scaling(2, 2, 2) * Matrix.Translation(0, 20, -58);          //Bounce
            cylWorld = Matrix.Scaling(1, 2, 1) * Matrix.Translation(5, 3.9f, 58);
            triangleWorld = Matrix.Scaling(1, 2, 1) * Matrix.Translation(5, 5, 57.9f);
            grid1World = Matrix.Translation(0, 23, 0);

            sphereVertexOffset = gridVertexOffset = cylVertexOffset = triangleVertexOffset = grid1VertexOffset =
                sphereIndexOffset = gridIndexOffset = cylIndexOffset = triangleIndexOffset = grid1IndexOffset =
                sphereIndexCount = gridIndexCount = cylIndexCount = triangleIndexCount = grid1IndexCount = 0;

            theta = MathF.PI;
            phi = 0.46f * MathF.PI;
            radius = 140;

            lastMousePosition = new Point(0, 0);

            camera = new FpsCamera { Position = new Vector3(0, 2, -70) };

            gravity = -1;
            step = 0;
            yOffset = 1;
            zOffset = 0;
            landing = 0;

            sphereMat = new Material
            {
                Ambient = new Color4(0.6f, 0.8f, 0.9f),
                Diffuse = new Color4(0.6f, 0.8f, 0.9f),
                Specular = new Color4(16, 0.9f, 0.9f, 0.9f),
                Reflect = Color.Black
            };
        }

        public override void DrawScene()
        {
            base.DrawScene();

            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.LightSkyBlue);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);

            ImmediateContext.InputAssembler.InputLayout = inputLayout;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, VertexPC.Stride, 0));
            ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

            var vp = view * projection;
            for(int i = 0; i < effectTechnique.Description.PassCount; i++)
            {
                fxWVP.SetMatrix(gridWorld * vp);
                effectTechnique.GetPassByIndex(i).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(gridIndexCount, gridIndexOffset, gridVertexOffset);

                fxWVP.SetMatrix(sphereWorld * vp);
                /*Core.FX.Effects.BasicFX.SetWorld(sphereWorld);
                Core.FX.Effects.BasicFX.SetWorldInvTranspose(MathF.InverseTranspose(sphereWorld));
                Core.FX.Effects.BasicFX.SetWorldViewProj(sphereWorld * view * projection);
                Core.FX.Effects.BasicFX.SetTexTransform(Matrix.Identity);
                Core.FX.Effects.BasicFX.SetMaterial(sphereMat);
                Core.FX.Effects.BasicFX.SetDiffuseMap(diffuseMapSphere);*/
                effectTechnique.GetPassByIndex(i).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(sphereIndexCount, sphereIndexOffset, sphereVertexOffset);

                fxWVP.SetMatrix(cylWorld * vp);
                effectTechnique.GetPassByIndex(i).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(cylIndexCount, cylIndexOffset, cylVertexOffset);

                fxWVP.SetMatrix(triangleWorld * vp);
                effectTechnique.GetPassByIndex(i).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(triangleIndexCount, triangleIndexOffset, triangleVertexOffset);

                fxWVP.SetMatrix(grid1World * vp);
                effectTechnique.GetPassByIndex(i).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(grid1IndexCount, grid1IndexOffset, grid1VertexOffset);
            }
            SwapChain.Present(0, PresentFlags.None);
        }

        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }

            diffuseMapSphere = ShaderResourceView.FromFile(Device, "D:\\3Shape\\SlimDX Antonio\\SlimDX Tutorial\\RollingSphere\\stone.dds");
            diffuseMapFloor = ShaderResourceView.FromFile(Device, "D:\\3Shape\\SlimDX Antonio\\SlimDX Tutorial\\RollingSphere\\grass.dds");

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

            //Translation on z-axis
            /*if (sphereWorld.M43 < 55)
            {
                var sphereScale = Matrix.Scaling(2, 2, 2);
                var sphereOffset = Matrix.Translation(0, 1.9f, 5.0f * Timer.TotalTime - 58);
                sphereWorld = sphereScale * sphereOffset;

                camera.UpdateViewMatrix();
            }*/

            //Bounce and translation on xz-plane
            if (sphereWorld.M43 < 55)
            {
                if (sphereWorld.M42 > 1.8f && sphereWorld.M42 < 21)
                {
                    var sphereScale = Matrix.Scaling(2, 2, 2);
                    var sphereOffset = Matrix.Translation(0, gravity * step + yOffset * 20 + (1 - yOffset) * 3, -58 + zOffset);
                    sphereWorld = sphereScale * sphereOffset;
                    step += 0.02f;
                    zOffset += 0.0098f;

                    camera.UpdateViewMatrix();
                }
                else
                {
                    gravity = -gravity;
                    step = 0;

                    if(gravity == -1)
                    {
                        yOffset = 1;
                    }
                    else
                    {
                        yOffset = 0;
                    }

                    if (sphereWorld.M42 < 1.9f)
                    {
                        sphereWorld.M42 = 1.9f;
                    }
                    else
                    {
                        sphereWorld.M42 = 20;
                    }
                }
                landing = sphereWorld.M42;
            }
            else
            {
                if(landing > 1.9f)
                {
                    var sphereScale = Matrix.Scaling(2, 2, 2);
                    var sphereOffset = Matrix.Translation(0, landing, sphereWorld.M43);
                    sphereWorld = sphereScale * sphereOffset;
                    landing -= 0.02f;

                    camera.UpdateViewMatrix();
                }
            }
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
                    Util.ReleaseCom(ref diffuseMapSphere);
                    Util.ReleaseCom(ref diffuseMapFloor);
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
                var dx = 0.1f * (e.X - lastMousePosition.X);
                var dy = 0.1f * (e.Y - lastMousePosition.Y);
                radius += dx - dy;

                radius = MathF.Clamp(radius, 3.0f, 200.0f);
            }
            lastMousePosition = e.Location;
        }

        protected override void OnMouseUp(object sender, MouseEventArgs e)
        {
            Window.Capture = false;
        }

        private void BuildGeometryBuffers()
        {
            var grid = GeometryGenerator.CreateGrid(60, 120, 50, 50);
            var sphere = GeometryGenerator.CreateSphere(1, 20, 20);
            var cylinder = GeometryGenerator.CreateCylinder(0.1f, 0.1f, 4, 10, 10);
            var grid1 = GeometryGenerator.CreateGrid(60, 120, 50, 50);

            gridVertexOffset = 0;
            sphereVertexOffset = grid.Vertices.Count;
            cylVertexOffset = sphereVertexOffset + sphere.Vertices.Count;
            triangleVertexOffset = cylVertexOffset + cylinder.Vertices.Count;
            grid1VertexOffset = triangleVertexOffset + 3;

            gridIndexCount = grid.Indices.Count;
            sphereIndexCount = sphere.Indices.Count;
            cylIndexCount = cylinder.Indices.Count;
            triangleIndexCount = 3;
            grid1IndexCount = grid1.Indices.Count;

            gridIndexOffset = 0;
            sphereIndexOffset = gridIndexCount;
            cylIndexOffset = sphereIndexOffset + sphereIndexCount;
            triangleIndexOffset = cylIndexOffset + cylIndexCount;
            grid1IndexOffset = triangleIndexOffset + triangleIndexCount;

            var totalVertexCount = grid.Vertices.Count + sphere.Vertices.Count + cylinder.Vertices.Count + 3 + grid1.Vertices.Count;
            var totalIndexCount = gridIndexCount + sphereIndexCount + cylIndexCount + 3 + grid1IndexCount;

            var vs = new List<VertexPC>();
            foreach(var vertex in grid.Vertices)
            {
                vs.Add(new VertexPC(vertex.Position, Color.Green));
            }
            foreach(var vertex in sphere.Vertices)
            {
                vs.Add(new VertexPC(vertex.Position, Color.Red));
            }
            foreach(var vertex in cylinder.Vertices)
            {
                vs.Add(new VertexPC(vertex.Position, Color.Black));
            }

            vs.Add(new VertexPC(new Vector3(-1, 1, 0), Color.Gold));
            vs.Add(new VertexPC(new Vector3(1, 1, 0), Color.Gold));
            vs.Add(new VertexPC(new Vector3(0, 2, 0), Color.Gold));

            foreach(var vertex in grid1.Vertices)
            {
                vs.Add(new VertexPC(vertex.Position, Color.ForestGreen));
            }

            var vbd = new BufferDescription(
                VertexPC.Stride * totalVertexCount,
                ResourceUsage.Default,
                BindFlags.VertexBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            vertexBuffer = new Buffer(Device, new DataStream(vs.ToArray(), false, false), vbd);

            var indices = new List<int>();
            indices.AddRange(grid.Indices);
            indices.AddRange(sphere.Indices);
            indices.AddRange(cylinder.Indices);
            indices.Add(0);
            indices.Add(2);
            indices.Add(1);
            grid1.Indices.Reverse();
            indices.AddRange(grid1.Indices);

            var ibd = new BufferDescription(
                sizeof(int) * totalIndexCount,
                ResourceUsage.Default,
                BindFlags.IndexBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);

            indexBuffer = new Buffer(Device, new DataStream(indices.ToArray(), false, false), ibd);
        }

        private void BuildFX()
        {
            ShaderBytecode compiledBytecode = ShaderBytecode.CompileFromFile("D:\\3Shape\\SlimDX Antonio\\SlimDX Tutorial\\RollingSphere\\rollingSphere.fx", "fx_5_0", ShaderFlags.None, EffectFlags.None);
            effect = new Effect(Device, compiledBytecode);

            effectTechnique = effect.GetTechniqueByName("ColorTech");
            fxWVP = effect.GetVariableByName("gWorldViewProj").AsMatrix();
        }

        private void BuildVertexLayout()
        {
            var vertexDesc = new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0, InputClassification.PerVertexData, 0)
            };

            inputLayout = new InputLayout(Device, effectTechnique.GetPassByIndex(0).Description.Signature, vertexDesc);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Configuration.EnableObjectTracking = true;
            var app = new RollingSphereApp(Process.GetCurrentProcess().Handle);
            if (!app.Init())
            {
                return;
            }
            app.Run();
        }
    }
}

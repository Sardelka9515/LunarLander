#define TEST
using SharpD2D;
using SharpD2D.Drawing;
using SharpD2D.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using BoxSharp;
using Box = BoxSharp.Box<LunarLander.DrawContext>;
using Shape = BoxSharp.Shape<LunarLander.DrawContext>;
using Polygon = BoxSharp.Polygon<LunarLander.DrawContext>;
using World = BoxSharp.World<LunarLander.DrawContext>;
using Manifold = BoxSharp.Manifold<LunarLander.DrawContext>;
using System.Runtime.InteropServices;

namespace LunarLander
{
    internal static class Program
    {
        static Vector2 Gravity = new(0, 1.62f);
        static bool IsGoingUp;
        static bool IsTurningRight;
        static bool IsTurningLeft;
        static bool Restarted;
        static float FuelLevel = 1;
#if TEST
        const float Torque = 0.4f;
        const float ThrustPower = 100;
#else
        const float Torque = 0.2f;
        const float ThrustPower = 70;
#endif
        static World Moon = new(new(192, 108));
        static Box Lander = new(new(4.3f, 5.5f))
        {
            CollisionIndex = 1,
            Tag = new(),
            Gravity = Gravity
        };
        static Box Pad = new(new(8, 2)) { CollisionIndex = 0, Tag = new() };

#if TEST
        static Box Ground = new(new(60, 30))
        {
            Position = new(50, 50),
#else
        static Box Ground = new(new(Moon.Size.X, 30))
        {
            Position = new(Moon.Size.X / 2, Moon.Size.Y),
#endif

            CollisionIndex = 0
        };
        static MainWindow Window;
        static Polygon[] Obstacles;
        static SceneDisplay Display;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            WindowHelper.DisableScalingGlobal();
            TimerService.EnableHighPrecisionTimers();
            ApplicationConfiguration.Initialize();

            Window = new MainWindow();
            Display = new(Moon)
            {
                Size = Window.Size,
                Visible = true,
                Location = default
            };
            Window.Controls.Add(Display);
            Window.SizeChanged += (s, e) => Display.Size = Window.Size;
            Display.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Space)
                {
                    Display.Stop();
                    GameSetup();
                    Display.Start();
                }
                else if (e.KeyCode == Keys.F11)
                {
                    if (Window.FormBorderStyle == FormBorderStyle.None)
                    {
                        Window.FormBorderStyle = FormBorderStyle.Sizable;
                        Window.WindowState = FormWindowState.Normal;
                    }
                    else
                    {
                        Window.FormBorderStyle = FormBorderStyle.None;
                        Window.WindowState = FormWindowState.Maximized;
                    }
                }
            };
            GameSetup();
            Application.Run(Window);
        }
        static void GameSetup()
        {
            FuelLevel = 1;
            Display.Message = null;
            Moon.Clear();
            Lander.Position = new(Random.Shared.Next(10, (int)Moon.Size.X - 10), 0);
            Lander.Velocity = default;
            Lander.AngularVelocity = 0;
            Lander.Angle = 0;

            Obstacles = new Polygon[(int)Moon.Size.X / 5 + 1];
            for (int i = 0; i < Obstacles.Length - 1; i++)
            {
                Obstacles[i++] = new Box(new(Random.Shared.Next(10, 20), Random.Shared.Next(5, 30)))
                {
                    Position = new(Random.Shared.Next(i * 10, (i + 1) * 10), Random.Shared.Next(0, 10) + Moon.Size.Y - 15),
                    Angle = Random.Shared.NextSingle() * MathF.PI,
                    CollisionIndex = 0
                };
                if (i < Obstacles.Length - 1)
                {
                    var pos = new Vector2(Random.Shared.Next(i * 10, (i + 1) * 10), Random.Shared.Next(0, 10) + Moon.Size.Y - 15);
                    var vertices = new Vector2[]{
                    new(Random.Shared.Next(0,20 ), Random.Shared.Next(0,30)),
                    new(Random.Shared.Next(0,30), -Random.Shared.Next(0,30)),
                    new(-Random.Shared.Next(0,30), 0),
                    };
                    Obstacles[i] = new Polygon(vertices)
                    {
                        Position = pos,
                        Angle = Random.Shared.NextSingle() * MathF.PI,
                        CollisionIndex = 0
                    };
                }
            }
            Obstacles[Obstacles.Length - 1] = Ground;
            foreach (var m in Obstacles)
            {
                m.Tag = new();
                m.SetStatic();
                Moon.Add(m);
            }

            Moon.Update(0);
            BoxSharp.Line ray;
            while (!Moon.TryRayCast(
                ray = new BoxSharp.Line(
                    new(Random.Shared.Next(20, (int)Moon.Size.X - 20), 0),
                    Vector2.UnitY * 99999),
                out Pad.Position, out _)) ;
            Pad.SetStatic();
            Pad.Tag.Brush = null;
            Moon.Add(Pad);
            Moon.Add(Lander);
            Restarted = true;
        }
        static void Control()
        {
#if !TEST
            if (FuelLevel <= 0)
            {
                IsGoingUp = IsTurningLeft = IsTurningRight = false;
                return;
            }
#endif
            if (IsGoingUp = IsKeyDown(Key.Up))
            {
                FuelLevel -= 0.0005f;
                Lander.ApplyForce(Lander.UpVector * ThrustPower);
            }
            if (IsTurningLeft = IsKeyDown(Key.Left))
            {
                FuelLevel -= 0.0001f;
                Lander.ApplyImpulse(Torque * Lander.LeftVector, 1.5f * Lander.UpVector);
            }
            if (IsTurningRight = IsKeyDown(Key.Right))
            {
                FuelLevel -= 0.0001f;
                Lander.ApplyImpulse(Torque * Lander.RightVector, 1.5f * Lander.UpVector);
            }
        }

        static bool IsKeyDown(Key k)
        {
            var state = GetKeyState(KeyInterop.VirtualKeyFromKey(k));
            return (state & 0x8000) != 0;
        }

        [DllImport("user32.dll")]
        static extern short GetKeyState(int key);

        internal class SceneDisplay : Control
        {
            private Canvas _canvas;
            World _scene;
            float _scale;
            public SceneDisplay(World scene)
            {
                _scene = scene;
                _scene.OnCollision += OnCollision;
            }
            Graphics _gfx;
            void OnCollision(Manifold m)
            {
#if !TEST
                if (m.A == Lander || m.B == Lander)
                {
                    if (m.intensity > 3.5)
                    {
                        Explode();
                    }
                }
#endif
#if DEBUG
                _gfx.TransformStart(_scaleMatrix);
                foreach (var c in m.contacts)
                {
                    _gfx.DrawCircle(_purpleBrush, c.ToPointF(), 0.2f, 0.5f);
                    _gfx.DrawLine(_whiteBrush, c.ToPointF(), (c + m.normal).ToPointF(), 0.3f);
                }
                _gfx.DrawLine(_purpleBrush, m.incidentFace.start.ToPointF(), m.incidentFace.end.ToPointF(), 0.5f);
                _gfx.TransformEnd();
#endif
            }
            protected override void OnHandleCreated(EventArgs e)
            {
                _canvas = new(Handle);
                _canvas.DrawGraphics += Update;
                _canvas.SetupGraphics += Setup;
                _canvas.Initialize();
                Start();
            }
            public void Start()
            {
                _canvas.FPS = 60;
            }
            public void Stop()
            {
                _canvas.FPS = 0;
                _canvas.Join();
            }
            IBrush _debugBrush;
            IBrush _landerBrush;
            IBrush _fireBrush;
            IBrush _boxBrush;
            IBrush _whiteBrush;
            IBrush _purpleBrush;
            IBrush _blackBrush;
            Font _font;
            Font _smallFont;
            private void Setup(object sender, SetupGraphicsEventArgs e)
            {
                _debugBrush = e.Graphics.CreateSolidBrush(new Color(128, 128, 128, 128));
                _fireBrush = e.Graphics.CreateSolidBrush(new Color(230, 46, 0));
                _boxBrush = e.Graphics.CreateSolidBrush(new Color(150, 150, 150));
                _landerBrush = e.Graphics.CreateSolidBrush(new Color(100, 100, 100));
                _whiteBrush = e.Graphics.CreateSolidBrush(new Color(255, 255, 255));
                _blackBrush = e.Graphics.CreateSolidBrush(new Color(0, 0, 0));
                _purpleBrush = e.Graphics.CreateSolidBrush(new Color(108, 66, 245));
                _font = e.Graphics.CreateFont("Arial", 20);
                _smallFont = e.Graphics.CreateFont("Arial", 5);

            }
            TransformationMatrix _scaleMatrix;
            public string Message = "";
            bool _stopped = false;
            private void Update(object sender, DrawGraphicsEventArgs e)
            {
                _gfx = e.Graphics;
                if (Restarted)
                {
                    Restarted = _stopped = false;
                    return;
                }
                if (!_stopped)
                    Control();
                var gfx = e.Graphics;
                var currentSize = Size;
                var sceneRatio = _scene.Size.X / _scene.Size.Y;
                var windowRatio = currentSize.Width / currentSize.Height;
                if (sceneRatio > windowRatio)
                {
                    _scale = currentSize.Height / _scene.Size.Y;
                }
                else
                {
                    _scale = currentSize.Width / _scene.Size.X;
                }
                _scaleMatrix = new TransformationMatrix(_scale, 0, 0, _scale, 0, 0);
                gfx.BeginScene();
                gfx.ClearScene();
                _scene.Update(1.0f / _canvas.FPS);
                if (!_stopped && !Lander.Position.X.IsBetween(0, Moon.Size.X) || !Lander.Position.Y.IsBetween(-20, Moon.Size.Y))
                    Explode();
                gfx.DrawText(_font, _whiteBrush, default, $"FPS: {1000 / e.DeltaTime}");
                gfx.DrawText(_font, _whiteBrush, new(0, 30), $"Velocity: {Lander.Velocity.Length()}");
                gfx.DrawText(_font, _whiteBrush, new(0, 90), $"Fuel: {FuelLevel}");


                _scene.EnumObjects(DrawBox, gfx);
                gfx.TransformStart(_scaleMatrix);
                if (Lander.Position != default && MathF.Abs(Lander.Angle) < 0.05f && Lander.Velocity.Length() < 0.05f && Lander.IsIntersectingWith(Pad))
                {
                    var score = FuelLevel * 700;
                    score += Math.Max(6 - Lander.Position.DistanceTo(Pad.Position), 0) * 300;
                    score = MathF.Truncate(score * 100) / 100;
                    Message = $"Score: {score}, press Space to restart";
                    _stopped = true;
                    _canvas.IsRunning = false;
                }
#if DEBUG
                Message = Lander.Position.ToString();


                var ray = new BoxSharp.Line(Lander.Position - Lander.UpVector * 10, Lander.UpVector * -1000);

                gfx.DrawLine(_fireBrush, ray.ToLine(), 1);
                var hits = new List<(Shape, Vector2)>();
                if (Moon.TryRayCast(ray, out var hit, out _, hits))
                {
                    gfx.DrawCircle(_fireBrush, hit.ToPointF(), 3, 1f);
                }

                Moon.EnumObjects((b) =>
                {
                    if (b is Polygon p)
                    {
                        p.EnumEdges((e) =>
                        {

                            gfx.DrawLine(_fireBrush, e.ToLine(), 0.3f);
                            gfx.DrawCircle(_fireBrush, b.Position.ToPointF(), 0, 2);
                        });
                        for (int i = 0; i < p.EdgeNormals.Length; i++)
                        {
                            var v1 = p.Vertices[i].world;
                            var v2 = p.Vertices[i + 1 >= p.Vertices.Length ? 0 : i + 1].world;
                            var normal = p.EdgeNormals[i].world * 15;
                            var l = new BoxSharp.Line((v1 + v2) / 2, normal);
                            gfx.DrawLine(_fireBrush, l.ToLine(), 0.2f);
                        }
                        for (int i = 0; i < p.Axes.Length; i++)
                        {
                            var dir = p.Axes[i].world;
                            gfx.DrawLine(_whiteBrush, p.Position.ToPointF(), (p.Position + dir * 10).ToPointF(), 0.2f);
                        }
                        // gfx.DrawCircle(_fireBrush, p.GetSupport(Vector2.UnitY).ToPointF(), 0, 2);
                    }
                    return true;
                });

                // gfx.DrawLine(_fireBrush, Lander.Project(Ground.XAxis).ToLine(), 0.5f);
                // gfx.DrawLine(_fireBrush, Lander.Project(Ground.YAxis).ToLine(), 0.5f);
                // gfx.DrawLine(_fireBrush, Ground.Project(Lander.XAxis).ToLine(), 0.5f);
                // gfx.DrawLine(_fireBrush, Ground.Project(Lander.YAxis).ToLine(), 0.5f);
                // gfx.DrawLine(_fireBrush, Ground.Project(Lander.YAxis).ToLine(), 0.5f);

#endif
                gfx.TransformEnd();
                if (!string.IsNullOrEmpty(Message))
                {
                    gfx.DrawText(_font, _whiteBrush, new(0, 60), Message);
                }
                gfx.EndScene();
            }

            private void Explode()
            {
                Message = $"Game Over, press Space to restart";
                _stopped = true;
                var dc = Random.Shared.Next(40, 60);
                for (int i = 0; i < dc; i++)
                {
                    var d = new Box(new(Random.Shared.Next(1, 5) * 0.5f, Random.Shared.Next(1, 5) * 0.5f))
                    {
                        Position =
                        Random.Shared.Next(-5, 5) * Lander.UpVector * 0.2f +
                        Random.Shared.Next(-5, 5) * Lander.RightVector * 0.2f +
                        Lander.Position,
                        Tag = new()
                    };
                    d.Tag.Spawned = Environment.TickCount64;
                    d.Velocity = (d.Position - Lander.Position) * 20 * (Random.Shared.NextSingle() + 0.4f) + Lander.Velocity * 0.5f;
                    d.Gravity = Gravity * 2;
                    d.Angle = Random.Shared.NextSingle() * MathF.PI * 2;
                    d.AngularVelocity = (Random.Shared.NextSingle() - 0.5f) * MathF.PI * 4;
                    if (Random.Shared.Next(0, 3) == 1)
                        d.Tag.Brush = _fireBrush;
                    else
                        d.Tag.Brush = _landerBrush;
                    Moon.Add(d);
                }
                Lander.Position = default;
                Lander.Velocity = default;
                Lander.SetRemove();
            }

            bool DrawBox(Shape b, Graphics gfx)
            {
                gfx.TransformStart(RotationTransform(-b.Angle, b.Position) * _scaleMatrix);
                if (b == Lander)
                {
                    DrawLander(b as Box, gfx);
                }
                else if (b is Polygon p)
                {
                    var brush = p.Tag.Brush ?? (p == Pad ? _whiteBrush : _boxBrush);
#if DEBUG
                    if (p.IsIntersectingWith(Lander))
                        brush = _fireBrush;
#endif
                    Geometry geo = p.Tag.Geometry;
                    geo ??= p.Tag.Geometry = new(gfx);
                    geo.BeginFigure(p.Vertices[0].local.ToPointF(), true);
                    for (int i = 0; i < p.Vertices.Length; i++)
                    {
                        geo.AddPoint(p.Vertices[i].local.ToPointF());
                    }
                    geo.EndFigure(true);
                    geo.Close();
                    gfx.FillGeometry(geo, brush);
                }
                // gfx.DrawText(_smallFont, _whiteBrush, default, $"{b.Mass} {b.Inertia}");
                if (b.CollisionIndex > 1
                    && b.Tag.Brush == _fireBrush
                    && Environment.TickCount64 > b.Tag.Spawned + 1000
                    && Random.Shared.Next(0, 100) == 13)
                    b.SetRemove();
                gfx.TransformEnd();
                return true;
            }

            private void DrawLander(Box b, Graphics gfx)
            {
                var hw = b.Size.X / 2;
                var hh = b.Size.Y / 2;
                var thrustPos = new Vector2(0, 1);
                var headPos = new Vector2(0, thrustPos.Y - hw);
                var head = new Circle(headPos.ToPointF(), hw);

                // draw head
                gfx.FillCircle(_landerBrush, head);

                // Draw legs
                gfx.DrawLine(_landerBrush, new PointF(hw - 1, headPos.Y), new PointF(hw, hh), 1);
                gfx.DrawLine(_landerBrush, new PointF(-hw + 1, headPos.Y), new PointF(-hw, hh), 1);

                if (IsGoingUp)
                {
                    var hwFire = Random.Shared.Next(1, 2);
                    var hFire = Random.Shared.Next(3, 5);
                    gfx.FillTriangle(_fireBrush, thrustPos.ToPointF(), new PointF(hwFire, hFire), new PointF(-hwFire, hFire));
                }
                if (IsTurningLeft)
                {
                    var fireWidth = Random.Shared.NextSingle() / 2;
                    gfx.FillTriangle(_fireBrush, new PointF(hw, headPos.Y), new PointF(hw + 2, headPos.Y + fireWidth), new PointF(hw + 2, headPos.Y - fireWidth));
                }

                if (IsTurningRight)
                {
                    var fireWidth = Random.Shared.NextSingle() / 2;
                    gfx.FillTriangle(_fireBrush, new PointF(-hw, headPos.Y), new PointF(-hw - 2, headPos.Y + fireWidth), new PointF(-hw - 2, headPos.Y - fireWidth));
                }
            }

            static TransformationMatrix RotationTransform(float angel, Vector2 center)
            {
                var c = (float)Math.Cos(angel);
                var s = (float)Math.Sin(angel);
                return new(c, -s, s, c, center.X, center.Y);
            }
        }
    }
    class DrawContext
    {
        public IBrush Brush;
        public Geometry Geometry;
        public long Spawned;
    }
}
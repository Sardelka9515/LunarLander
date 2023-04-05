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

namespace LunarLander
{
    internal static class Program
    {
        static Vector2 Gravity = new(0, 1.62f);
        static bool IsGoingUp;
        static bool IsTurningRight;
        static bool IsTurningLeft;
        static bool Restarted;
        static Scene Moon = new Scene(new(192, 108));
        static Box Lander = new(new(4.3f, 5.5f));
        static Box Ground = new(new(Moon.Size.X, 30))
        {
            Position = new(Moon.Size.X / 2, Moon.Size.Y)
        };
        static MainWindow Window;
        static Box Pad = new(new(8, 2));
        static Box[] Colliders;
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
                if (e.KeyData == Keys.Space)
                {
                    GameSetup();
                    Display.Stop();
                    Display.Start();
                }
                else if (e.KeyData == Keys.F11)
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
            var controllerThread = new Thread(Controller) { IsBackground = true };
            controllerThread.SetApartmentState(ApartmentState.STA);
            GameSetup();
            controllerThread.Start();
            Application.Run(Window);
        }
        static void GameSetup()
        {
            Display.Message = null;
            Moon.Clear();
            Lander.Position = new(Random.Shared.Next(0, (int)Moon.Size.X), 0);
            Lander.Velocity = default;
            Lander.AngularVelocity = 0;
            Lander.Angle = 0;

            Colliders = new Box[(int)Moon.Size.X / 10 + 2];
            for (int i = 0; i < Colliders.Length - 1; i++)
            {
                Colliders[i] = new(new(Random.Shared.Next(10, 20), Random.Shared.Next(5, 30)))
                {
                    Position = new(Random.Shared.Next(i * 10, (i + 1) * 10), Random.Shared.Next(0, 10) + Moon.Size.Y - 15),
                    Angle = Random.Shared.NextSingle() * MathF.PI
                };
            }
            Colliders[Colliders.Length - 2] = Ground;
            Colliders[Colliders.Length - 1] = Pad;
            foreach (var m in Colliders)
            {
                Moon.AddBox(m);
            }

            Line ray;
            while (!Moon.TryRayCast(
                ray = new Line(
                    new(Random.Shared.Next(0, (int)Moon.Size.X), 0),
                    Vector2.UnitY * 99999),
                out Pad.Position)) ;

            Moon.AddBox(Pad);
            Moon.AddBox(Lander);
            Restarted = true;
        }
        static void Controller()
        {
            while (true)
            {
                var a = Gravity;
                if (IsGoingUp = Keyboard.IsKeyDown(Key.Up))
                {
                    a += 3 * Lander.UpVector;
                }
                Lander.Acceleration = a;
                Lander.AngularAcceleration = 0;
                if (IsTurningLeft = Keyboard.IsKeyDown(Key.Left))
                {
                    Lander.AngularAcceleration += 0.2f;
                }
                if (IsTurningRight = Keyboard.IsKeyDown(Key.Right))
                {
                    Lander.AngularAcceleration -= 0.2f;
                }
                Thread.Sleep(16);
            }
        }


        internal class SceneDisplay : Control
        {
            private Canvas _canvas;
            Scene _scene;
            float _scale;
            Font _font;
            public SceneDisplay(Scene scene)
            {
                _scene = scene;
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
            }
            IBrush _debugBrush;
            IBrush _landerBrush;
            IBrush _fireBrush;
            IBrush _boxBrush;
            IBrush _whiteBrush;
            private void Setup(object sender, SetupGraphicsEventArgs e)
            {
                _debugBrush = e.Graphics.CreateSolidBrush(new Color(128, 128, 128, 128));
                _fireBrush = e.Graphics.CreateSolidBrush(new Color(230, 46, 0));
                _boxBrush = _landerBrush = e.Graphics.CreateSolidBrush(new Color(112, 112, 112));
                _whiteBrush = e.Graphics.CreateSolidBrush(new Color(255, 255, 255));
                _font = e.Graphics.CreateFont("Arial", 20);
            }
            TransformationMatrix _scaleMatrix;
            public string Message = "";
            private void Update(object sender, DrawGraphicsEventArgs e)
            {
                if (Restarted)
                {
                    Restarted = false;
                    return;
                }
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
                var desiredSize = new System.Drawing.Size((int)(_scene.Size.X * _scale), (int)(_scene.Size.Y * _scale));
                if (desiredSize != Size)
                    BeginInvoke(() => Size = desiredSize);
                _scaleMatrix = new TransformationMatrix(_scale, 0, 0, _scale, 0, 0);
                _scene.Update(e.DeltaTime / 1000f);
                gfx.BeginScene();
                gfx.ClearScene();
                gfx.DrawText(_font, _whiteBrush, default, $"FPS: {1000 / e.DeltaTime}");
                gfx.DrawText(_font, _whiteBrush, new(0, 30), $"Velocity: {Lander.Velocity.Length()}");


                _scene.EnumSceneObjects(DrawBox, gfx);


                gfx.TransformStart(_scaleMatrix);


                bool isColliding = false;
                foreach (var m in Colliders)
                {
                    if (Lander.IsIntersectingWith(m))
                    {
                        isColliding = true;
                        gfx.TransformEnd();
                        if (Lander.Velocity.Length() < 3.5)
                        {
                            var score = (3.5f - Lander.Velocity.Length()) * 150;
                            score += Math.Max(10 - Lander.Position.DistanceTo(Pad.Position), 0) * 300;
                            score = MathF.Truncate(score * 100) / 100;
                            Message = $"Score: {score}, press Space to restart";
                            _canvas.IsRunning = false;
                        }
                        else
                        {
                            Message = $"Game Over, press Space to restart";
                            Explode();
                        }
                        gfx.DrawText(_font, _whiteBrush, new(0, 60), Message);
                        gfx.EndScene();
                        return;
                    }
                }


                _landerBrush = isColliding ? _fireBrush : _boxBrush;
#if DEBUG

                Vector2 point = default;

                var ray = new Line(Lander.Position - Lander.UpVector * 10, Lander.UpVector * -1000);

                gfx.DrawLine(_fireBrush, ray, 1);
                var hits = new List<Vector2>();
                if (Moon.TryRayCast(ray, out point, hits))
                {
                    Message = $"{point} {hits.Count}";
                    gfx.DrawCircle(_fireBrush, point.ToPointF(), 3, 1f);
                }

                Moon.EnumSceneObjects((b) =>
                {
                    b.EnumEdges((e) =>
                    {
                        gfx.DrawLine(_fireBrush, e, 1);
                    });
                });

                gfx.DrawLine(_fireBrush, Lander.Project(Ground.XAxis), 0.5f);
                gfx.DrawLine(_fireBrush, Lander.Project(Ground.YAxis), 0.5f);
                gfx.DrawLine(_fireBrush, Ground.Project(Lander.XAxis), 0.5f);
                gfx.DrawLine(_fireBrush, Ground.Project(Lander.YAxis), 0.5f);


                gfx.DrawLine(_fireBrush, Ground.Project(Lander.YAxis), 0.5f);

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
                var dc = Random.Shared.Next(30, 50);
                for (int i = 0; i < dc; i++)
                {
                    var d = new Box(new(Random.Shared.Next(1, 5) * 0.5f, Random.Shared.Next(1, 5) * 0.5f))
                    {
                        Position =
                        Random.Shared.Next(-5, 5) * Lander.UpVector +
                        Random.Shared.Next(-5, 5) * Lander.RightVector +
                        Lander.Position
                    };
                    d.Velocity = (d.Position - Lander.Position) * 5 * Random.Shared.NextSingle() + Lander.Velocity * 0.3f;
                    d.Acceleration = Gravity * 2;
                    d.Angle = Random.Shared.NextSingle() * MathF.PI * 2;
                    d.AngularVelocity = (Random.Shared.NextSingle() - 0.5f) * MathF.PI * 4;
                    if (Random.Shared.Next(0, 3) == 1)
                        d.Brush = _fireBrush;
                    Moon.AddBox(d);
                }
                Lander.Position = default;
                Lander.Velocity = default;
                Lander.SetRemove();
            }

            void DrawBox(Box b, Graphics gfx)
            {
                gfx.TransformStart(RotationTransform(b.Angle, b.Position) * _scaleMatrix);

                if (b == Lander)
                {
                    DrawLander(b, gfx);
                }
                else
                {
                    var hw = b.Size.X / 2;
                    var hh = b.Size.Y / 2;
                    var brush = b.Brush ?? (b == Pad ? _whiteBrush : _boxBrush);
                    gfx.DrawBox2D(brush, brush, new RectangleF(-hw, -hh, hw, hh), 0);
                }
                gfx.TransformEnd();
            }

            private void DrawLander(Box b, Graphics gfx)
            {
                var hw = b.Size.X / 2;
                var hh = b.Size.Y / 2;
                var thrustPos = new Vector2(0, 1);
                var headPos = new Vector2(0, thrustPos.Y - hw);
                // draw head
                gfx.FillCircle(_landerBrush, headPos.ToPointF(), hw);
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
#if DEBUG
                 gfx.DrawBox2D(_debugBrush, _debugBrush, new RectangleF(-hw, -hh, hw, hh), 0.1f);
#endif
            }

            static TransformationMatrix RotationTransform(float angel, Vector2 center)
            {
                var c = (float)Math.Cos(angel);
                var s = (float)Math.Sin(angel);
                return new(c, -s, s, c, center.X, center.Y);
            }
        }
    }
}
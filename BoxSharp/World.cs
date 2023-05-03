using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoxSharp
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">Type of tag to be appended for each objects</typeparam>
    public class World<T> : IEnumerable<Shape<T>>
    {
        public event Action<Manifold<T>> OnCollision;
        Thread _physicsThread;
        bool _stopping;
        readonly List<Shape<T>> _objects = new();
        readonly IEnumerator<Shape<T>> _enumerator;
        Manifold<T> _manifold = new();
        public float PhysicsFPS
        {
            get => 1 / _timeWarp;
            set => _timeWarp = 1 / value;
        }
        float _timeWarp = 1 / 60;
        public Vector2 Size { get; }
        public World(Vector2 size)
        {
            _enumerator = _objects.GetEnumerator();
            Size = size;
        }

        public void Start()
        {
            _stopping = false;
            _physicsThread = new(UpdateLoop);
            _physicsThread.Start();
        }
        public void UpdateLoop()
        {
            while (!_stopping)
            {
                Update(_timeWarp);
            }
        }

        public void Update(float timeWarp)
        {
            for (int i = 0; i < _objects.Count;)
            {
                _objects[i].Update(timeWarp);
                if (_objects[i].Remove)
                {
                    _objects[i].Remove = false;
                    _objects.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            // Resolve collisions
            EnumCollisionPairs(ResolveCollision);
        }

        bool ResolveCollision(Shape<T> A,Shape<T> B)
        {
            if (A.IsStatic && B.IsStatic)
                return true;
            _manifold.A = A;
            _manifold.B = B;
            _manifold.Solve();
            if (_manifold.contactCount > 0)
            {
                _manifold.MixMaterials();
                _manifold.ApplyImpulse();
                _manifold.PositionalCorrection();
                OnCollision?.Invoke(_manifold);
            }
            return true;
        }

        public void Stop()
        {
            _stopping = true;
            if (_physicsThread?.IsAlive == true)
                _physicsThread.Join();
        }
        public void Clear()
        {
            _objects.Clear();
        }
        public void Add(Shape<T> obj)
        {
            _objects.Add(obj);
        }

        public bool TryRayCast(Line ray, out Vector2 result, out Shape<T> hit, List<(Shape<T>, Vector2)> hits = null)
        {
            result = default;
            hit = default;
            if (hits == null)
            {
                hits = new();
            }
            else
            {
                hits.Clear();
            }
            EnumObjects((b) =>
            {
                if (b is Polygon<T> p)
                {
                    p.EnumEdges((e) =>
                    {
                        if (ray.TryIntersect(e, out var hit, true))
                        {
                            hits.Add((p, hit));
                        }
                    });
                }
                return true;
            });

            if (hits.Count == 0)
                return false;

            result = hits[0].Item2;
            float closest = hits[0].Item2.DistanceToSquared(ray.Start);
            for (int i = 1; i < hits.Count; i++)
            {
                var h = hits[i];
                var ds = h.Item2.DistanceToSquared(ray.Start);
                if (ds < closest)
                {

                    closest = (result = h.Item2).DistanceToSquared(ray.Start);
                    hit = h.Item1;
                }
            }
            return true;
        }

        public void EnumObjects(Func<Shape<T>, bool> proc)
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                if (!proc(_objects[i]))
                    return;
            }
        }

        public void EnumObjects<TParam>(Func<Shape<T>, TParam, bool> proc, TParam parameter)
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                if (!proc(_objects[i], parameter))
                    return;
            }
        }

        public void EnumCollisionPairs(Func<Shape<T>, Shape<T>, bool> proc)
        {

            for (int i = 0; i < _objects.Count; i++)
            {
                for (int j = i + 1; j < _objects.Count; j++)
                {
                    var b1 = _objects[i];
                    var b2 = _objects[j];
                    if (b1.CollisionGroup == b2.CollisionGroup)
                    {
                        if (!proc(b1,b2))
                            return;
                    }
                }
            }
        }

        public IEnumerator<Shape<T>> GetEnumerator() => _enumerator;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

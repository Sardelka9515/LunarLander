using System;
using System.Collections;
using System.Collections.Generic;
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
        Thread _physicsThread;
        bool _stopping;
        int _curCollisionIndex = 0;
        readonly List<Shape<T>> _objects = new();
        readonly IEnumerator<Shape<T>> _enumerator;
        List<(Shape<T>, Shape<T>)> _collisionPairs;
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
            _collisionPairs ??= CalculateCollisionPairs();
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
            if (obj.CollisionIndex == -1)
                obj.CollisionIndex = _curCollisionIndex++;
            _objects.Add(obj);
            _collisionPairs = null;
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

        public void EnumCollisionPairs(Func<Shape<T>, Shape<T>, bool> proc)
        {
            _collisionPairs ??= CalculateCollisionPairs();
            for (int i = 0; i < _collisionPairs.Count; i++)
            {
                var p = _collisionPairs[i];
                if (!proc(p.Item1, p.Item2))
                    return;
            }
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

        public List<(Shape<T>, Shape<T>)> CalculateCollisionPairs()
        {
            var result = new List<(Shape<T>, Shape<T>)>();
            for (int i = 0; i < _objects.Count; i++)
            {
                for (int j = i + 1; j < _objects.Count; j++)
                {
                    var b1 = _objects[i];
                    var b2 = _objects[j];
                    if (b1.CollisionIndex != b2.CollisionIndex)
                        result.Add((b1, b2));
                }
            }
            return result;
        }

        public IEnumerator<Shape<T>> GetEnumerator() => _enumerator;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

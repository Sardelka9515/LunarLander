using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LunarLander
{
    internal class Scene : IEnumerable<Box>
    {
        Thread _physicsThread;
        bool _stopping;
        int _curCollisionIndex = 0;
        List<Box> _objects = new();
        List<(Box, Box)> _collisionPairs;
        public float PhysicsFPS
        {
            get => 1 / _timeWarp;
            set => _timeWarp = 1 / value;
        }
        float _timeWarp = 1 / 60;
        public Vector2 Size { get; }
        public Scene(Vector2 size)
        {
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
        public void AddBox(Box box)
        {
            if (box.CollisionIndex == -1)
                box.CollisionIndex = _curCollisionIndex++;
            _objects.Add(box);
            _collisionPairs = null;
        }

        public bool TryRayCast(Line ray, out Vector2 result, out Shape hit, List<(Shape, Vector2)> hits = null)
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
            EnumSceneObjects((b) =>
            {
                if (b is Polygon p)
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

        public void EnumCollisionPairs(Func<Box, Box, bool> proc)
        {
            _collisionPairs ??= CalculateCollisionPairs();
            for (int i = 0; i < _collisionPairs.Count; i++)
            {
                var p = _collisionPairs[i];
                if (!proc(p.Item1, p.Item2))
                    return;
            }
        }
        public void EnumSceneObjects(Func<Shape, bool> proc)
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                if (!proc(_objects[i]))
                    return;
            }
        }

        public void EnumSceneObjects<T>(Func<Box, T, bool> proc, T parameter)
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                if (!proc(_objects[i], parameter))
                    return;
            }
        }

        public List<(Box, Box)> CalculateCollisionPairs()
        {
            var result = new List<(Box, Box)>();
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

        public IEnumerator<Box> GetEnumerator() => _objects.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

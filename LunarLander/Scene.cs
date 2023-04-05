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
        List<Box> _objects = new();
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
            _objects.Add(box);
        }

        public bool TryRayCast(Line ray, out Vector2 result, List<Vector2> hits = null)
        {
            result = default;
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
                b.EnumEdges((e) =>
                {
                    if (ray.TryIntersect(e, out var hit, true))
                    {
                        hits.Add(hit);
                    }
                });
            });

            if (hits.Count == 0)
                return false;

            result = hits[0];
            float closest = hits[0].DistanceToSquared(ray.Start);
            for (int i = 1; i < hits.Count; i++)
            {
                var h = hits[i];
                var ds = h.DistanceToSquared(ray.Start);
                if (ds < closest)
                    closest = (result = h).DistanceToSquared(ray.Start);
            }
            return true;
        }

        public void EnumSceneObjects(Action<Box> proc)
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                proc(_objects[i]);
            }
        }

        public void EnumSceneObjects<T>(Action<Box, T> proc, T parameter)
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                proc(_objects[i], parameter);
            }
        }

        public IEnumerator<Box> GetEnumerator() => _objects.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

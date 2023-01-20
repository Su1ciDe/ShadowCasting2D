using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ShadowCasting
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class LineOfSight : MonoBehaviour
    {
        public float Radius;
        [Range(0f, 360f)]
        public float Angle;
        public float Resolution;

        [Space]
        [SerializeField] private int maxTargetSize = 10;
        [SerializeField] private float searchInterval = .1f;
        [SerializeField] private int edgeResolveIteration = 10;
        [SerializeField] private float edgeDistanceThreshold = .5f;

        [Space]
        public LayerMask ObstacleMask;
        public LayerMask TargetMask;

        public DebugOptions Debug;
        [SerializeField] private Info info;

        public List<Transform> Targets { get; set; } = new List<Transform>();

        private Coroutine findTargetsCoroutine;
        private WaitForSeconds searchWait;

        private MeshFilter meshFilter;
        private Mesh mesh;

        public event UnityAction<Target> OnTargetInSight;

        private void Awake()
        {
            mesh = new Mesh();
            mesh.name = "LineOfSightMesh";
            meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            searchWait = new WaitForSeconds(searchInterval);
        }

        private void Start()
        {
            findTargetsCoroutine = StartCoroutine(FindTargetsCoroutine());
        }

        private void LateUpdate()
        {
            DrawLineOfSight();
        }

        private void OnDisable()
        {
            if (findTargetsCoroutine is not null)
            {
                StopCoroutine(findTargetsCoroutine);
                findTargetsCoroutine = null;
            }
        }

        private IEnumerator FindTargetsCoroutine()
        {
            while (true)
            {
                yield return searchWait;

                FindVisibleTargets();
            }
        }

        private void FindVisibleTargets()
        {
            Targets.Clear();

            var targetInView = new Collider2D[maxTargetSize];
            var size = Physics2D.OverlapCircleNonAlloc(transform.position, Radius, targetInView, TargetMask);
            for (int i = 0; i < size; i++)
            {
                var target = targetInView[i].transform;
                Vector2 dir = (target.position - transform.position).normalized;
                if (Vector2.Angle(transform.up, dir) < Angle / 2f)
                {
                    float distance = Vector2.Distance(transform.position, target.position);
                    if (!Physics2D.Raycast(transform.position, dir, distance, ObstacleMask))
                    {
                        Targets.Add(target);
                        if (targetInView[i].attachedRigidbody && targetInView[i].attachedRigidbody.TryGetComponent(out Target _target))
                        {
                            _target.OnGettingInSight();
                            OnTargetInSight?.Invoke(_target);
                        }
                    }
                }
            }

#if UNITY_EDITOR
            info.TargetsInView = Targets;
#endif
        }

        private void DrawLineOfSight()
        {
            int stepCount = Mathf.RoundToInt(Angle * Resolution);
            float stepAngleSize = Angle / stepCount;

            var viewPoints = new List<Vector2>();
            var prevViewCast = new ViewCastInfo();

            for (int i = 0; i <= stepCount; i++)
            {
                float angle = -transform.eulerAngles.z - Angle / 2f + stepAngleSize * i;
                var viewCast = ViewCast(angle);

                if (i > 0)
                {
                    var edgeThresholdExceeded = Mathf.Abs(prevViewCast.Distance - viewCast.Distance) > edgeDistanceThreshold;
                    if (!prevViewCast.Hit.Equals(viewCast.Hit) || (prevViewCast.Hit && viewCast.Hit && edgeThresholdExceeded))
                    {
                        var edge = FindEdge(prevViewCast, viewCast);
                        if (!edge.PointA.Equals(Vector2.zero))
                            viewPoints.Add(edge.PointA);

                        if (!edge.PointB.Equals(Vector2.zero))
                            viewPoints.Add(edge.PointB);
                    }
                }

                viewPoints.Add(viewCast.Point);
                prevViewCast = viewCast;
            }

            int vertexCount = viewPoints.Count + 1;
            var vertices = new Vector3[vertexCount];
            var triangles = new int[(vertexCount - 2) * 3];

            vertices[0] = Vector2.zero;
            for (int i = 0; i < vertexCount - 1; i++)
            {
                vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);
                if (i < vertexCount - 2)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateTangents();
            mesh.RecalculateNormals();
        }

        private ViewCastInfo ViewCast(float globalAngle)
        {
            var dir = DirectionFromAngle(globalAngle, false);
            var hit = Physics2D.Raycast(transform.position, dir, Radius, ObstacleMask);

            if (hit)
            {
#if UNITY_EDITOR
                if (Debug.ShowResolutionDebug)
                    UnityEngine.Debug.DrawLine(transform.position, hit.point, Debug.ResolutionDebugColor);
#endif
                return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
            }
            else
            {
#if UNITY_EDITOR
                if (Debug.ShowResolutionDebug)
                    UnityEngine.Debug.DrawLine(transform.position, (Vector2)transform.position + dir * Radius, Debug.ResolutionDebugColor);
#endif
                return new ViewCastInfo(false, (Vector2)transform.position + dir * Radius, Radius, globalAngle);
            }
        }

        private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
        {
            float minAngle = minViewCast.Angle;
            float maxAngle = maxViewCast.Angle;
            var minPoint = Vector2.zero;
            var maxPoint = Vector2.zero;

            for (int i = 0; i < edgeResolveIteration; i++)
            {
                float angle = (minAngle + maxAngle) / 2f;
                var viewCast = ViewCast(angle);

                var edgeThresholdExceeded = Mathf.Abs(minViewCast.Distance - viewCast.Distance) > edgeDistanceThreshold;
                if (viewCast.Hit.Equals(minViewCast.Hit) && !edgeThresholdExceeded)
                {
                    minAngle = angle;
                    minPoint = viewCast.Point;
                }
                else
                {
                    maxAngle = angle;
                    maxPoint = viewCast.Point;
                }
            }

            return new EdgeInfo(minPoint, maxPoint);
        }

        public Vector2 DirectionFromAngle(float angleDeg, bool isLocal = true)
        {
            if (isLocal)
                angleDeg -= transform.eulerAngles.z;

            return new Vector2(Mathf.Sin(angleDeg * Mathf.Deg2Rad), Mathf.Cos(angleDeg * Mathf.Deg2Rad));
        }

        private struct ViewCastInfo
        {
            public bool Hit;
            public Vector2 Point;
            public float Distance;
            public float Angle;

            public ViewCastInfo(bool hit, Vector2 point, float distance, float angle)
            {
                Hit = hit;
                Point = point;
                Distance = distance;
                Angle = angle;
            }
        }

        private struct EdgeInfo
        {
            public Vector2 PointA;
            public Vector2 PointB;

            public EdgeInfo(Vector2 pointA, Vector2 pointB)
            {
                PointA = pointA;
                PointB = pointB;
            }
        }

        [System.Serializable]
        public struct DebugOptions
        {
            public bool ShowDebug;
            public Color DebugColor;
            [Space]
            public bool ShowResolutionDebug;
            public Color ResolutionDebugColor;
        }

        [System.Serializable]
        public struct Info
        {
            [ReadOnly]
            [SerializeField] public List<Transform> TargetsInView;
        }
    }
}
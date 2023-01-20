using System.Collections.Generic;
using UnityEngine;

namespace ShadowCasting.Demo
{
    /// <summary>
    /// This script generates EdgeCollider2Ds to edges of the screen
    /// If your camera is not stationary then attach this script to the camera
    /// </summary>
    public class ScreenEdgeCollider : MonoBehaviour
    {
        private EdgeCollider2D[] edgeColliders;
        private float aspectRatio;
        private float camSize;

        private Camera cam;

        private void Awake()
        {
            edgeColliders = new EdgeCollider2D[4];
            cam = Camera.main;
            camSize = cam.orthographicSize;
            aspectRatio = (float)cam.pixelWidth / cam.pixelHeight;
        }

        private void Start()
        {
            if (!cam.orthographic)
            {
                Debug.LogWarning("This only works if the Camera is orthographic!");
                return;
            }

            SetupColliders();
        }

        private void SetupColliders()
        {
            if (TryGetComponent(out EdgeCollider2D _))
            {
                edgeColliders = GetComponents<EdgeCollider2D>();
                for (int i = 0; i < 4; i++)
                {
                    SetupCollider(edgeColliders[i], i);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    var col = gameObject.AddComponent<EdgeCollider2D>();
                    edgeColliders[i] = SetupCollider(col, i);
                }
            }
        }

        
        //(-,+) ---------- (+,+)
        //      |        |
        //      |        |
        //      |        |
        //(-,-) ---------- (+,-)
        private EdgeCollider2D SetupCollider(EdgeCollider2D edgeCollider2D, int index)
        {
            var points = new List<Vector2>();
            for (int i = 0; i < 2; i++)
            {
                int delta = 45 + (index + i) * 90;
                var signX = Mathf.Sign(Mathf.Cos(Mathf.Deg2Rad * delta));
                var signY = Mathf.Sign(Mathf.Sin(Mathf.Deg2Rad * delta));

                points.Add(new Vector2(signX * camSize * aspectRatio, signY * camSize));
            }

            edgeCollider2D.SetPoints(points);
            return edgeCollider2D;
        }
    }
}
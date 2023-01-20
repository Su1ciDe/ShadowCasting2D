using UnityEditor;
using UnityEngine;

namespace ShadowCasting.Editor
{
    [CustomEditor(typeof(LineOfSight))]
    public class LineOfSightEditor : UnityEditor.Editor
    {
        private LineOfSight los;

        private void OnEnable()
        {
            los = (LineOfSight)target;
        }

        private void OnSceneGUI()
        {
            if (!los.Debug.ShowDebug) return;

            var pos = los.transform.position;

            Handles.color = los.Debug.DebugColor;
            Handles.DrawWireArc(pos, Vector3.forward, Vector3.up, 360, los.Radius);

            if (!los.Angle.Equals(360))
            {
                var viewAngleA = los.DirectionFromAngle(-los.Angle / 2);
                var viewAngleB = los.DirectionFromAngle(los.Angle / 2);

                Handles.DrawLine(pos, pos + (Vector3)viewAngleA * los.Radius);
                Handles.DrawLine(pos, pos + (Vector3)viewAngleB * los.Radius);
            }

            Handles.color = Color.red;
            foreach (var t in los.Targets)
                Handles.DrawLine(los.transform.position, t.position);
        }
    }
}
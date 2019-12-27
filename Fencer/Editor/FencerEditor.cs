using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace FencerUtility
{

    [CustomEditor(typeof(Fencer))]
    public class FencerEditor : Editor
    {
        public bool debugMode = true;

        public Vector2 currentMousePosition;

        private Fencer fencerInstance;

        public override void OnInspectorGUI()
        {

            //dbg("Current Event OnInspectorGUI : " + Event.current.type.ToString());

            base.OnInspectorGUI();

            Fencer fencerInstance = (Fencer)target;

            if (!fencerInstance.CreatingMode)
            {
                // draw button will only be shown if the fencer is not allready in editing mode
                if (GUILayout.Button("Draw"))
                {
                    fencerInstance.CreateFence();
                }
            }
            else
            {

                // button for closing the polygon, meaning the last point will be connected to the first polygon point
                if (GUILayout.Button("Close Polygon"))
                {
                    fencerInstance.isPolygon = true;
                    fencerInstance.CloseFence();
                    
                }

                // button for ending the drawing without connecting the last point to the first polygon point
                if (GUILayout.Button("Finish without Closing"))
                {
                    fencerInstance.isPolygon = false;
                    fencerInstance.CloseFence();
                    
                }
            }

            // if there are more than two points drawn, the button for playing the fence elements on the edges will be shown
            if (fencerInstance.FencePoints.Count > 2)
                if (GUILayout.Button("Place Fence"))
                {
                    fencerInstance.GenerateFence();
                }


        }

        public void OnSceneGUI()
        {

            // get the current instance of the gameobject this script is attached to
            fencerInstance = (Fencer)this.target;

            if(fencerInstance.GetComponent<Collider>()==null)
            {
                dbg("The Object to place the fence on MUST have some sort of collider attached to it.",true);
            }

            // paints the fence polygon if there is one and the repaint event is the current event
            if (Event.current.type == EventType.Repaint)
            {

                List<Vector3> fencePoints = new List<Vector3>();

                if(fencerInstance.FencePoints != null)
                    fencePoints = fencerInstance.FencePoints;

                float fencePointDiscRadius = 1f;

                for (int fencePointIndex = 0; fencePointIndex < fencePoints.Count; fencePointIndex++)
                {

                    if (fencePointIndex == 0)
                    {
                        Handles.color = Color.green;
                        Handles.DrawSolidDisc(fencePoints[fencePointIndex], Vector3.up, fencePointDiscRadius * 3f);

                        if (!fencerInstance.CreatingMode & fencePoints.Count > 0)
                        {
                            Handles.DrawLine(fencePoints[fencePointIndex], fencePoints[fencePoints.Count - 1]);
                        }
                    }
                    else
                    {
                        Handles.color = Color.white;
                        Handles.DrawSolidDisc(fencePoints[fencePointIndex], Vector3.up, fencePointDiscRadius);
                        //if(fencePoints.Count > 2)
                        //    Handles.DrawLine(fencePoints[fencePointIndex],fencePoints[fencePointIndex+1]);
                        if (fencerInstance.CreatingMode & fencePointIndex > 0)
                        {
                            Handles.DrawLine(fencePoints[fencePointIndex], fencePoints[fencePointIndex - 1]);
                        }

                        if (!fencerInstance.CreatingMode & fencePointIndex > 0)
                        {
                            Handles.DrawLine(fencePoints[fencePointIndex], fencePoints[fencePointIndex - 1]);
                        }
                    }



                }




            }

            // if Fencer is in Creating mode, then check the apropriate Events and draw mouse mausposition and partial line
            if (fencerInstance.CreatingMode)
            {

                this.DrawCurrentMousePositionAndCurrentFencePartLine();

                if (Event.current.type == EventType.Layout)
                {

                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
                }

                // update mouse position for the drawing of the current next fence point
                // needed for the case the mouse doesnt move at all
                if (Event.current.type == EventType.MouseMove)
                {

                    this.currentMousePosition = Event.current.mousePosition;
                    Event.current.Use();
                }


                if (Event.current.type == EventType.MouseDown)
                {

                    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit))
                    {

                        if (hit.collider.gameObject == fencerInstance.gameObject)
                        {
                            fencerInstance.AddFencePoint(hit.point);
                        }

                        //Debug.DrawLine(ray.origin, hit.point, Color.red);
                        //Debug.Log(hit.point);
                        //dbg("Mouse Hit clicked on Scene View at : " + hit.point.ToString());
                        //dbg("Hit Object with Name : "+hit.collider.gameObject.name);

                    }

                    Event.current.Use();

                }


            }

        }

        private void DrawCurrentMousePositionAndCurrentFencePartLine()
        {
            // draw a sold disc at the current mouse position
            Ray rayMousePointer = HandleUtility.GUIPointToWorldRay(this.currentMousePosition);

            RaycastHit hitMousePointer;

            if (Physics.Raycast(rayMousePointer, out hitMousePointer))
            {
                if (hitMousePointer.collider.gameObject == fencerInstance.gameObject)
                {

                    Handles.DrawSolidDisc(hitMousePointer.point, Vector3.up, 2f);

                    // Draw a line from the last fence point to the current mouse position
                    if (fencerInstance.FencePoints.Count > 0)
                    {
                        Vector3 previousSetFencerPoint = fencerInstance.FencePoints[fencerInstance.FencePoints.Count - 1];

                        // get the direction normal and the length of the current potential fence point relativ to the last set
                        Vector3 direction = (hitMousePointer.point - previousSetFencerPoint).normalized;

                        float length = (hitMousePointer.point - previousSetFencerPoint).magnitude;

                        // get the size of the fence prefab element
                        Bounds fencePrefabBounds = fencerInstance.fencePrefab.GetComponent<Renderer>().bounds;

                        float lengthOfFencePrefab = fencePrefabBounds.size.x;

                        // calculate how many elements of the prefab would fit
                        int numberOfFencesThatWouldFitOnTheNewLineSegment = (int)(length / lengthOfFencePrefab);

                        for (int i = 0; i < numberOfFencesThatWouldFitOnTheNewLineSegment; i++)
                        {
                            Handles.color = Color.red;
                            Handles.DrawSolidDisc(previousSetFencerPoint + ((direction * lengthOfFencePrefab) * (i + 1)), Vector3.up, 0.5f);
                        }

                        Handles.color = Color.white;
                        Handles.DrawLine(previousSetFencerPoint, hitMousePointer.point);
                    }
                }
            }
        }

        protected void dbg(string message, bool error = false)
        {
            if (debugMode & !error)
                Debug.Log("[ " + this.GetType().Name + " (" + Time.time + ")] " + message);

            if (error)
                Debug.LogError("[" + this.GetType().Name + " (" + Time.time + ")] " + message);
        }
    }

}

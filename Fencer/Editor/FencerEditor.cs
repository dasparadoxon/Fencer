/*

    FENCER - A unity helper to quickly build fence-like structures on meshes or terrains

    https://github.com/dasparadoxon/Fencer

    2019 written by Tom Trottel, dpgaming blog

    Version 0.2.1

    Any modified distribution without reference to the original is not allowed. 

    Usage is free as in free software. 

    I will add a proper license soon some time.

*/

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

        private Vector3 lastFullFenceElementLengthPositionSuggestion = Vector3.zero;

        public override void OnInspectorGUI()
        {

            base.OnInspectorGUI();



            Fencer fencerInstance = (Fencer)target;

            if (!fencerInstance.CreatingMode)
            {
                // draw button will only be shown if the fencer is not allready in editing mode
                if (GUILayout.Button("Draw"))
                {
                    fencerInstance.CreateFence();
                }

                lastFullFenceElementLengthPositionSuggestion = Vector3.zero;
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

            if (fencerInstance.GetComponent<Collider>() == null)
            {
                dbg("The Object to place the fence on MUST have some sort of collider attached to it.", true);
            }

            // paints the fence polygon if there is one and the repaint event is the current event
            if (Event.current.type == EventType.Repaint)
            {

                List<Vector3> fencePoints = new List<Vector3>();

                if (fencerInstance.FencePoints != null)
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

                // user pressed (right) mouse button, time to place a new polygon point
                if (Event.current.type == EventType.MouseDown)
                {
                    if (!fencerInstance.correctLength || lastFullFenceElementLengthPositionSuggestion == Vector3.zero)
                    {

                        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit))
                        {

                            if (hit.collider.gameObject == fencerInstance.gameObject)
                            {
                                fencerInstance.AddFencePoint(hit.point);
                            }

                        }
                    }
                    else

                    if (fencerInstance.correctLength)
                    {
                        fencerInstance.AddFencePoint(this.lastFullFenceElementLengthPositionSuggestion);
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

                    Handles.DrawSolidDisc(hitMousePointer.point, Vector3.up, fencerInstance.sizeOfDrawingPoints);

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

                        // draw points showing the new fence elements
                        for (int i = 0; i < numberOfFencesThatWouldFitOnTheNewLineSegment; i++)
                        {
                            Handles.color = Color.red;

                            Handles.DrawSolidDisc(previousSetFencerPoint + ((direction * lengthOfFencePrefab) * (i + 1)), Vector3.up,
                            fencerInstance.sizeOfDrawingPoints / 2);
                        }

                        // this is the point needed for automatically correction of the edge length, so that always a correct closing of the 
                        // fence number happens. 
                        // note that a 0.01 amount is added to the length, so that there is in every case a minimal overlength so that the 
                        // fence placing loop works correct in every case

                        this.lastFullFenceElementLengthPositionSuggestion =
                            previousSetFencerPoint +
                            ((direction * lengthOfFencePrefab) * (numberOfFencesThatWouldFitOnTheNewLineSegment + 1)
                            + (direction * (lengthOfFencePrefab * 0.01f)));

                        Handles.color = Color.white;

                        Handles.DrawLine(previousSetFencerPoint, hitMousePointer.point);

                        // calculate how much the current position has reached a new fence element length
                        float amountOfLengthForANewFenceElement = (length % lengthOfFencePrefab);

                        // calculate how much more length is need for a new fence element to be placed
                        float amountNeededToBeAbleToPlaceANewFenceLement = lengthOfFencePrefab - amountOfLengthForANewFenceElement;

                        /*
                        Handles.color = Color.yellow;

                        Handles.DrawSolidDisc(previousSetFencerPoint 
                        + ((direction * numberOfFencesThatWouldFitOnTheNewLineSegment) +
                        (direction * amountNeededToBeAbleToPlaceANewFenceLement)), Vector3.up, 0.5f);
                        */

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

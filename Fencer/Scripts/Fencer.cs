/*

    FENCER - A unity helper to quickly build fence-like structures on meshes or terrains

    https://github.com/dasparadoxon/Fencer

    2019 written by Tom Trottel, dpgaming blog

    Version 0.2.1

    Any modified distribution without reference to the original is not allowed. 

    Usage is free as in free software. 

    I will add a proper license soon some time.

*/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FencerUtility
{

    public class Fencer : MonoBehaviour
    {
        #region Public Fields

        [HideInInspector]
        public bool debugMode = false;

        [HideInInspector]
        public bool creatingMode = false;

        [HideInInspector]
        public bool isPolygon = true;

        // enables the automatic shortening of a new polygonline to a length that fits to the last full fence element
        public bool correctLength = true;

        public GameObject fencePrefab = null;

        public GameObject fenceContainer = null;

        [Range(0.5f,5f)]
        public float sizeOfDrawingPoints = 2f;

        // The List containing the points for the fence polygon/line segments
        // right now they can be edited by hand, but they need a own custom editor widget so they
        // be added more easily/clearly until they can be changed by clicking on the drawn points 
        public List<Vector3> fencePoints;

        public List<Vector3> FencePoints { get => fencePoints; set => fencePoints = value; }

        // this is the bool that is set when the user wants to draw a fence polygon and is used in the custom editor class
        // to act accordingly
        public bool CreatingMode { get => creatingMode; set => creatingMode = value; }

        #endregion

        #region Private Fields

        #endregion


        #region Unity Methods

        public void OnValidate()
        {
        }

        #endregion

        #region Class Methods

        /// Is called by the Editor Class when the fence should be drawn and the button is clicked in the editor
        public void CreateFence()
        {

            // only start fence drawing if there is an actual fence prefab set
            if (this.fencePrefab != null)
            {
                this.CreatingMode = true;
                this.FencePoints.Clear();
            }
            else
                dbg("You need to set a prefab fence element !", true);

        }

        /// Is called by the Editor class when the user wants to close the fence polygon
        public void CloseFence()
        {

            this.CreatingMode = false;
        }

        public void GenerateFence()
        {

            // if there is no fenceContainer, create a new one
            if (fenceContainer == null)
            {
                GameObject newFenceContainer = new GameObject();

                newFenceContainer.name = "Unnamend Fence Container";

                this.fenceContainer = newFenceContainer;

            }

            for (var currentFencePointIndex = 0; currentFencePointIndex < this.fencePoints.Count; currentFencePointIndex++)
            {

                Vector3 firstPoint = this.fencePoints[currentFencePointIndex];

                Vector3 secondPoint = Vector3.zero;

                // if it is the last point, then the second point has to be the start point of the fencepoints
                if (currentFencePointIndex == (this.fencePoints.Count - 1))
                    secondPoint = this.fencePoints[0];
                else
                    secondPoint = this.fencePoints[currentFencePointIndex + 1];

                // now I need the angle of the edge consisting of both points relative to the X axsis.

                // make it relative to the origin (0,0,0)
                Vector3 edgeVector = secondPoint - firstPoint;

                // calculate the signed angle between the positive x-axis and the edge
                float signedAngleBetweenXAxisAndCurrentEdge = Vector3.SignedAngle(Vector3.right, edgeVector, Vector3.up);

                // with this angle we need to fill the whole edge with the according number of fence elements

                // get the direction normal and the length of the current potential fence point relativ to the last set
                Vector3 direction = (secondPoint - firstPoint).normalized;

                float length = (secondPoint - firstPoint).magnitude;

                // get the size of the fence prefab element
                Bounds fencePrefabBounds = this.fencePrefab.GetComponent<Renderer>().bounds;

                float lengthOfFencePrefab = fencePrefabBounds.size.x;

                // calculate how many elements of the prefab would fit
                int numberOfFencesThatWouldFitOnTheNewLineSegment = (int)(length / lengthOfFencePrefab);

                for (int i = 0; i < numberOfFencesThatWouldFitOnTheNewLineSegment; i++)
                {
                    Vector3 positionOfFenceElement = firstPoint + ((direction * lengthOfFencePrefab) * (i));

                    Vector3 firstPointPartialEdgeForFenceElement = positionOfFenceElement;

                    Vector3 secondPointPartialEdgeForFenceElement = firstPoint + ((direction * lengthOfFencePrefab) * (i + 1));

                    // now to get the correct y-axis (height) for each of the two fence element corner points

                    RaycastHit hit;
                    // Does the ray intersect any objects excluding the player layer
                    if (Physics.Raycast(firstPointPartialEdgeForFenceElement + new Vector3(0,5f,0), transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity))
                    {

                        if (hit.collider.gameObject == this.gameObject)
                        {
                            firstPointPartialEdgeForFenceElement.y = hit.point.y;
                            positionOfFenceElement.y = hit.point.y;
                        }
                    }

                    if (Physics.Raycast(secondPointPartialEdgeForFenceElement + new Vector3(0,5f,0) , transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity))
                    {
                        if (hit.collider.gameObject == this.gameObject)
                        {
                            secondPointPartialEdgeForFenceElement.y = hit.point.y;
                        }
                    }

                    // transform it to origin zero
                    Vector3 partialEdgeVector = secondPointPartialEdgeForFenceElement - firstPointPartialEdgeForFenceElement;

                    // calculate the signed angle between the positive x-axis and the edge, in other words the stepness of the current edge part

                    Vector3 point1;
                    Vector3 point2;

                    point1 = partialEdgeVector;

                    point2 = point1.normalized * point1.magnitude;
                    point2.y = 0;

                    Vector3 normalVectorOfBothEdges = Vector3.Cross(point1, point2);

                    if (point1.z >= 0 && point1.x >= 0)
                        normalVectorOfBothEdges = Vector3.Cross(point1, point2);

                    if (point1.z < 0 && point1.x >= 0)
                        normalVectorOfBothEdges = Vector3.Cross(point2, point1);

                    if (point1.z < 0 && point1.x < 0)
                        normalVectorOfBothEdges = Vector3.Cross(point2, point1);

                    if (point1.z >= 0 && point1.x < 0)
                        normalVectorOfBothEdges = Vector3.Cross(point1, point2);

                    if (point1.z < 0 && point1.x >= 0 && point1.y < 0)
                        normalVectorOfBothEdges = Vector3.Cross(point1, point2);

                    if (point1.z < 0 && point1.x < 0 && point1.y < 0)
                        normalVectorOfBothEdges = Vector3.Cross(point1, point2);

                    if (point1.z >= 0 && point1.x < 0 && point1.y >= 0)
                        normalVectorOfBothEdges = Vector3.Cross(point2, point1);

                    Vector3 positionCrossDot = normalVectorOfBothEdges.normalized * 0.5f;

                    float signedAngle = Vector3.SignedAngle(point2, point1, positionCrossDot);

                    GameObject newFenceElement = Instantiate
                    (
                        this.fencePrefab,
                        positionOfFenceElement,
                        Quaternion.Euler(0, 0, 0)
                    );

                    newFenceElement.transform.Rotate(new Vector3(0, signedAngleBetweenXAxisAndCurrentEdge, 0), Space.Self);

                    newFenceElement.transform.Rotate(new Vector3(0, 0, signedAngle), Space.Self);

                    /*newFenceElement.name = "[" + i + "] | P1 : " + point1.ToString() + " | P2 : " + point2.ToString();*/

                    newFenceElement.name += " (" + i + ")";

                    newFenceElement.transform.parent = this.fenceContainer.transform;





                }




            }



        }

        public void AddFencePoint(Vector3 newFencePoint)
        {

            if (this.fencePoints == null)
                this.fencePoints = new List<Vector3>();

            this.fencePoints.Add(newFencePoint);
        }

        protected void dbg(string message, bool error = false)
        {
            if (debugMode & !error)
                Debug.Log("[ " + this.GetType().Name + " (" + Time.time + ")] " + message);

            if (error)
                Debug.LogError("[" + this.GetType().Name + " (" + Time.time + ")] " + message);
        }

        #endregion
    }

}

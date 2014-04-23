using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class StickFigureRenderer : MonoBehaviour {

	public Transform[] joints;
	public LineRenderer lineRenderer;


	// Use this for initialization
	void Start () {
		lineRenderer.SetVertexCount(joints.Length); 
	}
	
	// Update is called once per frame
	void Update () {
		for (int i = 0; i < joints.Length; i++)
		{
			lineRenderer.SetPosition(i, joints[i].position);
		}
	}
}

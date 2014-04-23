using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

public class AnimationRecorder : MonoBehaviour {
	
	
	//public GUIStyle customGuiStyle;

	// the root transform of the animation
	// (the object that the animation is attached to)
	public Transform animationRoot;
	// the joints to include in the animation
	public Transform [] animationJoints;

	// the name to call the animation
	public string clipName = "Anim";
	// used to add numbers to ensure the animation is different
	public int clipCounter = 0;


	//public Animation featureAnimation;
	
	//public GameObject kinectSensor;

	// the start tie of the animation
	private float startTime = -1;

	//public float StartTime{
	//	get {return startTime;}
	//}

	// the animation curves we are saving
	private AnimationCurve[,] curves;
	// the paths for the joints associated with 
	// each curve
	private string[] paths;
	
	// Use this for initialization
	void Start () {
	
	}

	// creates a keyframe from the current values of the joints
	void SaveKeyFrame(float t, AnimationCurve[,] _curves, Transform [] joints)
	{
		for (int i = 0; i < _curves.GetLength(0); i++)
		{
			_curves[i, 0].AddKey(new Keyframe(t, joints[i].localPosition.x));
			_curves[i, 1].AddKey(new Keyframe(t, joints[i].localPosition.y));
			_curves[i, 2].AddKey(new Keyframe(t, joints[i].localPosition.z));
			_curves[i, 3].AddKey(new Keyframe(t, joints[i].localRotation.x));
			_curves[i, 4].AddKey(new Keyframe(t, joints[i].localRotation.y));
			_curves[i, 5].AddKey(new Keyframe(t, joints[i].localRotation.z));
			_curves[i, 6].AddKey(new Keyframe(t, joints[i].localRotation.w));
		}
	}
	
	// Update is called once per frame
	// saves the current frame
	void Update () {
		// checks if we have set startTime yet
		if(startTime < 0)
			startTime = Time.time;
			
		// the keyframe time
		float t = Time.time - startTime;

		// save the keyframe to the curves
		SaveKeyFrame(t, curves, animationJoints);
	}

	// works out the path string that will identify the transform
	String CalculateTransformPath(Transform joint, Transform root)
	{
		if(joint == root || joint == null)
			return "";
		String rest = CalculateTransformPath(joint.parent, root);
		if(rest == "")
			return joint.name;
		else
			return rest + "/"+ joint.name;
	}

	// creates the animation curves
	void SetUpCurves(AnimationCurve [,] _curves, string[] _paths, Transform root, Transform[] joints)
	{
		for (int i = 0; i < joints.Length; i++)
		{
			if(joints[i])
			{
				_paths[i] = CalculateTransformPath (joints[i], root); 
				print (_paths[i]);
				for(int j = 0; j < 7; j++)
				{
					_curves[i,j] = new AnimationCurve();
				}
			}
		}
	}

	// start recording
	void OnEnable()
	{
		startTime = -1;
		
		curves = new AnimationCurve[animationJoints.Length,7];
		paths = new string[animationJoints.Length];
		SetUpCurves(curves, paths, animationRoot, animationJoints);
		
	}

	// finish recording and save the animation
	void OnDisable()
	{
		
		AnimationClip clip = new AnimationClip();
	
		for (int i = 0; i < curves.GetLength(0); i++)
		{
			if(curves[i,0] != null)
			{
				//print(paths[i]);
				clip.SetCurve(paths[i], typeof(Transform), "localPosition.x", curves[i,0]);
				clip.SetCurve(paths[i], typeof(Transform), "localPosition.y", curves[i,1]);
				clip.SetCurve(paths[i], typeof(Transform), "localPosition.z", curves[i,2]);
				clip.SetCurve(paths[i], typeof(Transform), "localRotation.x", curves[i,3]);
				clip.SetCurve(paths[i], typeof(Transform), "localRotation.y", curves[i,4]);
				clip.SetCurve(paths[i], typeof(Transform), "localRotation.z", curves[i,5]);
				clip.SetCurve(paths[i], typeof(Transform), "localRotation.w", curves[i,6]);
			}
		}

		string animationName = clipName;
		
		if(clipCounter > 0)
			animationName = animationName + "_" + clipCounter;
		
		
		animation.AddClip(clip, animationName);


		AssetDatabase.CreateAsset(clip, "Assets/Gestyour/RecordedAnimations/" + animationName + "_" + DateTime.Now.ToString("hh_mm_ss_dd_MM_yyyy") + ".anim");
		AssetDatabase.SaveAssets();
		
		clipCounter += 1;
	}

	
}

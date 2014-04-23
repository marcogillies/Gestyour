using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

/*
 *  A class for representing an individual posture
 */ 
[System.Serializable]
public class MotionDataItem
{
	public String label;
	public int labelIndex;
	public String clip;
	public float time;
	public float [] data;
	public float lastProbability;
	public Texture2D tex;
	public Texture2D selectedTex;
}

/*
 *  Does posture recognition 
 */
public class AIController : MonoBehaviour {

	// whether to record animation in play mode
	public Boolean recordData = false;

	// the last classification 
	public string classification = "nothing";

	// the component that records animations
	public AnimationRecorder animationRecorder;

	// the postures that are used for the nearest
	// neighbour recognition
	public MotionDataItem [] m_DataItems;

	// the largest probability in the last 
	// classification calculation
	private float maxProbability;

	
	// objects that will receive a 
	// message when there is a new 
	// classification
	public GameObject[] listeners;
	
	// controls the range of influence
	// of each pose
	public float defaultSigma = 1.0f;

	// the joints (transforms) used in 
	// the recognition calculations
	public Transform [] featureJoints;
	//public bool useVelocityFeatures;

	// the names of the classes
	public string [] classLabelArray;

	// the if of the current selected class
	public int currentClassId = -1;
	// the name of the current class
	// implemented as a property so it can
	// be calculated from the id
	public string currentClassName
	{
		get 
		{
			if(currentClassId >= 0)
				return classLabelArray[currentClassId];
			else
				return "";
		}
		set 
		{
			currentClassId = -1;
			for (int i = 0; i < classLabelArray.Length; i++)
			{
				if(classLabelArray[i] == value)
				{
					currentClassId = i;
				}
			}
		}
	}

	// the colours associated with 
	// each class
	public Color [] classColours;

	// the name to use for creating a new class
	public string newClassName = "";

	// the current selected posture
	public int currentPosture = -1;
	
	// get the numerical class id from it's name
	public int GetClassId(string classLabel)
	{
		for (int i = 0; i < classLabelArray.Length; i++)
		{
			if (classLabelArray[i] == classLabel)
			{
				return i;
			}
		}
		return -1;
	}

	// add a new class
	public void AddClass(string label)
	{
		// don't add if there is no label
		if(label.Equals(""))
			return;

		//  only add if it isn't already there
		if(GetClassId(label) < 0)
		{
			// do some array switching, because unity doesn't 
			// play well with more complex types
			Array.Resize(ref classLabelArray, classLabelArray.Length+1);
			classLabelArray[classLabelArray.Length-1] = label;
		}
	}
	
	// deletes a class
	public void RemoveClass(string label)
	{
		// check that there is actually a label
		if(label.Equals(""))
			return;
		
		// get the numerical id of the class
		int labelId = GetClassId(label);
		if(labelId >= 0)
		{
			// copy all the later labels down in the array
			for (int i = labelId; i < classLabelArray.Length-1; i++)
			{
				classLabelArray[i] = classLabelArray[i+1];
			}

			// do some array switching, because unity doesn't 
			// play well with more complex types
			Array.Resize(ref classLabelArray, classLabelArray.Length-1);

			// regenerate all of the labels for the data items
			// as the label ids will have been invalidates
			for (int i = 0; i < m_DataItems.Length; i++)
			{
				m_DataItems[i].labelIndex = GetClassId(m_DataItems[i].label);
			}
		}

	}

	// relabel a given posture
	public void LabelPosture(string label, int postureId)
	{
		if(postureId >= 0 && postureId < m_DataItems.Length)
		{
			m_DataItems[postureId].label = label;

		}
	}
	 
	// relabel a given posture to the current selected class
	public void LabelPosture(int postureId)
	{
		LabelPosture(currentClassName, postureId);
	}

	// numbe of postures
	public int GetNumPostures()
	{
		return m_DataItems.Length;
	}

	// get the class label of a posture
	public string GetPostureLabel(int i)
	{
		return m_DataItems[i].label;
	}
	
	// get the animation clip that a posture comes from
	public string GetPostureClip(int i)
	{
		return m_DataItems[i].clip;
	}
	
	// get time in the animation clip that the posture appears
	public float GetPostureTime(int i)
	{
		return m_DataItems[i].time;
	}

	// get an animation curve from a clip
	AnimationClipCurveData getCurve(AnimationClip clip, string pathName)
	{
		AnimationClipCurveData [] curveData = AnimationUtility.GetAllCurves(clip);
		foreach (AnimationClipCurveData thiscurve in curveData)
		{
			if(thiscurve.path == pathName)
			{
				return thiscurve;
			}
		}
		return null;
	}

	// get the rotation value of a particular joint (given by pathName) 
	// at time t
	Quaternion getRotationValue(AnimationClip clip, string pathName, float t)
	{
		// this is the quaternion we use to represent the rotation. 
		// set it to a zero rotation and we will then add individual 
		// components to it
		var q = Quaternion.identity;
	
		AnimationClipCurveData [] curveData = AnimationUtility.GetAllCurves(clip);
		foreach (AnimationClipCurveData thiscurve in curveData)
		{
			if(thiscurve.path == pathName)
			{
				if(thiscurve.propertyName == "m_LocalRotation.x")
				{
					q.x = thiscurve.curve.Evaluate(t);
				}
				if(thiscurve.propertyName == "m_LocalRotation.y")
				{
					q.y = thiscurve.curve.Evaluate(t);
				}
				if(thiscurve.propertyName == "m_LocalRotation.z")
				{
					q.z = thiscurve.curve.Evaluate(t);
				}
				if(thiscurve.propertyName == "m_LocalRotation.w")
				{
					q.w = thiscurve.curve.Evaluate(t);
				}
			}
		}
		return q;
	}

	// turn the angle into radians between
	// pi and -pi
	float processAngle(float angle)
	{
		if(angle > 180)
			angle = 360-angle;
		return Mathf.Deg2Rad*angle;
	}

	// turn a pose in an animation into an array of numbers that 
	// can be used by the classification algorithm
	// clipName is the name of the animation clip
	// time is the time at which the pose appears
	float [] GetDataInstanceFromAnimation(string clipName, float time)
	{
		// get the animation component
		Animation anim = gameObject.animation;
		if(!anim)
			Debug.Log("anim is null");

		// three features per joint (x,y,z)
		// currently velocity features aren't implemented
		int numFeatures = featureJoints.Length*3;
		//if(useVelocityFeatures)
		//	numFeatures = featureJoints.Length*6;
		float [] data = new float[numFeatures];

		// check that the clip exists
		if(!anim[clipName])
			return null;
		AnimationClip clip = anim[clipName].clip;
			
		// loop through all the features
		// getting hold of the value
		// j is our position in the array
		int j = 0;
		foreach (Transform feature in featureJoints)
		{
			// get the rotation value and turn it into x,y,z 
			// components (Euler Angles)
			Quaternion q = getRotationValue(clip, feature.gameObject.name, time);
			Vector3 eulers = q.eulerAngles;
			data[j] = processAngle(eulers.x);
			j++;
			data[j] = processAngle(eulers.y);
			j++;
			data[j] = processAngle(eulers.z);
			j++;
			// not currently using velocity features
			/*
			if(useVelocityFeatures)
			{
				Quaternion dq = Quaternion.Inverse(q)*getRotationValue(clip, feature.gameObject.name, time+0.3f);
				Vector3 dEulers = dq.eulerAngles;
				data[j] = processAngle(dEulers.x)/0.3f;
				j++;
				data[j] = processAngle(dEulers.y)/0.3f;
				j++;
				data[j] = processAngle(dEulers.z)/0.3f;
				j++;
			}
			*/
		}
		return data;
	}

	// turn the current pose into into an array of numbers that 
	// can be used by the classification algorithm
	float [] GetDataInstanceLive()
	{
		// three features per joint (x,y,z)
		// currently velocity features aren't implemented
		int numFeatures = featureJoints.Length*3;
		//if(useVelocityFeatures)
		//	numFeatures = featureJoints.Length*3;
		float [] data = new float[numFeatures];
		
			
		// loop through all the features
		// getting hold of the value
		// j is our position in the array
		int j = 0;
		foreach (Transform feature in featureJoints)
		{
			// get the rotation value and turn it into x,y,z 
			// components (Euler Angles)
			Quaternion q = feature.localRotation;
			Vector3 eulers = q.eulerAngles;
			data[j] = processAngle(eulers.x);
			j++;
			data[j] = processAngle(eulers.y);
			j++;
			data[j] = processAngle(eulers.z);
			j++;
			//if(useVelocityFeatures)
			//{
			//	throw new System.Exception("velocity features in real time have not been implemented");
			//}
		}
		return data;
	}

	// add a new posture to the data set
	// parameters are:
	// label - the class label to attach to the posture
	// clip - the animation clip containing the posture
	// time - the time at which it appears
	// tex - the texture to use for displaying the posture
	// selectedTex - the texture to use for displaying the posture when selected
	public int AddPosture(string label, string clip, float time, Texture2D tex, Texture2D selectedTex)
	{
		// turn the posture into animation data
		float [] data = GetDataInstanceFromAnimation(clip, time);

		// get the label ID of the posture
		// (checking for labels that don't exist)
		int labelId = -1;
		if(label == "")
		{
			labelId = -1;
		}
		else
		{
			labelId = GetClassId(label);
			if(labelId < 0)
				return -1;
		}

		// resize the DataItems array, adding a new item at the end
		MotionDataItem [] newDataItems = new MotionDataItem[m_DataItems.Length+1];
		for (int i = 0; i < m_DataItems.Length; i++)
		{
			newDataItems[i] = m_DataItems[i];
		}
		m_DataItems = newDataItems;
		m_DataItems[m_DataItems.Length-1]  = new MotionDataItem{ label = label, clip = clip, time = time, labelIndex = labelId, data = data, lastProbability = 0, tex=tex, selectedTex = selectedTex};

		// return the id of the posture
		int id = m_DataItems.Length-1;
		return id;
	}

	// conventiece overloaded version of AddPosture, which labels it with
	// the current class
	public int AddPosture(string clip, float time, Texture2D tex, Texture2D selectedTex)
	{
		return AddPosture(currentClassName, clip, time, tex, selectedTex);
	}

	// removes a posture
	public void RemovePosture(int id)
	{
		if(id >= 0 && id < m_DataItems.Length)
		{
			// copy all the later labels down in the array
			for (int i = id; i < m_DataItems.Length-1; i++)
			{
				m_DataItems[i] = m_DataItems[i+1];
			}
			
			// do some array switching, because unity doesn't 
			// play well with more complex types
			Array.Resize(ref m_DataItems, m_DataItems.Length-1);	
		}
	}

	// delete all postures
	public void ClearDataSet()
	{
		m_DataItems = new MotionDataItem[0];
	}

	// get the probability of the pose given by data based on the 
	// gaussian associated with a single data item
	public float getProbability(MotionDataItem gaussian, float [] data)
	{
		// calculation for the gaussian distribution
		float coef = (float)(1.0/Math.Pow(2*Math.PI, gaussian.data.Length/2.0));
		coef = (float) (coef * 1.0f/(Math.Pow(defaultSigma, gaussian.data.Length/2.0f)));
	
		float product = 0.0f;
		for (int i = 0; i < data.Length; i++)
		{
			product += (data[i]-gaussian.data[i]) * (data[i]-gaussian.data[i])/defaultSigma;
		}
		float p = (float)(coef*Math.Exp(-0.5f*product));
		
		if (Double.IsInfinity(p) || Double.IsNaN(p))
			throw new Exception("Calculated probability is NaN");

		// save the last probability 
		// (used for scaling the thumbnails)
		gaussian.lastProbability = p;
		return p;
	}

	// get the probabilites of the pose given by data
	// given all of the class names
	public float [] getProbabilities(float [] data)
	{
		// start with zero probabilities
		float [] probs = new float[classLabelArray.Length];
		// the number of data items used for each class
		// (used to normalise the results)
		float [] counts = new float[classLabelArray.Length];
		// keep track of the maximum probability
		maxProbability = 0.0f;

		// for each data item calculated the probability
		// and add it to the probabilities for that class
		for(int i  = 0; i < m_DataItems.Length; i++)
		{
			float p = getProbability(m_DataItems[i], data);
			if(p > maxProbability)
				maxProbability = p;
			if(m_DataItems[i].labelIndex >= 0)
			{
				probs[m_DataItems[i].labelIndex] += p;
				counts[m_DataItems[i].labelIndex] += 1.0f;
			}
		}
		// normalise by the number of postures in the class
		for (int i  = 0; i < probs.Length; i++) 
		{
			probs[i] /= counts[i];
		}
		return probs;
	}

	// classify the pose given by data
	// chooses the class with the highest 
	// probabilty
	public string Classify(float [] data)
	{
		float [] probs = getProbabilities(data);
		int maxId = -1;
		float maxProb = 0.0f;
		for (int i = 0; i < probs.Length; i++)
		{
			if(probs[i] > maxProb)
			{
				maxId = i;
				maxProb = probs[i];
			}
		}
		if(maxId < 0 || maxProb <= 0)
			return "nothing";
		else 
			return classLabelArray[maxId];
	}

#warning get velocities into the classification
	// classify the current posture 
	// and send a message to any gameObjects that 
	// are listening
	public string Classify()
	{
		float [] data = GetDataInstanceLive();

		string new_classification = Classify(data);
		if (new_classification != classification) 
		{
			classification = new_classification;
			if (Application.isPlaying){
			
				if (new_classification != "nothing")
				{
					for (int i = 0; i < listeners.Length; i++)
					{
						listeners[i].SendMessage("OnNewPostureRecognised", new_classification);
					}
				}
				
			} 
		}
		return classification;
	}
	
	public double GetLastProbability(int i)
	{
		return m_DataItems[i].lastProbability;
	}
	
	public double GetMaxProbability(int i)
	{
		return maxProbability;
	}

	// sets up the class 
	// (creates the classLabelArray if 
	// it doesn't exist)
	public void Init(){
		if(classLabelArray == null)
			classLabelArray= new string[0];
	}

	// called when it starts playing
	// starts of the animation recording 
	// if needed
	void Awake () {
		Init ();
		classification = "";
		if(recordData && animationRecorder)
		{
			animationRecorder.enabled = true;
		}
	}

	// turn off recording when we stop playing
	void OnApplicationQuit()
	{
		if(recordData && animationRecorder)
		{
			animationRecorder.enabled = false;
		}
	}
	
	// Update is called once per frame
	// we classify the new posture
	void Update () {
		Classify ();
	}
	

	void OnEnable()
	{
		currentClassName = "";
	}
	
	
	void OnDisable()
	{
		currentClassName = "";
	}  

	// gets the color associated with a class
	public Color GetClassColour(int classId, bool selected)
	{
		if (classId < 0) 
		{
			return Color.white;
		}

		if (selected) 
		{
			return classColours [classId % classColours.Length];
		}
		else 
		{
			Color selectedCol =  classColours [classId % classColours.Length];
			return new Color(selectedCol.r/2.0f, selectedCol.g/2.0f, selectedCol.b/2.0f, 1.0f);
		}
	}
}

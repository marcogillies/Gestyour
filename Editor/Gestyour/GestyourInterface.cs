using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.IO;

public class GestyourInterface : EditorWindow {

	float currentFrame = 0.0f;
	int currentGO = 0;
	int currentAnim = 0;

	private bool isPlaying = false;

	private GUIStyle [] selectedStyles = {};
	private GUIStyle [] unselectedStyles = {};

	// Add menu item named "My Window" to the Window menu
	[MenuItem("Window/Gestyour")]
	public static void ShowWindow()
	{
		//Show existing window instance. If one doesn't exist, make one.
		Debug.Log ("Showing the Posture Recogniser");
		GestyourInterface e = EditorWindow.GetWindow(typeof(GestyourInterface)) as GestyourInterface;
		Debug.Log (e);
		e.Show ();
		e.isPlaying = false;
	}

	// creates a button style for the class buttons from a colour value
	public GUIStyle createGUIStyle(Color col)
	{
		// create a small texture full of the colour
		GUIStyle style = new GUIStyle ();
		Texture2D tex = new Texture2D( 16, 16, TextureFormat.RGB24, false );
		for (int i = 0; i < tex.width; i++) {
			for (int j = 0; j < tex.height; j++) {
				tex.SetPixel (i, j, col);
			}
		}
		tex.Apply ();

		// set the styles parameters, 
		// use the texture for everyting
		style.normal.background = tex;
		style.normal.textColor = Color.white;
		style.hover.background = tex;
		style.hover.textColor = Color.white;
		style.active.background = tex;
		style.active.textColor = Color.white;
		style.focused.background = tex;
		style.focused.textColor = Color.white;
		style.onNormal.background = tex;
		style.onNormal.textColor = Color.white;
		style.onHover.background = tex;
		style.onHover.textColor = Color.white;
		style.onActive.background = tex;
		style.onActive.textColor = Color.white;
		style.onFocused.background = tex;
		style.onFocused.textColor = Color.white;

		style.border.left = 6;
		style.border.right = 6;
		style.border.top = 0;
		style.border.bottom = 4;

		style.margin.left = 4;
		style.margin.right = 4;
		style.margin.top = 0;
		style.margin.bottom = 7;
		
		style.padding.left = 6;
		style.padding.right = 6;
		style.padding.top = 2;
		style.padding.bottom = 3;

		return style;
	}

	// get a button style for a particular class
	// the styles are cached in a array so they are
	// only generated once
	GUIStyle GetButtonStyle(int id, bool selected, AIController aiController)
	{
		//Debug.Log (id);
		// this will be a reference to either the 
		// selected styles array or the unselected styles array
		GUIStyle [] styles;
		// choose which styles to use depending on the selected variable
		// if the styles array doesn't exist, create it
		if (selected) 
		{
			if(selectedStyles == null) selectedStyles = new GUIStyle[0];
			styles = selectedStyles;
		}
		else 
		{
			if(unselectedStyles == null) unselectedStyles = new GUIStyle[0];
			styles = unselectedStyles;
		}
		// if the class id isn't in the styles array, create it
		// and add it to the array
		if (id >= styles.Length) {
			Array.Resize(ref styles, id+1);
			for(int i = 0; i < styles.Length; i++)
			{
				styles[i] = createGUIStyle(aiController.GetClassColour(i, selected));
			}
		}
		return styles [id];
	}

	// creates a button bar for the class buttons
	public int ClassToolbar(Rect r, int selection, String[] labels, AIController aiController)
	{
		//loop through all of the labels, drawing a button for each
		for( int i = 0; i < labels.Length; i++ )
		{
			if( GUILayout.Button( labels[ i ], GetButtonStyle(i, ( selection == i ),aiController), GUILayout.ExpandWidth(false)))
			{
				// if the button has been pressed, select the class
				if( selection != i )
				{
					selection = i;
				}
				else
				{
					selection = -1;
				}
			}
		}

		return selection;
	}

	// a helper function to draw a line in a texture
	void drawLine(Texture2D tex, Color col, int x1, int y1, int x2, int y2)
	{
		int dx = x2-x1;
		int dy = y2-y1;

		if (Math.Abs(dx) > Math.Abs(dy)) {

			if(x1 > x2){
				int tmp = x2;
				x2 = x1;
				x1 = tmp;
				tmp = y2;
				y2 = y1;
				y1 = tmp;
			}

			float y = y1;
			float diff = ((float)dy) / dx;
			for (int x = x1; x <= x2; x++) {
					y += diff;
					tex.SetPixel (x, (int)Math.Round (y), col);
			}
		} else {
			if(y1 > y2){
				int tmp = x2;
				x2 = x1;
				x1 = tmp;
				tmp = y2;
				y2 = y1;
				y1 = tmp;
			}

			float x = x1;
			float diff = ((float)dx) / dy;
			for (int y = y1; y <= y2; y++) {
				x += diff;
				tex.SetPixel ((int)Math.Floor (x), y, col);
				tex.SetPixel ((int)Math.Ceiling (x), y, col);
			}
		}
	}

	// creates a textures of a particular pose
	Texture2D createTexture(AIController aiController, Color col)
	{
		// draw the backround, so it looks like the editor
		Texture2D tex = new Texture2D( 80, 120, TextureFormat.RGB24, false );
		Color editorBack = new Color (194.0f/255.0f, 194.0f/255.0f, 194.0f/255.0f);
		for (int i = 0; i < tex.width; i++) {
			for (int j = 0; j < tex.height; j++) {
				tex.SetPixel (i, j, editorBack);
			}
		}

		// StickFigureRenderer is what draws the pose
		// there are several for different body parts
		StickFigureRenderer [] stickRenderers = aiController.GetComponentsInChildren<StickFigureRenderer>();

		// find the minimium and maximum x and y positions
		// of the joints of the pose (for scaling the figure)
		float minX = 100000, maxX = -100000;
		float minY = 100000, maxY = -100000;
		for (int i = 0; i < stickRenderers.Length; i++) {
			for (int j = 0; j < stickRenderers[i].joints.Length; j++) {
				Vector3 pos = stickRenderers[i].joints[j].position;
				if(pos.x < minX){
					minX = pos.x;
				}
				if(pos.x > maxX){
					maxX = pos.x;
				}
				if(pos.y < minY){
					minY = pos.y;
				}
				if(pos.y > maxY){
					maxY = pos.y;
				}
			}
		}

		float xsize = maxX - minX;
		float ysize = maxY - minY;

		// go through each of the renderers and then through
		// each joint of that renderer
		// draw a line between each pair of joints
		for (int i = 0; i < stickRenderers.Length; i++) 
		{
			    for (int j = 0; j < stickRenderers[i].joints.Length-1; j++) 
				{
					// get the positions of the start and end of a bone
					float x1 = stickRenderers[i].joints[j].position.x;
					float x2 = stickRenderers[i].joints[j+1].position.x;
					float y1 = stickRenderers[i].joints[j].position.y;
					float y2 = stickRenderers[i].joints[j+1].position.y;

					// scale them
					x1 = (tex.height - 10)*(x1-minX - xsize/2)/ysize + tex.width/2;
					x2 = (tex.height - 10)*(x2-minX - xsize/2)/ysize + tex.width/2;
					y1 = (tex.height - 10)*(y1-minY)/ysize + 5;
					y2 = (tex.height - 10)*(y2-minY)/ysize + 5;

					// draw a line between them 
					drawLine (tex, col, tex.width-(int)x1,(int)y1,tex.width-(int)x2,(int)y2);
				}
		}

		tex.Apply ();
		return tex;
	}

	// make sure that the window updates in play mode to reflect 
	// the current pose returned by the kinect
	void Update () {
		if (EditorApplication.isPlaying && !EditorApplication.isPaused){
			Repaint();
		} 
		if (EditorApplication.isPlaying)
		{
			isPlaying = true;
		}

//		if(!EditorApplication.isPlaying && isPlaying)
//		{
//			isPlaying = false;
//			Debug.Log ("stopped playing");
//
//			//string sAssetFolderPath = AssetDatabase.GetAssetPath(objSelected);
//			
//			// Construct the system path of the asset folder 
//			string sDataPath  = Application.dataPath;
//			Debug.Log (sDataPath);
//
//			string sFolderPath = sDataPath.Substring(0 ,sDataPath.Length-6)+"Assets/Gestyour/RecordedAnimations/";         
//			
//			// get the system file paths of all the files in the asset folder
//			string[] aFilePaths = Directory.GetFiles(sFolderPath);
//			
//			// enumerate through the list of files loading the assets they represent and getting their type
//			
//			foreach (string sFilePath in aFilePaths) {
//				string sAssetPath = sFilePath.Substring(sDataPath.Length-6);
//				//Debug.Log (sAssetPath);
//				Debug.Log (sAssetPath.Substring(sAssetPath.Length-5));
//				Debug.Log (Path.GetExtension(sAssetPath));
//				if(Path.GetExtension(sAssetPath) == ".anim")
//					Debug.Log(Path.GetFileNameWithoutExtension(sAssetPath));
//				
//				//Object objAsset =  AssetDatabase.LoadAssetAtPath(sAssetPath,typeof(Object));
//				
//				//Debug.Log(objAsset.GetType().Name);
//			}
//			/*
//			UnityEngine.Object [] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Gestyour/RecordedAnimations/RecordedAnimation_04_22_14_23_04_2014.anim");
//			Debug.Log (assets.Length);
//			for (int i = 0; i < assets.Length; i++)
//			{
//				Debug.Log (assets[i].name);
//			}
//			*/
//		}
	}

	// this draws the user interface
	void OnGUI()
	{
		//Debug.Log ("creating gui");

		// all of the AIController components in the scene
		AIController[] aicontrollers = FindObjectsOfType(typeof(AIController)) as AIController[];
		if(aicontrollers.Length > 0)
		{
			/*
			 * 	selecting an animation controller and an animation
			 */

			// chooser for the aiController objects
			string[] goNames = new string[aicontrollers.Length];
			for (int i = 0; i < aicontrollers.Length; i++)
			{
				goNames[i] = aicontrollers[i].gameObject.name;
			}
			EditorGUILayout.BeginHorizontal();
			int prevGO = currentGO;
			currentGO = EditorGUILayout.Popup(currentGO, goNames,GUILayout.Width(150));
			AIController aiController = aicontrollers[currentGO];
			if(prevGO != currentGO)
			{
				aiController.Init();
			}

			// chooser for the animation attached to the corresponding objects
			Animation anim = aicontrollers[currentGO].gameObject.animation;

			string sDataPath  = Application.dataPath;
			//Debug.Log (sDataPath);
			
			string sFolderPath = sDataPath.Substring(0 ,sDataPath.Length-6)+"Assets/Gestyour/RecordedAnimations/";         
			
			// get the system file paths of all the files in the asset folder
			string[] aFilePaths = Directory.GetFiles(sFolderPath);
			
			// enumerate through the list of files loading the assets they represent and getting their type
			
			foreach (string sFilePath in aFilePaths) {
				//Debug.Log (sAssetPath);
				//Debug.Log (sAssetPath.Substring(sAssetPath.Length-5));
				//Debug.Log (Path.GetExtension(sAssetPath));
				if(Path.GetExtension(sFilePath) == ".anim")
				{
					//Debug.Log(Path.GetFileNameWithoutExtension(sAssetPath));
					if(anim[Path.GetFileNameWithoutExtension(sFilePath)] == null)
					{
						string sAssetPath = sFilePath.Substring(sDataPath.Length-6);
						//Debug.Log ("not found");
						AnimationClip animAsset =  AssetDatabase.LoadAssetAtPath(sAssetPath,typeof(AnimationClip)) as AnimationClip;
						
						//Debug.Log (animAsset);
						anim.AddClip(animAsset,Path.GetFileNameWithoutExtension(sAssetPath));
					}
				}


				//Debug.Log(objAsset.GetType().Name);
			}

			string [] animNames;
			if(anim && !EditorApplication.isPlaying)
			{
				int animCount = 0;
				foreach (AnimationState state in anim) {
					animCount++;
				}
				if(animCount > 0)
				{
					animNames = new string[animCount];
					int i = 0;
					foreach (AnimationState state in anim) {
						//Debug.Log (state.name);
						animNames[i] = state.name;
						i++;
					}
					int prevAnim = currentAnim;
					currentAnim = EditorGUILayout.Popup(currentAnim, animNames,GUILayout.Width(250));
					
					// play the selected animation but set the speed to zero
					// this loads a pose of the animation with out actually animating
					// (we will scrub through the animation manually with a slider)
					if(anim && (!anim.isPlaying || !anim.IsPlaying(animNames[currentAnim])))
					{
						anim.Play (animNames[currentAnim]);
						anim[animNames[currentAnim]].speed = 0;
					}
				}
				else
				{
					animNames = new string[0];
				}
			}
			else
			{
				animNames = new string[0];
			}

			bool deleteClip = false;
			if(anim && animNames.Length > 0 && !EditorApplication.isPlaying)
			{
				if(GUILayout.Button("delete animation", EditorStyles.miniButton, GUILayout.Width(120)))
				{
					deleteClip = true;
					//if(currentAnim >= 0 && currentAnim < animNames.Length)
					//{
					//	Debug.Log(AssetDatabase.GetAssetPath(anim[animNames[currentAnim]].clip));
					//	//AssetDatabase.CreateFolder ("Assets/Gestyour/RecordedAnimations/", "Trash");
					//	AnimationClip clip = anim[animNames[currentAnim]].clip;
					//	anim.RemoveClip(animNames[currentAnim]);
					//	Debug.Log (AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(clip), "Assets/Gestyour/Trash/" + animNames[currentAnim] + ".anim"));
					//	currentAnim = 0;
					//}
				}
			}

			EditorGUILayout.EndHorizontal();

			/*
			 *   the class labels horizontal section  
			 */
			EditorGUILayout.BeginHorizontal();
			// display the current classification
			EditorGUILayout.LabelField ("classification", GUILayout.Width(80));
			int classId = aiController.GetClassId(aiController.classification);

			// a text area for setting a new class name
			if(classId >= 0)
				EditorGUILayout.LabelField (aiController.classification, GetButtonStyle(classId,true,aiController), GUILayout.Width(80));
			else
				EditorGUILayout.LabelField ("None", GUILayout.Width(80));
			aiController.newClassName = EditorGUILayout.TextField (aiController.newClassName, GUILayout.Width(80));  

			// buttons for adding and removing a class name
			if(GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(40)))
			{
				aiController.AddClass(aiController.newClassName);
				aiController.newClassName = "";
			}
			if(GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(40)))
			{
				aiController.RemoveClass(aiController.classLabelArray[aiController.currentClassId]);
				aiController.newClassName = "";
			}

			// generate a button bar with a coloured button for each class
			int toolbarSize = aiController.classLabelArray.Length * 100;
			int oldClass = aiController.currentClassId;
			aiController.currentClassId = ClassToolbar (new Rect (190, 150, toolbarSize, 100), aiController.currentClassId, aiController.classLabelArray, aiController);
			
			EditorGUILayout.EndHorizontal();


			/*
			 * 	Control the current animation (if there is one) 
			 */
			if(anim && animNames.Length > 0 && !EditorApplication.isPlaying)
			{
				// this is the object that controls playback of a particular
				// animation clip
				AnimationState animState = anim[animNames[currentAnim]];
				EditorGUILayout.BeginHorizontal();

				// a slider for scrubbing through the current animation
				currentFrame = EditorGUILayout.Slider ("", currentFrame, 0, animState.length);

				// check if we have changed the frame, if so 
				// resample the animation
				if(Math.Abs(currentFrame - animState.time) > 0.001)
				{
					animState.time = currentFrame;
					anim.Sample();
					EditorUtility.SetDirty(anim.gameObject);
					aiController.Classify();
				}
				
				EditorGUILayout.EndHorizontal();
			}

			/*
			 *  Thumbnails of each of the poses in the data set
			 */
			GUIStyle thumbStyle = new GUIStyle();
			// a hideously complex for loop. 
			// i steps through the data items in 10s because we are drawing 
			// 10 to a row (should make this even more complex by having the row
			// size depend on the window size)
			// the i == 0 bit is a hack to make it always draw the add and remove buttons
			for(int i = 0; i == 0 || i < aiController.m_DataItems.Length; i+=10)
			{
				EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
				// before drawing the first thumbnal we create add and remove
				// buttons for poses
				if( i == 0)
				{
					EditorGUILayout.BeginVertical(GUILayout.Width(20));
					if(GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
					{
						// we can only add if we have a currently loaded animation
						if(anim)
						{
							PrefabUtility.DisconnectPrefabInstance(aiController.gameObject);
							AnimationState animState = anim[animNames[currentAnim]];
							// create the thumbnail buttons
							Color thumbCol = aiController.GetClassColour(aiController.currentClassId, true);
							Texture2D selectedTex = createTexture(aiController, thumbCol);
							thumbCol = aiController.GetClassColour(aiController.currentClassId, false);
							Texture2D tex = createTexture(aiController, thumbCol);

							// add the pose
							aiController.AddPosture(animState.clip.name, currentFrame, tex, selectedTex);

							// force a redraw
							EditorUtility.SetDirty(anim.gameObject);
							// reclassify the current pose to include the new data item
							aiController.Classify();
						}
					}
					// remove the posture
					if(GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(20)))
					{
						aiController.RemovePosture(aiController.currentPosture);
					}
					EditorGUILayout.EndVertical();
					GUILayout.Space (10);
				}
				else
				{
					GUILayout.Space (40);
				}
				// this loop draws a row of 10 thumbnails
				// again it is complex because we have to check
				// whether we are less than the 10 items per row
				// but also if we are less than the total 
				// number of data items
				for(int j = i; j - i < 10 && j < aiController.m_DataItems.Length; j++)
				{
					// get the probabilities calculated in the last classification
					// (used for scaling the thumbnail)
					double prob = aiController.GetLastProbability(j);
					double maxProb = aiController.GetMaxProbability(j);

					// the size of the thumbnail texture
					float w = aiController.m_DataItems[ j ].tex.width;
					float h = aiController.m_DataItems[ j ].tex.width;

					// scale the button based on the probabilities
					float buttonScale = 0.5f;
					if(maxProb > 0 && prob >= 0)
					{
						buttonScale = (float)((prob*0.5f)/maxProb) + 0.5f;
					}
					w *= buttonScale;
					h *= buttonScale;

					// get the appropriate texture (it will be 
					// different depending on whether it is 
					// selected)
					Texture2D tex;
					if(aiController.currentPosture == j)
					{
						tex = aiController.m_DataItems[ j ].selectedTex;
					}
					else
					{
						tex = aiController.m_DataItems[ j ].tex;
					}
					// draw the button
					// if it is clicked, select the texture
					if( GUILayout.Button(tex, thumbStyle, GUILayout.Width(w), GUILayout.Height(h)))
					{
						aiController.currentPosture = j;
					}
					// add some padding so that all the buttons are in a neat
					// row even as they change size
					GUILayout.Space (aiController.m_DataItems[ j ].tex.width - w);
				}
				EditorGUILayout.EndHorizontal();
			}

			if(deleteClip && currentAnim >= 0 && currentAnim < animNames.Length)
			{
				Debug.Log(AssetDatabase.GetAssetPath(anim[animNames[currentAnim]].clip));
				AssetDatabase.CreateFolder ("Assets/Gestyour/RecordedAnimations/", "Trash");
				AnimationClip clip = anim[animNames[currentAnim]].clip;
				anim.RemoveClip(animNames[currentAnim]);
				if(AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(clip), "Assets/Gestyour/DeletedAnimations/" + animNames[currentAnim] + ".anim") != "")
				{
					Debug.Log ("trying to create trash");
					Debug.Log (AssetDatabase.CreateFolder ("Assets/Gestyour/RecordedAnimations/", "DeletedAnimations"));
					Debug.Log (AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(clip), "Assets/Gestyour/DeletedAnimations/" + animNames[currentAnim] + ".anim"));
				}
				currentAnim = 0;
			}
		}

	}
}

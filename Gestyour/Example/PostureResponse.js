#pragma strict

var last_classification : String = "";
var guiStyle : GUIStyle;

function Start () {

}

function Update () {

}

function OnNewPostureRecognised(classification){
	last_classification = classification;
	if(classification == "up")
	{
		transform.position.y += 0.1;
	}
	if(classification == "left")
	{
		transform.position.x -= 0.1;
	}
	if(classification == "right")
	{
		transform.position.x += 0.1;
	}
}

function OnGUI(){
	//GUILayout.Label(last_classification);
	GUI.Label(Rect(30, 100, 100, 50), last_classification, guiStyle);
}
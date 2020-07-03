using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
[CustomEditor(typeof(SpriteMaker))]
public class SpriteMakerEditor : Editor {
	private ReorderableList clipList;
	private static int index=0; //valeur de l'index choisi pour le modèle dans la liste filtrée
	private static GameObject m; // variable temporaire pour le modèle choisi
	private static float sca=1.0f; //valeur du scale
	private static GameObject no; // variable contenant un GameObject vide "none" pour ne rien avoir d'affiché à l'écran
	
	private void OnEnable() {
	
		//Initialisation de la liste de clips d'animation
		clipList = new ReorderableList(serializedObject, serializedObject.FindProperty("clips"), true, true, true, true);
		clipList.drawElementCallback =  (Rect rect, int index, bool isActive, bool isFocused) => {
			var element = clipList.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;
			rect.height=EditorGUIUtility.singleLineHeight;
			EditorGUI.PropertyField(rect, element, GUIContent.none);
		};
		clipList.drawHeaderCallback = (Rect rect) => {  
			EditorGUI.LabelField(rect, "Animation Clips");
		};
		
	}





	public override void OnInspectorGUI ()
	{


		GUILayout.Space (5.0f);
		serializedObject.Update ();

		// 
		///
		////
		/////Model Choice
		SerializedProperty model = serializedObject.FindProperty ("model"); //Création de la propriété sérialisée "model" pour mettre en relation le modèle entre "SpriteMaker" et "SpriteMakerEditor"
		GameObject[] list=Resources.FindObjectsOfTypeAll<GameObject>(); //Création d'une liste de tous les GameObjects
		List<GameObject> oklist=new List<GameObject>(); //Création d'une liste que l'on va filtrer à partir de la liste précédente
		
		 //On filtre la liste pour n'avoir que les objets sans parents et avec un animateur
		 if(GameObject.Find("none")==null) no = new GameObject("none");
		 oklist.Add(GameObject.Find("none"));
		for(int i=0; i<list.Length; i++)
		{
			if((list[i].GetComponent<Animator>()!=null)&&(list[i].transform.parent==null)&&(list[i].name!="Sprite Maker")&&(list[i].name!="Light 1")&&(list[i].name!="Light 2")&&(list[i].name!="SceneLight")&&(list[i].name!="SceneCamera")&&(list[i].name!="InternalIdentityTransform")&&(list[i].name!="Light")&&(list[i].name!="HandlesGO")&&(list[i].name!="GAME MODEL")&&(list[i].name!="DefaultGeneric"))
			{
				oklist.Add(list[i]);
			}
		}
		int size=oklist.Count;
		
		string[] names=new string[size]; //Création d'une liste contenant les noms des objets de la liste filtrée
		
		for(int j=0; j<oklist.Count; j++)
		{
			names[j]=(oklist[j].ToString());
		}
		

		EditorGUI.BeginChangeCheck ();
		
		index=EditorGUILayout.Popup("Select Model",index,names);//Création d'un choix déroulant dans l'éditeur à partir de la liste de nom, l'index du nom choisi par l'utilisateur sera renvoyé dans la variable index
		if(index!=0) EditorPrefs.SetInt("Temp", index); //Sauvegarde de index dans la mémoire de l'application
		//////
		//Scale
		SerializedProperty scale = serializedObject.FindProperty ("scale"); //Création de la propriété sérialisée "scale" pour mettre en relation le scale (échelle) entre "SpriteMaker" et "SpriteMakerEditor"
		sca = EditorGUILayout.Slider("Scale",scale.floatValue, 0,3); //Création d'un slider dans l'éditeur qui changera la variable de "sca"
		scale.floatValue=sca; //Affectation de la valeur de "sca" à la variable "scale" dans "SpriteMaker"
		
		////Modify Model and Scale values if change happens
		if (EditorGUI.EndChangeCheck ()) 
		{
			m=oklist[index]; //la variable "m" prend la valeur de l'objet choisi dans le choix déroulant
			model.objectReferenceValue=m; //Affectation du GameObject de "m" à la variable "model" dans "SpriteMaker"
			
			////Faire apparaitre le modèle choisi sur la scène et disparaitre tous les autres
			for(int j=0; j<oklist.Count; j++)
			{
				if(oklist[j]!=GameObject.Find("none"))
				{
					if(oklist[j]!=m) oklist[j].SetActive(false);
					else if(oklist[j]==m) oklist[j].SetActive(true);
				}
			}
			
			////Si l'objet n'a pas d'avatar, prendre son enfant comme modèle
			if(m!=GameObject.Find("none"))
			{
				if(m.GetComponent<Animator>().avatar==null)
				{
					m=m.transform.GetChild(0).gameObject;
				}
			}
			
			m.transform.localScale = new Vector3(sca,sca,sca); //Permet de modifier en temps réel l'echelle du modèle quand on bouge le slider de "sca"
		}
		if(EditorApplication.isPlaying) index=EditorPrefs.GetInt("Temp", -1); //Remise de l'index à la position sauvegardée par l'application
		///
		///// LEGACY
		DrawProperty ("legacy", true); //Ajout d'une case à cocher "legacy" pour les modèles ayant des vieilles animations
		/////
		
		/////
		//Toggle Selected Model
		
		
		////
		//Directions

		//Création de cases à cocher pour chaque direction souhaitée à la capture
		if (DrawProperty ("selectDirection").boolValue) {
			DrawProperty ("south", true);
			DrawProperty ("southWest", true);
			DrawProperty ("west", true);
			DrawProperty ("northWest", true);
			DrawProperty ("north", true);
			DrawProperty ("northEast", true);
			DrawProperty ("east", true);
			DrawProperty ("southEast", true);
		}
		

		
		

		////////
		//Lights
		GameObject li=GameObject.Find("Light 2"); //Création de l'objet "li" qui prend la lumiére "Light 2" située dans la scène
		Light lt=li.GetComponent<Light>();// Ajout du component Light à l'object pour pouvoir effectuer des modifications sur l'intensité et le type de lumière.
		lt.intensity=EditorGUILayout.Slider("Light 2 Intensity",lt.intensity, 0, 50); //Création d'un slider dans l'éditeur qui changera la variable de l'intensité de la lumière
		lt.type=(LightType)EditorGUILayout.EnumPopup("Light 2 Type",lt.type);//Création d'un choix déroulant dans l'éditeur pour séléctionner et changer le type de lumière

		////Mise en place du chemin de sauvegarde des images rendues
		SerializedProperty directoryPath = serializedObject.FindProperty ("directoryPath"); 
		if (string.IsNullOrEmpty (directoryPath.stringValue)) {
			EditorGUILayout.HelpBox ("Please select a directory where the sprites or atlas texture will be saved.", MessageType.Error, true);
		} else {
			if(!System.IO.Directory.Exists(directoryPath.stringValue)){
				directoryPath.stringValue=string.Empty;
			}
		}


		if(GUILayout.Button(!string.IsNullOrEmpty(directoryPath.stringValue)?(directoryPath.stringValue.Contains("Assets")?directoryPath.stringValue.Substring(directoryPath.stringValue.IndexOf("Assets")):directoryPath.stringValue):"Select directory","PreDropDown")){
			string path = EditorUtility.OpenFolderPanel("Select directory, where the sprites will be saved.", "", "");
			if(!string.IsNullOrEmpty(path)){
				directoryPath.stringValue=path;
			}
		}


		//
		//
		//
		//
		SerializedProperty framesPerAnim = DrawProperty ("framesPerAnim");
		framesPerAnim.intValue = Mathf.Clamp (framesPerAnim.intValue, 1, int.MaxValue);

		//
		//
		//
		//
		SerializedProperty captureType = serializedObject.FindProperty ("captureType");
		if ((captureType.enumValueIndex == 1 || captureType.enumValueIndex == 2) && !Application.HasProLicense ()) {
			EditorGUILayout.HelpBox("This capture type requires Unity Pro!", MessageType.Error, true);	
		}
		DrawProperty ("captureType");

		if (captureType.enumValueIndex==1 || captureType.enumValueIndex == 2) {
			DrawProperty("resolution");
		}
		if (DrawProperty ("trim").boolValue) {
			DrawProperty ("padding",true);
		}
		if (DrawProperty ("createAtlas").boolValue) {
			DrawProperty("createSingleSprites",true);
			DrawProperty("packingType",true);
			DrawProperty("pivotPosition",true);
		}
		if (clipList.count == 0) {
			DrawProperty("frameRate");		
		}
		GUILayout.Space (3.0f);
		clipList.DoLayoutList();
		serializedObject.ApplyModifiedProperties ();
		bool guiEnabled = GUI.enabled;
		if (string.IsNullOrEmpty (directoryPath.stringValue)) {
			GUI.enabled=false;
		}
		if (GUILayout.Button ("Capture")) {
			EditorApplication.isPlaying=true;	
		}

		GUI.enabled = guiEnabled;
	}
   

	private SerializedProperty DrawProperty(string propertyName){
		return DrawProperty (propertyName, false);
	}

	////Fonction DrawProperty qui sert à afficher facilement des choix correspondant à la propriété dans l'editeur
	private SerializedProperty DrawProperty(string propertyName, bool insert){
		SerializedProperty property = serializedObject.FindProperty (propertyName);
		if (insert) {
			EditorGUI.indentLevel+=1;		
		}
		EditorGUILayout.PropertyField (property);
		if (insert) {
			EditorGUI.indentLevel-=1;		
		}
		return property;
	}
	}
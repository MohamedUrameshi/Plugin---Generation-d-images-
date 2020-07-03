using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpriteMaker : MonoBehaviour {

	public enum CaptureType {
		Screen,
		RenderTexture,
		RenderTextureFast
	}
	
	public enum PackingType{
		Uniform,
		Sliced
	}
	
	public enum PivotPosition{
		Center,
		CenterOfMass,
		TopPixel,
		RightPixel,
		LeftPixel,
		BottomPixel,
	}

	public enum Direction{
		South=1, 
		SouthWest=2,
		West=3,
		NorthWest=4,
		North=5,
		NorthEast=6,
		East=7,
		SouthEast=8
	}

#if UNITY_EDITOR
	[SerializeField]
	private string directoryPath;

	[SerializeField]
	[Tooltip("Choose the number of frames per animation you want.")]
	[ContextMenuItem("Reset","resetFrames")]
	private int framesPerAnim = 10;
	private void resetFrames(){
		framesPerAnim = 10;
	}

	[SerializeField]
	private List<AnimationClip> clips;

	[SerializeField]
	[Tooltip("Click if you want a trim then select the padding.")]
	private bool trim;

	[SerializeField]
	[Tooltip("Choose the padding.")]
	[ContextMenuItem("Reset","resetPadding")]
	private int padding=2;
	private void resetPadding(){
		padding = 2;
	}
	
	[SerializeField]
	[Tooltip("If you click, you will be able to choose the packing type, pivot position and it will allow you to create single sprites.")]
	private bool createAtlas;

	[SerializeField]
	[Tooltip("Select the packing type between Uniform and Sliced.")]
	private PackingType packingType;

	[SerializeField]
	[Tooltip("Click if you want to create only one sprite.")]
	private bool createSingleSprites;

	[SerializeField]
	[Tooltip("Select pivot position you want between Center, Center of Mass, Top Pixel, Right Pixel, Left Pixel and Bottom Pixel.")]
	private PivotPosition pivotPosition;

	[SerializeField]
	private int frameRate=25;

	[SerializeField]
	[Tooltip("Choose the capture type you want.")]
	private CaptureType captureType;

    [SerializeField]
	[Tooltip("Here you can select the model you want to animate.")]
    private GameObject model;

	[SerializeField]
	[Tooltip("Select between 1 and 8 directions you want to animate.")]
	private bool selectDirection;

	[SerializeField]
	[Tooltip("Click if you want to save south direction.")]
	private bool south;

	[SerializeField]
	[Tooltip("Click if you want to save south-west direction.")]
	private bool southWest;

	[SerializeField]
	[Tooltip("Click if you want to save west direction.")]
	private bool west;

	[SerializeField]
	[Tooltip("Click if you want to save north-west direction.")]
	private bool northWest;

	[SerializeField]
	[Tooltip("Click if you want to save north direction.")]
	private bool north;

	[SerializeField]
	[Tooltip("Click if you want to save north-east direction.")]
	private bool northEast;

	[SerializeField]
	[Tooltip("Click if you want to save east direction.")]
	private bool east;

	[SerializeField]
	[Tooltip("Click if you want to save south-east direction.")]
	private bool southEast;
	
	[SerializeField]
	private float scale=1.0f;
	
	[SerializeField]
	private bool legacy = false;
	// Object that needs to be rendered
	private GameObject objectToRender;

	// White background camera
	private Camera whiteCam;

	// Black background camera
	private Camera blackCam;

	// time scale of the rendering used for animation processing
	private float originalTimeScale;

	// Current animation
	private Animation anim;

	// Current clip
	private int currentClipIndex;

	// Current clip name
	private string currentClipName;

	// Current Animation Frame
	private int currentAnimFrame;

	// Current unity Frame that is used as reference for the capture.
	private int unityFrame;

	// Current direction of the model

	private Direction direction = Direction.South;

	private bool canCapture = false;

	// mecanim
	private Animator animator;



	/*
	 *	Start the rendering 
	 *
	 *  Perform initial checks..
	 */
	private void Start () {
		if (string.IsNullOrEmpty (directoryPath) || !System.IO.Directory.Exists(directoryPath)) {
			Debug.LogError("Please setup the directory path in the inspector of sprite maker.");		
			EditorApplication.isPlaying=false;
			return;
		}

		blackCam = GameObject.Find ("Black Camera").GetComponent<Camera>();
		if (blackCam == null) {
			Debug.LogError("Black Cam is null");
			EditorApplication.isPlaying=false;
			return;
		}

		whiteCam = GameObject.Find ("White Camera").GetComponent<Camera>();
		if(whiteCam == null) {
			Debug.LogError("White Cam is null");
			EditorApplication.isPlaying=false;
			return;	
		}

		objectToRender = model;
		if(objectToRender.GetComponent<Animator>().avatar==null)
			objectToRender=objectToRender.transform.GetChild(0).gameObject;

		if(objectToRender == null) {
			Debug.LogError("Object to render is null.  An object with id/name model is required");
			EditorApplication.isPlaying=false;
			return;	
		}

		
		
		
		// Optionnal animator (used only for mecanim animations
		animator = objectToRender.GetComponent<Animator>();

		// Get the original timescale
		originalTimeScale = Time.timeScale;

		// Setup the initial animation
		SetupAnimation();	
	}

	/*
	 *	Main Update loop.
	 */
	private void Update() {
     //Mise à jour de l'échelle du modèle lors de la capture avec la valeur "scale"
    objectToRender.transform.localScale=new Vector3(scale, scale, scale);

		// White Cam
		if (unityFrame % 2 == 0) {
			blackCam.enabled = true;
			whiteCam.enabled = false;

			// Stop time 
			Time.timeScale = 0;

			// Check Capture
			StartCoroutine ( checkCapture() ); 
		} 
		// Black Cam
		else {
			blackCam.enabled = false;
			whiteCam.enabled = true;
			// Restore time speed
			Time.timeScale = originalTimeScale;
		}
		// Increment unity frame counter
		unityFrame++;
	}


	/**
	 * 
	 */
	private IEnumerator checkCapture (){
		//Check if the current direction is ticked by user
		if (directionCancapture(direction)) canCapture=true;
		//
		// still processing current animation ?
		//
		if (currentAnimFrame <= framesPerAnim) {
			yield return new WaitForEndOfFrame (); 
			if (canCapture) yield return StartCoroutine (CaptureWithScreen ());
			currentAnimFrame++;
		}
		//
		// Current animation done.
		//
		else {
			// Next animation ?
			if(currentClipIndex+1 < clips.Count){
				// Increment animation index
				currentClipIndex+=1;
				// Setup the animation
				SetupAnimation ();
			}
			// Next angle ?
			else if( direction != Direction.SouthEast){
				// Rotate the model
				objectToRender.transform.Rotate (Vector3.up * 45);
				direction =  getNextDirection(direction);
				canCapture=false;

				// Reset animation index
				currentClipIndex=0;
				// setup the animation
				SetupAnimation ();
			}

			// end of rendering
			else{
				EditorApplication.isPlaying=false;
			}
		}
	}

		

	/**
	 * 
	 */ 
	private void SetupAnimation(){
		if (clips.Count > 0) {
			AnimationClip clip = clips [currentClipIndex];
			if (clip != null) {
				// Using legacy animations ?
				if(legacy == true){				
					Animation anim = FindObjectOfType<Animation> ();
					if (anim != null) {
						// Restart anim if needed
						if(anim.IsPlaying (clip.name)){
							anim.Stop(clip.name);
						}
						anim.Play (clip.name, PlayMode.StopAll);	
					}
					else{
						// No animation
						Debug.LogError("Animation not found in this model");
						EditorApplication.isPlaying=false;
					}
				}
				//  Using the new mecanim animation
				else{
					// check that we have the animator for this model.
					if(animator == null) {
						Debug.LogError("Animator is null..");
						EditorApplication.isPlaying=false;
						return;	
					}

					// Get the current state (unused so far...)
					AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

					// get the hash Id for the required animation.
					int jumpHash = Animator.StringToHash(clip.name);

					// Force the required animation whatever the current state.
					animator.CrossFade(jumpHash, 0f);
				}

				// Set the current clip name
				currentClipName = clip.name;
				
				// Get alternative framerate for this animation
				frameRate = (int)(framesPerAnim / clip.length);
			}
		}
		// Reset animation counters
		unityFrame = 1;
		currentAnimFrame = 1;
		Time.captureFramerate = frameRate;
	}


	private string getDirectionString(Direction dir){
		switch (dir) {
		case Direction.South: return "S";
		case Direction.SouthWest: return "SW";
		case Direction.West: return "W";
		case Direction.NorthWest: return "NW";
		case Direction.North: return "N";
		case Direction.NorthEast: return "NE";
		case Direction.East: return "E";
		case Direction.SouthEast: return "SE";
		}
		return "";
	}

	private Direction getNextDirection(Direction dir){
		switch (dir) {
		case Direction.South: return Direction.SouthWest;
		case Direction.SouthWest: return Direction.West;
		case Direction.West: return Direction.NorthWest;
		case Direction.NorthWest: return Direction.North;
		case Direction.North: return Direction.NorthEast;
		case Direction.NorthEast: return Direction.East;
		case Direction.East: return Direction.SouthEast;
		case Direction.SouthEast: return Direction.South;
		}
		return Direction.South;
	}

	/*
	 * 
	 *  Capture the current Frame
	 * 
	 * 
	 */
	private IEnumerator CaptureWithScreen(){
		// Format: anim-SW-001
		string fileName =System.String.Format("{0}/"+currentClipName+"-"+getDirectionString(direction)+"-{1:D02}.png", directoryPath, currentAnimFrame);


		int width = Screen.width;
		int height = Screen.height; 
		
		Texture2D texb = new Texture2D(width, height, TextureFormat.RGB24, false);
		texb.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		texb.Apply();
		
		yield return 0; 
		yield return new WaitForEndOfFrame(); 
		
		Texture2D texw = new Texture2D(width, height, TextureFormat.RGB24, false);
		texw.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		texw.Apply();
		
		Texture2D outputtex = new Texture2D(width, height, TextureFormat.ARGB32, false);


		// Compare the 2 capture White vs Black cams, to get background
		for (int y = 0; y < outputtex.height; ++y) { 
			for (int x = 0; x < outputtex.width; ++x) { 
				var alpha = texw.GetPixel(x, y).r - texb.GetPixel(x, y).r;   
				alpha = 1.0f - alpha;
				Color color = Color.clear;
				if(alpha != 0) {
					color = texb.GetPixel(x, y) / alpha;
				} 
				color.a = alpha;
				outputtex.SetPixel(x, y, color);
			}
		}
		Texture2D tex=trim?outputtex.Trim(padding):outputtex;


		// Finally save the file
		if(createAtlas && createSingleSprites || !createAtlas){
			tex.SaveTexture(fileName);
		}
		// Clean temporary textures
		Destroy(texb);
		Destroy(texw);
	}

	////Vérification pour voir si les cases de directions sont cochées
	private bool directionCancapture(Direction dir)
	{
	switch (dir) {

		case Direction.South: if(south) return true;
							  else return false;
		case Direction.SouthWest: if(southWest) return true;
							      else return false;
		case Direction.West: if(west) return true;
							 else return false;
		case Direction.NorthWest: if(northWest) return true;
								  else return false;
		case Direction.North: if(north) return true;
							  else return false;
		case Direction.NorthEast: if(northEast) return true;
								  else return false;
		case Direction.East: if(east) return true;
							 else return false;
		case Direction.SouthEast: if(southEast) return true;
								  else return false;
		default : return false;
		}

	}
	#endif
}

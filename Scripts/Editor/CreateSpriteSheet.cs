using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class CreateSpriteSheet {
	[MenuItem ("Assets/Sprite Sheet/Trim",false,5)]
	private static void Trim() {
		Texture2D texture= Selection.activeObject as Texture2D;
		string path=AssetDatabase.GetAssetPath(texture);
		texture.SetReadable (true);
		Texture2D tex = texture.Trim (2);
		tex.SaveTexture (path);
	}
	[MenuItem ("Assets/Sprite Sheet/Sliced",false,1)]
	private static void CreateSpriteSheetAtlasSliced() {
		List<Texture2D> textures = new List<Texture2D> ();
		Object[] selectedObjects = Selection.objects;
		foreach (Object obj in selectedObjects) {
			if(obj is Texture2D){
				TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
				if (importer != null) {
					importer.textureType= TextureImporterType.Default;
					importer.npotScale= TextureImporterNPOTScale.None;
					importer.mipmapEnabled=false;
					importer.isReadable=true;
					
					AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath(obj));
					AssetDatabase.Refresh();
				}

				textures.Add(obj as Texture2D);
			}
		}
		textures.Sort ((x, y) => string.Compare(x.name, y.name));
		string path=AssetDatabase.GetAssetPath(textures[0]);
		path=path.Remove(path.LastIndexOf('/'));

		Texture2D atlas = new Texture2D(4096, 4096, TextureFormat.ARGB32, false);
		Color[] colors=new Color[4096*4096];
		for(int i=0; i< colors.Length;i++){
			colors[i]=Color.clear;
		}
		atlas.SetPixels(colors);
		atlas.Apply();
		
		Rect[] rects=atlas.PackTextures(textures.ToArray(), 0,false);

		byte[] bytes = atlas.EncodeToPNG(); 
		System.IO.File.WriteAllBytes(path+ "/atlas.png", bytes);

		AssetDatabase.Refresh();
		TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(path+"/atlas.png");
		if (tempImporter != null) {
			tempImporter.textureType = TextureImporterType.Sprite;
			tempImporter.spriteImportMode=SpriteImportMode.Multiple;
			tempImporter.maxTextureSize=4096;

			int count=textures.Count;
			SpriteMetaData[] meta= new SpriteMetaData[count];
			
			for(int i=0; i< count;i++){
				meta[i].name=i.ToString();
				meta[i].alignment = (int)SpriteAlignment.Center;
				meta[i].pivot=Vector2.zero;
				meta[i].rect = rects[i];
			}
			
			tempImporter.spritesheet=meta;
			AssetDatabase.ImportAsset (path+"/atlas.png");
			AssetDatabase.Refresh();
		}
	}

	[MenuItem ("Assets/Sprite Sheet/Uniform",false,1)]
	private static void CreateSpriteSheetAtlasUniform() {
		List<Texture2D> textures = new List<Texture2D> ();
		Object[] selectedObjects = Selection.objects;
		foreach (Object obj in selectedObjects) {
			if(obj is Texture2D){
				TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
				if (importer != null) {
					importer.textureType= TextureImporterType.Default;
					importer.npotScale= TextureImporterNPOTScale.None;
					importer.textureFormat=TextureImporterFormat.RGBA32;
					importer.mipmapEnabled=false;
					importer.isReadable=true;
					
					AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath(obj));
					AssetDatabase.Refresh();
				}
				
				textures.Add(obj as Texture2D);
			}
		}
		textures.Sort ((x, y) => string.Compare(x.name, y.name));
		string path=AssetDatabase.GetAssetPath(textures[0]);
		path=path.Remove(path.LastIndexOf('/'));

		Vector2 maxSize = Texture2DExtension.GetMaxSize (textures.ToArray ());
		float max = (maxSize.x > maxSize.y ? maxSize.x : maxSize.y);
		int framesPerLine = Mathf.CeilToInt(Mathf.Sqrt (textures.Count));
		int textureSize = Mathf.CeilToInt(framesPerLine * max);

		Texture2D atlas = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
		Color[] colors=new Color[textureSize*textureSize];
		for(int i=0; i< colors.Length;i++){
			colors[i]=Color.clear;
		}
		atlas.SetPixels(colors);
		atlas.Apply();
		
		Rect[] rects=atlas.PackTextures(textures.ToArray(),0,true);
		
		byte[] bytes = atlas.EncodeToPNG(); 
		System.IO.File.WriteAllBytes(path+ "/atlas.png", bytes);
		
		AssetDatabase.Refresh();
		TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(path+"/atlas.png");
		if (tempImporter != null) {
			tempImporter.textureType = TextureImporterType.Sprite;
			tempImporter.spriteImportMode=SpriteImportMode.Multiple;
			tempImporter.maxTextureSize=textureSize;
			
			int count=textures.Count;
			SpriteMetaData[] meta= new SpriteMetaData[count];
			for(int i=0; i< count;i++){
				meta[i].name=i.ToString();
				meta[i].alignment = (int)SpriteAlignment.Center;
				meta[i].pivot=Vector2.zero;
				meta[i].rect = rects[i];
			}
			
			tempImporter.spritesheet=meta;
			AssetDatabase.ImportAsset (path+"/atlas.png");
			AssetDatabase.Refresh();
		}
	}


	[MenuItem ("Assets/Sprite Sheet/Sliced", true)]
	private static bool ValidateCreateSpriteSheet () {
		Object[] selectedObjects = Selection.objects;
		int cnt = 0;
		foreach (Object obj in selectedObjects) {
			if(obj is Texture2D){
				cnt++;
			}
		}
		return cnt > 1;
	}

	[MenuItem ("Assets/Sprite Sheet/Uniform", true)]
	private static bool ValidateCreateSpriteSheetUniform () {
		Object[] selectedObjects = Selection.objects;
		int cnt = 0;
		foreach (Object obj in selectedObjects) {
			if(obj is Texture2D){
				cnt++;
			}
		}
		return cnt > 1;
	}

	[MenuItem ("Assets/Sprite Sheet/Pivot/CenterOfMass", false,1)]
	private static void SetPivotCenterOfMass() {
		Texture2D texture= Selection.activeObject as Texture2D;
		string path=AssetDatabase.GetAssetPath(texture);
		TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(path);
		SpriteMetaData[] meta = new SpriteMetaData[tempImporter.spritesheet.Length];
		texture.SetReadable (true);
		for(int i=0; i< meta.Length;i++){
			meta[i].name=i.ToString();
			meta[i].alignment = (int)SpriteAlignment.Custom;
			Vector2 centerOfMass=texture.CenterOfMass(tempImporter.spritesheet[i].rect);
			meta[i].pivot = new Vector2(centerOfMass.x/tempImporter.spritesheet[i].rect.width,centerOfMass.y/tempImporter.spritesheet[i].rect.height);
			meta[i].rect = tempImporter.spritesheet[i].rect;
		}
		tempImporter.spritesheet = meta;
		tempImporter.textureType = TextureImporterType.Sprite;
		AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath(texture));
		AssetDatabase.Refresh();
	}

	[MenuItem ("Assets/Sprite Sheet/Pivot/TopPixel", false,1)]
	private static void SetPivotTopPixel() {
		Texture2D texture= Selection.activeObject as Texture2D;
		string path=AssetDatabase.GetAssetPath(texture);
		TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(path);
		SpriteMetaData[] meta = new SpriteMetaData[tempImporter.spritesheet.Length];
		texture.SetReadable (true);
		for(int i=0; i< meta.Length;i++){
			meta[i].name=i.ToString();
			meta[i].alignment = (int)SpriteAlignment.Custom;
			Rect rect=tempImporter.spritesheet[i].rect;

			for (int y = (int)(rect.y); y < (int)(rect.height+rect.y); y++) { 
				for (int x = (int)rect.x; x < (int)(rect.width+rect.x); x++) { 
					Color color=texture.GetPixel(x,y);
					if(color.a != 0){
						meta[i].pivot = new Vector2((float)(x-rect.x)/rect.width, (float)(y-rect.y)/rect.height);
						break;
					}
				}
			}
			meta[i].rect = tempImporter.spritesheet[i].rect;
		}
		tempImporter.spritesheet = meta;
		tempImporter.textureType = TextureImporterType.Sprite;
		AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath(texture));
		AssetDatabase.Refresh();
	}

	[MenuItem ("Assets/Sprite Sheet/Pivot/RightPixel", false,1)]
	private static void SetPivotRightPixel() {
		Texture2D texture= Selection.activeObject as Texture2D;
		string path=AssetDatabase.GetAssetPath(texture);
		TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(path);
		SpriteMetaData[] meta = new SpriteMetaData[tempImporter.spritesheet.Length];
		texture.SetReadable (true);
		for(int i=0; i< meta.Length;i++){
			meta[i].name=i.ToString();
			meta[i].alignment = (int)SpriteAlignment.Custom;
			Rect rect=tempImporter.spritesheet[i].rect;

			for (int x = (int)rect.x; x < (int)(rect.width+rect.x); x++) { 
				for (int y = (int)(rect.y); y < (int)(rect.height+rect.y); y++) { 
	
					Color color=texture.GetPixel(x,y);
					if(color.a != 0){
						meta[i].pivot = new Vector2((float)(x-rect.x)/rect.width, (float)(y-rect.y)/rect.height);
						break;
					}
				}
			}
			meta[i].rect = tempImporter.spritesheet[i].rect;
		}
		tempImporter.spritesheet = meta;
		tempImporter.textureType = TextureImporterType.Sprite;
		AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath(texture));
		AssetDatabase.Refresh();
	}

	[MenuItem ("Assets/Sprite Sheet/Pivot/LeftPixel", false,1)]
	private static void SetPivotLeftPixel() {
		Texture2D texture= Selection.activeObject as Texture2D;
		string path=AssetDatabase.GetAssetPath(texture);
		TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(path);
		SpriteMetaData[] meta = new SpriteMetaData[tempImporter.spritesheet.Length];
		texture.SetReadable (true);
		for(int i=0; i< meta.Length;i++){
			meta[i].name=i.ToString();
			meta[i].alignment = (int)SpriteAlignment.Custom;
			Rect rect=tempImporter.spritesheet[i].rect;
			
			for (int x = (int)rect.x; x < (int)(rect.width+rect.x); x++) { 
				bool stop=false;
				for (int y = (int)(rect.y); y < (int)(rect.height+rect.y); y++) { 
					
					Color color=texture.GetPixel(x,y);
					if(color.a != 0){
						meta[i].pivot = new Vector2((float)(x-rect.x)/rect.width, (float)(y-rect.y)/rect.height);
						stop=true;
						break;
					}
				}
				if(stop)
					break;
			}
			meta[i].rect = tempImporter.spritesheet[i].rect;
		}
		tempImporter.spritesheet = meta;
		tempImporter.textureType = TextureImporterType.Sprite;
		AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath(texture));
		AssetDatabase.Refresh();
	}

	[MenuItem ("Assets/Sprite Sheet/Pivot/BottomPixel", false,1)]
	private static void SetPivotBottomPixel() {
		Texture2D texture= Selection.activeObject as Texture2D;
		string path=AssetDatabase.GetAssetPath(texture);
		TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(path);
		SpriteMetaData[] meta = new SpriteMetaData[tempImporter.spritesheet.Length];
		texture.SetReadable (true);
		for(int i=0; i< meta.Length;i++){
			meta[i].name=i.ToString();
			meta[i].alignment = (int)SpriteAlignment.Custom;
			Rect rect=tempImporter.spritesheet[i].rect;

			for (int y = (int)(rect.y); y < (int)(rect.height+rect.y); y++) { 
				bool stop=false;
				for (int x = (int)rect.x; x < (int)(rect.width+rect.x); x++) { 
					Color color=texture.GetPixel(x,y);
					if(color.a != 0){
						meta[i].pivot = new Vector2((float)(x-rect.x)/rect.width, (float)(y-rect.y)/rect.height);
						stop=true;
						break;
					}
				}
				if(stop)
					break;
			}
			meta[i].rect = tempImporter.spritesheet[i].rect;
		}
		tempImporter.spritesheet = meta;
		tempImporter.textureType = TextureImporterType.Sprite;
		AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath(texture));
		AssetDatabase.Refresh();
	}

	[MenuItem ("Assets/Sprite Sheet/Pivot/BottomPixel", true)]
	[MenuItem ("Assets/Sprite Sheet/Pivot/RightPixel", true)]
	[MenuItem ("Assets/Sprite Sheet/Pivot/LeftPixel", true)]	
	[MenuItem ("Assets/Sprite Sheet/Pivot/TopPixel", true)]
	[MenuItem ("Assets/Sprite Sheet/Pivot/CenterOfMass", true)]
	private static bool ValidatePivot() {
		if(Selection.activeObject is Texture2D){
			Texture2D texture= Selection.activeObject as Texture2D;
			string path=AssetDatabase.GetAssetPath(texture);
			TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(path);
			if (tempImporter != null && tempImporter.textureType == TextureImporterType.Sprite && tempImporter.spriteImportMode== SpriteImportMode.Multiple) {
				return true;
			}
		}
		return false;
	}
}

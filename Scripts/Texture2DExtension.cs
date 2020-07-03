using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;

public static class Texture2DExtension {
	#if UNITY_EDITOR
	public static void SetReadable(this Texture2D texture,bool isReadable){
		TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
		if (tempImporter != null) {
			Selection.activeObject=null;
			tempImporter.textureType=TextureImporterType.Default;
			tempImporter.isReadable = isReadable;
			AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath(texture));
			AssetDatabase.Refresh();
		}
	}
	
	public static void SetFormat(this Texture2D texture,TextureImporterFormat format){
		TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
		if (tempImporter != null) {
			tempImporter.textureType=TextureImporterType.Default;
			tempImporter.textureFormat = format;
			AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath(texture));
			AssetDatabase.Refresh();
		}
	}

	public static Rect[] PackTextures(this Texture2D atlas,Texture2D[] textures,int padding, bool uniformDimension){
		List<Rect> rects = new List<Rect> ();
		if (uniformDimension) {
			Vector2 maxSize = GetMaxSize (textures);
			float max=maxSize.x>maxSize.y?maxSize.x:maxSize.y;

			int x = 0;
			int y = 0;
			int p = (int)Mathf.Ceil (Mathf.Log (textures.Length) / Mathf.Log (2));
			Vector2 offset=Vector2.zero;


			for (int t=0; t< textures.Length; t++) {
				Color[] pixels = textures [t].GetPixels ();
				if (x >= p) {
					y++;
					x = 0;
				}
				offset= new Vector2((max-textures[t].width)*0.5f,(max-textures[t].height)*0.5f);

				rects.Add (new Rect (x * max + x * padding, y * max + y * padding, max, max));
				atlas.SetPixels ((int)(x * max + padding+offset.x), (int)(y * max + padding+offset.y), textures [t].width, textures [t].height, pixels);
				x++;
			}
			atlas.Apply ();
		} else {
			Rect[] r=atlas.PackTextures(textures,padding);
			for(int i=0;i<r.Length;i++){
				rects.Add(new Rect(r[i].x*atlas.width,r[i].y*atlas.height,textures[i].width,textures[i].height));
			}
		}
		return rects.ToArray ();
	
	}



	public static Vector2 CenterOfMass(this Texture2D texture){
		return texture.CenterOfMass (new Rect (0, 0, texture.width, texture.height));
	}

	public static Vector2 CenterOfMass(this Texture2D texture, Rect rect){
		float i=0;
		float total=0;
		Vector2 centerOfMass=Vector2.zero;
		for (int y = (int)rect.y; y < (int)(rect.height+ rect.y); y++) {
			for (int x = (int)rect.x; x < (int)(rect.width+rect.x); x++) {
				Color pixel = texture.GetPixel (x, y);
				i = pixel.a > 0 ? 1 : 0;
				centerOfMass.x += i * (x-rect.x);
				centerOfMass.y += i * (y-rect.y);
				total += i;
			}
		}
		centerOfMass.x /= total;
		centerOfMass.y /= total;
		return centerOfMass;
	}
	
	public static Rect[] GetSpriteRects(this Texture2D texture){
		string path=AssetDatabase.GetAssetPath(texture);
		TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(path);
		List<Rect> rects = new List<Rect> ();
		for (int i=0; i<tempImporter.spritesheet.Length; i++) {
			rects.Add(new Rect(tempImporter.spritesheet[i].rect));
		}
		return rects.ToArray();
	}


	public static Vector2 GetMaxSize(Texture2D[] textures){
		Vector2 maxSize = Vector2.zero;
		for (int i=0; i< textures.Length; i++) {
			if(maxSize.x<textures[i].width){
				maxSize.x=textures[i].width;
			}
			if(maxSize.y<textures[i].height){
				maxSize.y=textures[i].height;
			}
		}
		return maxSize;
	}

	public static void SaveTexture(this Texture2D texture, string path){
		byte[] bytes = texture.EncodeToPNG(); 
		System.IO.File.WriteAllBytes(path, bytes);
		AssetDatabase.Refresh ();
		//AssetDatabase.ImportAsset (path);
//		TextureImporter tempImporter = (TextureImporter)AssetImporter.GetAtPath(path.Substring(path.IndexOf("Assets")));
//		if (tempImporter != null) {
//			tempImporter.textureType = TextureImporterType.Sprite;
//			AssetDatabase.ImportAsset (path.Substring(path.IndexOf("Assets")));
//			AssetDatabase.Refresh();
//		}
	}

	public static int[] GetBounds(this Texture2D texture){
		int width = texture.width;
		int height = texture.height;
		int xMin = int.MaxValue,
		xMax = int.MinValue,
		yMin = int.MaxValue,
		yMax = int.MinValue;
		// Find xMin
		for (int x = 0; x < width; x++)
		{
			bool stop = false;
			for (int y = 0; y < height; y++)
			{
				float alpha = texture.GetPixel(x, y).a;   
				if (alpha != 0)
				{
					xMin = x;
					stop = true;
					break;
				}
			}
			if (stop)
				break;
		}
		
		// Find yMin
		for (int y = 0; y < height; y++)
		{
			bool stop = false;
			for (int x = xMin; x < width; x++)
			{
				float alpha = texture.GetPixel(x, y).a;
				if (alpha != 0)
				{
					yMin = y;
					stop = true;
					break;
				}
			}
			if (stop)
				break;
		}
		
		// Find xMax
		for (int x = width - 1; x >= xMin; x--)
		{
			bool stop = false;
			for (int y = yMin; y < height; y++)
			{
				float alpha = texture.GetPixel(x, y).a;
				if (alpha != 0)
				{
					xMax = x;
					stop = true;
					break;
				}
			}
			if (stop)
				break;
		}
		
		// Find yMax
		for (int y = height - 1; y >= yMin; y--)
		{
			bool stop = false;
			for (int x = xMin; x <= xMax; x++)
			{
				float alpha = texture.GetPixel(x, y).a;
				if (alpha != 0)
				{
					yMax = y;
					stop = true;
					break;
				}
			}
			if (stop)
				break;
		}
		return new int[]{xMin,xMax,yMin,yMax};
	}

	public static Texture2D Trim(this Texture2D texture, int padding){
		int[] bounds = texture.GetBounds ();
		int xMin = bounds[0],
		xMax = bounds[1],
		yMin = bounds[2],
		yMax = bounds[3];
	
		Texture2D tex = new Texture2D(xMax-xMin, yMax-yMin, TextureFormat.RGBA32, false);
		for (int y = 0; y < tex.height; ++y) { // each row
			for (int x = 0; x < tex.width; ++x) { // each column
				Color color = texture.GetPixel(xMin+x,yMin+ y);
				tex.SetPixel(x, y, color);
			}
		}
		return tex;
	}

	public static Texture2D Scale(this Texture2D source,int targetWidth,int targetHeight) {
		Texture2D result=new Texture2D(targetWidth,targetHeight,source.format,true);
		Color[] rpixels=result.GetPixels(0);
		float incX=(1.0f / (float)targetWidth);
		float incY=(1.0f / (float)targetHeight); 
		for(int px=0; px<rpixels.Length; px++) { 
			rpixels[px] = source.GetPixelBilinear(incX*((float)px%targetWidth), incY*((float)Mathf.Floor(px/targetWidth))); 
		} 
		result.SetPixels(rpixels,0); 
		result.Apply(); 
		return result; 
	}
#endif
}

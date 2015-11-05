using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class imageProcessing : MonoBehaviour {

	[DllImport ("__Internal")]
	private static extern void Init();

	[DllImport ("__Internal")]
	private static extern void UpdateTexture(System.IntPtr colors, int width, int height);
	
	WebCamTexture webcamTexture;
	Texture2D texture = null;
	
	// Use this for initialization
	void Start () {
		WebCamDevice[] devices = WebCamTexture.devices;
		if (devices.Length > 0) {
			webcamTexture = new WebCamTexture(devices[0].name ,320, 240, 10);
			webcamTexture.Play();
		}		
		Init ();
	}
	
	// Update is called once per frame
	void OnGUI() {
		Color32[] pixels = webcamTexture.GetPixels32();
		GCHandle pixelsHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
		UpdateTexture(pixelsHandle.AddrOfPinnedObject(), webcamTexture.width, webcamTexture.height);
		if (texture) Destroy(texture);
		texture = new Texture2D(webcamTexture.width, webcamTexture.height);
        texture.SetPixels32(pixels);
        texture.Apply();
		pixelsHandle.Free();
		GetComponent<Renderer>().material.mainTexture = texture;
	}
}

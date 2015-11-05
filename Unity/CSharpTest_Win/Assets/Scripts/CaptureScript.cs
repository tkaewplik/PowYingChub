using UnityEngine;
using System.Collections;
using OpenCvSharp;
using System.Runtime.InteropServices;   

public class CaptureScript : MonoBehaviour {

    const int CAPTURE_WIDTH = 320;
    const int CAPTURE_HEIGHT = 240;

    public Texture2D texture;
    private CvCapture capture;

	// Use this for initialization
	void Start () {
        capture = Cv.CreateCameraCapture(0);
        Cv.SetCaptureProperty(capture, CaptureProperty.FrameWidth, CAPTURE_WIDTH);
        Cv.SetCaptureProperty(capture, CaptureProperty.FrameHeight, CAPTURE_HEIGHT);
        IplImage frame = Cv.QueryFrame(capture);
        Debug.Log("width:" + frame.Width + " height:" + frame.Height);
        texture = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);
    }
	
	// Update is called once per frame
	void Update () {
        IplImage frame = Cv.QueryFrame(capture);
        Color[] cols = new Color[texture.width*texture.height];
        int t1 = System.Environment.TickCount;
        // ???????[?v???d??
        for (int y = 0; y < texture.height; y++) {
            for (int x = 0; x < texture.width; x++) {
                CvColor col = frame.Get2D(y, x);
                cols[y * texture.width + x] = new Color(col.R / 255.0f, col.G / 255.0f, col.B / 255.0f, 1.0f);
            }
        }
        int t2 = System.Environment.TickCount;
        texture.SetPixels(cols);
        int t3 = System.Environment.TickCount;
        Debug.Log("t2-t1=" + (t2 - t1) + " t3-t2=" + (t3 - t2));
        texture.Apply();
	}
}

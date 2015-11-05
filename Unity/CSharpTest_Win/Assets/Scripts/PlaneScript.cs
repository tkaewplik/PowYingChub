using UnityEngine;
using System.Collections;

public class PlaneScript : MonoBehaviour {

    public GameObject capture;
    public CaptureScript captureScript;

	// Use this for initialization
	void Start () {
        captureScript = capture.GetComponent<CaptureScript>();
    }
	
	// Update is called once per frame
	void Update () {
        GetComponent<Renderer>().material.mainTexture = captureScript.texture;
    }
}

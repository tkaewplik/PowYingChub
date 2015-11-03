using UnityEngine;
using System.Collections;

public class CameraScript : MonoBehaviour {

    public GameObject faceDetect;
    public FaceDetectScript faceDetectScript;

	// Use this for initialization
	void Start () {
        faceDetectScript = faceDetect.GetComponent<FaceDetectScript>();
	}
	
	// Update is called once per frame
	void Update () {
        Vector2 facepos = faceDetectScript.facepos;
        Debug.Log("facepos:(" + facepos.x + " " + facepos.y + ")");
        float alpha = facepos.y * Mathf.PI;
        float beta = facepos.x * Mathf.PI;
        Vector3 pos;
        pos.x = -3.0f * Mathf.Sin(alpha) * Mathf.Cos(beta);
        pos.y =  3.0f * Mathf.Cos(alpha);
        pos.z = -3.0f * Mathf.Sin(alpha) * Mathf.Sin(beta);
        transform.position = pos;
        Vector3 relativePos = -transform.position;
        Quaternion rotation = Quaternion.LookRotation(relativePos);
        transform.rotation = rotation;
    }
}

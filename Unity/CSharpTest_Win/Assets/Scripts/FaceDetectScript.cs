using UnityEngine;
using System.Collections;
using OpenCvSharp;
using OpenCvSharp.MachineLearning;
using System.Runtime.InteropServices;

public class FaceDetectScript : MonoBehaviour
{
    const int CAPTURE_WIDTH = 320;
    const int CAPTURE_HEIGHT = 240;

    public Vector2 facepos;

    private CvHaarClassifierCascade cascade;
    private CvCapture capture;

    CvColor[] colors = new CvColor[]{
                new CvColor(0,0,255),
                new CvColor(0,128,255),
                new CvColor(0,255,255),
                new CvColor(0,255,0),
                new CvColor(255,128,0),
                new CvColor(255,255,0),
                new CvColor(255,0,0),
                new CvColor(255,0,255),
            };

    const double Scale = 1.04;
    const double ScaleFactor = 1.139;
    const int MinNeighbors = 2;

    // Use this for initialization
    void Start()
    {
        cascade = CvHaarClassifierCascade.FromFile(@"./Assets/haarcascade_frontalface_alt.xml");
        capture = Cv.CreateCameraCapture(0);
        Cv.SetCaptureProperty(capture, CaptureProperty.FrameWidth, CAPTURE_WIDTH);
        Cv.SetCaptureProperty(capture, CaptureProperty.FrameHeight, CAPTURE_HEIGHT);
        IplImage frame = Cv.QueryFrame(capture);
        Debug.Log("width:" + frame.Width + " height:" + frame.Height);
        Cv.NamedWindow("FaceDetect");
		
		CvSVM svm = new CvSVM ();
	  	CvTermCriteria criteria = new CvTermCriteria (CriteriaType.Epsilon, 1000, double.Epsilon);
	  	CvSVMParams param = new CvSVMParams (CvSVM.C_SVC, CvSVM.RBF, 10.0, 8.0, 1.0, 10.0, 0.5, 0.1, null, criteria);
    }

    // Update is called once per frame
    void Update()
    {
        IplImage frame = Cv.QueryFrame(capture);

        using (IplImage img = Cv.CloneImage(frame))
        using (IplImage smallImg = new IplImage(new CvSize(Cv.Round(img.Width / Scale), Cv.Round(img.Height / Scale)), BitDepth.U8, 1))
        {
            // 顔検出用の画像の生成
            using (IplImage gray = new IplImage(img.Size, BitDepth.U8, 1))
            {
                Cv.CvtColor(img, gray, ColorConversion.BgrToGray);
                Cv.Resize(gray, smallImg, Interpolation.Linear);
                Cv.EqualizeHist(smallImg, smallImg);
            }

            using (CvMemStorage storage = new CvMemStorage())
            {
                storage.Clear();

                // 顔の検出
                CvSeq<CvAvgComp> faces = Cv.HaarDetectObjects(smallImg, cascade, storage, ScaleFactor, MinNeighbors, 0, new CvSize(64, 64));

                // 検出した箇所にまるをつける
                for (int i = 0; i < faces.Total; i++)
                {
                    CvRect r = faces[i].Value.Rect;
                    CvPoint center = new CvPoint
                    {
                        X = Cv.Round((r.X + r.Width * 0.5) * Scale),
                        Y = Cv.Round((r.Y + r.Height * 0.5) * Scale)
                    };
                    int radius = Cv.Round((r.Width + r.Height) * 0.25 * Scale);
                    img.Circle(center, radius, colors[i % 8], 3, LineType.AntiAlias, 0);
                }

                if (faces.Total > 0)
                {
                    CvRect r = faces[0].Value.Rect;
                    facepos = new Vector2((r.X + r.Width / 2.0f) / CAPTURE_WIDTH, (r.Y + r.Height / 2.0f) / CAPTURE_HEIGHT);
                }
            }

            // ウィンドウに表示
            Cv.ShowImage("FaceDetect", img);
        }
    }

    void OnDestroy()
    {
        Cv.DestroyAllWindows();
        Cv.ReleaseCapture(capture);
        Cv.ReleaseHaarClassifierCascade(cascade);
    }
}

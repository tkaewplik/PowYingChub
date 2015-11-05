#ifdef __cplusplus
#import <opencv2/opencv.hpp>
#endif

CvHaarClassifierCascade *cascade = 0;
CvMemStorage *storage = 0;

extern "C" {
    void Init();
    void UpdateTexture(char* data, int width, int height);
}

void Init()
{
	NSString* path = [[NSBundle mainBundle] pathForResource: @"haarcascade_frontalface_default" ofType: @"xml"];
	
	// (2)ブーストされた分類器のカスケードを読み込む
	cascade = (CvHaarClassifierCascade *) cvLoad ([ path cStringUsingEncoding : 1 ], 0, 0, 0);
	
	// (3)メモリを確保し，読み込んだ画像のグレースケール化，ヒストグラムの均一化を行う
	storage = cvCreateMemStorage (0);
	cvClearMemStorage (storage);
}

void UpdateTexture(char* data, int width, int height)
{
    IplImage* src_img = cvCreateImageHeader(cvSize(width, height), IPL_DEPTH_8U, 4);
    cvSetData(src_img, data, src_img->widthStep);
    
	CvSeq *faces = NULL;
	static CvScalar colors[] = {
		{{0, 0, 255}}, {{0, 128, 255}},
		{{0, 255, 255}}, {{0, 255, 0}},
		{{255, 128, 0}}, {{255, 255, 0}},
		{{255, 0, 0}}, {{255, 0, 255}}
	};
	
	IplImage* src_gray = cvCreateImage (cvGetSize (src_img), IPL_DEPTH_8U, 1);
	
    // (3)メモリを確保し，読み込んだ画像のグレースケール化，ヒストグラムの均一化を行う
	cvCvtColor (src_img, src_gray, CV_RGBA2GRAY);
	cvEqualizeHist (src_gray, src_gray);
    
	// (4)物体（顔）検出
	faces = cvHaarDetectObjects (src_gray, cascade, storage, 1.11, 4, 0, cvSize (40, 40));
    
	// (5)検出された全ての顔位置に，円を描画する
	for (int i = 0; i < (faces ? faces->total : 0); i++) {
		CvRect *r = (CvRect *) cvGetSeqElem (faces, i);
		CvPoint center;
		int radius;
		center.x = cvRound (r->x + r->width * 0.5);
		center.y = cvRound (r->y + r->height * 0.5);
		radius = cvRound ((r->width + r->height) * 0.25);
		cvCircle (src_img, center, radius, colors[i % 8], 3, 8, 0);
	}	
    
    cvReleaseImageHeader(&src_img);
    cvReleaseImage(&src_gray);
}


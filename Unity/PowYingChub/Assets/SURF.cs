using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenCvSharp;

namespace OpenCvSharp.Test
{
	/// <summary>
	/// SURF(Speeded Up Robust Features)による対応点検出        
	/// </summary>
	/// <remarks>samples/c/find_obj.cpp から改変</remarks>
	class SURF
	{        
		public SURF()
		{
			// cvExtractSURF
			// SURFで対応点検出
			
			using (CvMemStorage storage = Cv.CreateMemStorage(0))
				using (IplImage obj = Cv.LoadImage(Const.ImageSurfBox, LoadMode.GrayScale))
					using (IplImage image = Cv.LoadImage(Const.ImageSurfBoxinscene, LoadMode.GrayScale))
					using (IplImage objColor = Cv.CreateImage(obj.Size, BitDepth.U8, 3))
					using (IplImage correspond = Cv.CreateImage(new CvSize(image.Width, obj.Height + image.Height), BitDepth.U8, 1))
			{
				Cv.CvtColor(obj, objColor, ColorConversion.GrayToBgr);
				
				Cv.SetImageROI(correspond, new CvRect(0, 0, obj.Width, obj.Height));
				Cv.Copy(obj, correspond);
				Cv.SetImageROI(correspond, new CvRect(0, obj.Height, correspond.Width, correspond.Height));
				Cv.Copy(image, correspond);
				Cv.ResetImageROI(correspond);
				
				// SURFの処理
				CvSeq<CvSURFPoint> objectKeypoints, imageKeypoints;
				CvSeq<float> objectDescriptors, imageDescriptors;
				Stopwatch watch = Stopwatch.StartNew();
				{
					CvSURFParams param = new CvSURFParams(500, true);
					Cv.ExtractSURF(obj, null, out objectKeypoints, out objectDescriptors, storage, param);
					Console.WriteLine("Object Descriptors: {0}", objectDescriptors.Total);
					Cv.ExtractSURF(image, null, out imageKeypoints, out imageDescriptors, storage, param);
					Console.WriteLine("Image Descriptors: {0}", imageDescriptors.Total);
				}
				watch.Stop();
				Console.WriteLine("Extraction time = {0}ms", watch.ElapsedMilliseconds);
				watch.Reset();
				watch.Start();
				
				// シーン画像にある局所画像の領域を線で囲む
				CvPoint[] srcCorners = new CvPoint[4]{
					new CvPoint(0,0), new CvPoint(obj.Width,0), new CvPoint(obj.Width, obj.Height), new CvPoint(0, obj.Height)
				};
				CvPoint[] dstCorners = LocatePlanarObject(objectKeypoints, objectDescriptors, imageKeypoints, imageDescriptors, srcCorners); 
				if (dstCorners != null)
				{
					for (int i = 0; i < 4; i++)
					{
						CvPoint r1 = dstCorners[i % 4];
						CvPoint r2 = dstCorners[(i + 1) % 4];
						Cv.Line(correspond, new CvPoint(r1.X, r1.Y + obj.Height), new CvPoint(r2.X, r2.Y + obj.Height), CvColor.White);
					}
				}
				
				// 対応点同士を線で引く
				int[] ptpairs = FindPairs(objectKeypoints, objectDescriptors, imageKeypoints, imageDescriptors);
				for (int i = 0; i < ptpairs.Length; i += 2)
				{
					CvSURFPoint r1 = Cv.GetSeqElem<CvSURFPoint>(objectKeypoints, ptpairs[i]).Value;
					CvSURFPoint r2 = Cv.GetSeqElem<CvSURFPoint>(imageKeypoints, ptpairs[i + 1]).Value;
					Cv.Line(correspond, r1.Pt, new CvPoint(Cv.Round(r2.Pt.X), Cv.Round(r2.Pt.Y + obj.Height)), CvColor.White);
				}                
				
				// 特徴点の場所に円を描く
				for (int i = 0; i < objectKeypoints.Total; i++)
				{
					CvSURFPoint r = Cv.GetSeqElem<CvSURFPoint>(objectKeypoints, i).Value;
					CvPoint center = new CvPoint(Cv.Round(r.Pt.X), Cv.Round(r.Pt.Y));
					int radius = Cv.Round(r.Size * (1.2 / 9.0) * 2);
					Cv.Circle(objColor, center, radius, CvColor.Red, 1, LineType.AntiAlias, 0);
				}
				watch.Stop();
				Console.WriteLine("Drawing time = {0}ms", watch.ElapsedMilliseconds);
				
				// ウィンドウに表示
				Cv.NamedWindow("Object", WindowMode.AutoSize);
				Cv.NamedWindow("Object Correspond", WindowMode.AutoSize);
				Cv.ShowImage("Object Correspond", correspond);
				Cv.ShowImage("Object", objColor);
				
				Cv.WaitKey(0);
				
				Cv.DestroyWindow("Object");
				Cv.DestroyWindow("Object Correspond");
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="d1Ptr">Cではconst float*</param>
		/// <param name="d2Ptr">Cではconst float*</param>
		/// <param name="best"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		private static unsafe double CompareSurfDescriptors(IntPtr d1Ptr, IntPtr d2Ptr, double best, int length)
		{
			//Debug.Assert(length % 4 == 0);
			
			double totalCost = 0;
			
			// ポインタでのアクセスの代わりに配列にコピーしてからやる。            
			/*float[] d1 = new float[length];
            float[] d2 = new float[length];
            Marshal.Copy(d1Ptr, d1, 0, length);
            Marshal.Copy(d2Ptr, d2, 0, length);*/
			
			// 遅くて問題ならunsafeとか
			float* d1 = (float*)d1Ptr;
			float* d2 = (float*)d2Ptr;
			
			double t0, t1, t2, t3;
			for (int i = 0; i < length; i += 4)
			{
				t0 = d1[i] - d2[i];
				t1 = d1[i + 1] - d2[i + 1];
				t2 = d1[i + 2] - d2[i + 2];
				t3 = d1[i + 3] - d2[i + 3];
				totalCost += t0 * t0 + t1 * t1 + t2 * t2 + t3 * t3;
				if (totalCost > best)
					break;
			}
			
			return totalCost;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="vec">Cではconst float*</param>
		/// <param name="laplacian"></param>
		/// <param name="model_keypoints"></param>
		/// <param name="model_descriptors"></param>
		/// <returns></returns>
		private static int NaiveNearestNeighbor(IntPtr vec, int laplacian, CvSeq<CvSURFPoint> model_keypoints, CvSeq<float> model_descriptors)
		{
			int length = (int)(model_descriptors.ElemSize / sizeof(float));
			int neighbor = -1;
			double dist1 = 1e6, dist2 = 1e6;
			CvSeqReader<float> reader = new CvSeqReader<float>();
			CvSeqReader<CvSURFPoint> kreader = new CvSeqReader<CvSURFPoint>();          
			Cv.StartReadSeq(model_keypoints, kreader, false);
			Cv.StartReadSeq(model_descriptors, reader, false);
			
			IntPtr mvec;
			CvSURFPoint kp;
			double d;
			for (int i = 0; i < model_descriptors.Total; i++)
			{
				// const CvSURFPoint* kp = (const CvSURFPoint*)kreader.ptr; が結構曲者。
				// OpenCvSharpの構造体はFromPtrでポインタからインスタンス生成できるようにしてるので、こう書ける。
				kp = CvSURFPoint.FromPtr(kreader.Ptr);
				// まともにキャストする場合はこんな感じか
				// CvSURFPoint kp = (CvSURFPoint)Marshal.PtrToStructure(kreader.Ptr, typeof(CvSURFPoint));  
				
				mvec = reader.Ptr;
				Cv.NEXT_SEQ_ELEM(kreader.Seq.ElemSize, kreader);
				Cv.NEXT_SEQ_ELEM(reader.Seq.ElemSize, reader);
				if (laplacian != kp.Laplacian)
				{
					continue;
				}
				d = CompareSurfDescriptors(vec, mvec, dist2, length);
				if (d < dist1)
				{
					dist2 = dist1;
					dist1 = d;
					neighbor = i;
				}
				else if (d < dist2)
					dist2 = d;
			}
			if (dist1 < 0.6 * dist2)
				return neighbor;
			else
				return -1;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectKeypoints"></param>
		/// <param name="objectDescriptors"></param>
		/// <param name="imageKeypoints"></param>
		/// <param name="imageDescriptors"></param>
		/// <returns></returns>
		private static int[] FindPairs(CvSeq<CvSURFPoint> objectKeypoints, CvSeq<float> objectDescriptors, CvSeq<CvSURFPoint> imageKeypoints, CvSeq<float> imageDescriptors)
		{
			CvSeqReader<float> reader = new CvSeqReader<float>();
			CvSeqReader<CvSURFPoint> kreader = new CvSeqReader<CvSURFPoint>();
			Cv.StartReadSeq(objectDescriptors, reader);
			Cv.StartReadSeq(objectKeypoints, kreader);
			
			List<int> ptpairs = new List<int>();
			
			for (int i = 0; i < objectDescriptors.Total; i++)
			{
				CvSURFPoint kp = CvSURFPoint.FromPtr(kreader.Ptr);
				IntPtr descriptor = reader.Ptr;
				Cv.NEXT_SEQ_ELEM(kreader.Seq.ElemSize, kreader);
				Cv.NEXT_SEQ_ELEM(reader.Seq.ElemSize, reader);
				int nearestNeighbor = NaiveNearestNeighbor(descriptor, kp.Laplacian, imageKeypoints, imageDescriptors);                
				if (nearestNeighbor >= 0)
				{
					ptpairs.Add(i);
					ptpairs.Add(nearestNeighbor);
				}
			} 
			return ptpairs.ToArray();
		}
		
		/// <summary>
		/// a rough implementation for object location
		/// </summary>
		/// <param name="objectKeypoints"></param>
		/// <param name="objectDescriptors"></param>
		/// <param name="imageKeypoints"></param>
		/// <param name="imageDescriptors"></param>
		/// <param name="srcCorners"></param>
		/// <returns></returns>
		private static CvPoint[] LocatePlanarObject(CvSeq<CvSURFPoint> objectKeypoints, CvSeq<float> objectDescriptors,
		                                            CvSeq<CvSURFPoint> imageKeypoints, CvSeq<float> imageDescriptors,
		                                            CvPoint[] srcCorners)
		{
			CvMat h = new CvMat(3, 3, MatrixType.F64C1);            
			int[] ptpairs = FindPairs(objectKeypoints, objectDescriptors, imageKeypoints, imageDescriptors);            
			int n = ptpairs.Length / 2;
			if (n < 4)
				return null;
			
			CvPoint2D32f[] pt1 = new CvPoint2D32f[n];
			CvPoint2D32f[] pt2 = new CvPoint2D32f[n];
			for (int i = 0; i < n; i++)
			{
				pt1[i] = (Cv.GetSeqElem<CvSURFPoint>(objectKeypoints, ptpairs[i * 2])).Value.Pt;
				pt2[i] = (Cv.GetSeqElem<CvSURFPoint>(imageKeypoints, ptpairs[i * 2 + 1])).Value.Pt;
			}
			
			CvMat pt1Mat = new CvMat(1, n, MatrixType.F32C2, pt1);
			CvMat pt2Mat = new CvMat(1, n, MatrixType.F32C2, pt2);
			if (Cv.FindHomography(pt1Mat, pt2Mat, h, HomographyMethod.Ransac, 5) == 0)
				return null;
			
			CvPoint[] dstCorners = new CvPoint[4];
			for (int i = 0; i < 4; i++)
			{
				double x = srcCorners[i].X;
				double y = srcCorners[i].Y;
				double Z = 1.0 / (h[6] * x + h[7] * y + h[8]);
				double X = (h[0] * x + h[1] * y + h[2]) * Z;
				double Y = (h[3] * x + h[4] * y + h[5]) * Z;
				dstCorners[i] = new CvPoint(Cv.Round(X), Cv.Round(Y));
			}
			
			return dstCorners;
		}
		
	}
}
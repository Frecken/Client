#include "pch.h"
#include "OCV3_Class.h"
#include <opencv2/imgproc.hpp>
#include <math.h>

using namespace Windows::Storage::Streams;
using namespace Windows::Graphics::Imaging;
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace Platform;
using namespace OCV3;
using namespace cv;
using namespace std;


vector<vector<Point>> SmoothContour(vector<vector<Point>> contours, int largestContourIndex)
{
	vector<vector<Point>> smoothedContour(1, vector<Point>(4));

	for (int i = 0; i < 4; i++)
	{
		smoothedContour[0][i].x = contours[largestContourIndex][0].x;
		smoothedContour[0][i].y = contours[largestContourIndex][0].y;
	}

	for (unsigned int i = 0; i < contours[largestContourIndex].size(); i++)
	{
		if (contours[largestContourIndex][i].x < smoothedContour[0][0].x)
		{
			smoothedContour[0][0].x = contours[largestContourIndex][i].x;
		}
		if (contours[largestContourIndex][i].y < smoothedContour[0][0].y)
		{
			smoothedContour[0][0].y = contours[largestContourIndex][i].y;
		}

		if (contours[largestContourIndex][i].x < smoothedContour[0][1].x)
		{
			smoothedContour[0][1].x = contours[largestContourIndex][i].x;
		}
		if (contours[largestContourIndex][i].y > smoothedContour[0][1].y)
		{
			smoothedContour[0][1].y = contours[largestContourIndex][i].y;
		}

		if (contours[largestContourIndex][i].x > smoothedContour[0][2].x)
		{
			smoothedContour[0][2].x = contours[largestContourIndex][i].x;
		}
		if (contours[largestContourIndex][i].y > smoothedContour[0][2].y)
		{
			smoothedContour[0][2].y = contours[largestContourIndex][i].y;
		}

		if (contours[largestContourIndex][i].x > smoothedContour[0][3].x)
		{
			smoothedContour[0][3].x = contours[largestContourIndex][i].x;
		}
		if (contours[largestContourIndex][i].y < smoothedContour[0][3].y)
		{
			smoothedContour[0][3].y = contours[largestContourIndex][i].y;
		}
	}

	return smoothedContour;
}

bool OCV3_Class::DetectBarcode(WriteableBitmap^ wb)
{
	BYTE *extracted = new BYTE[wb->PixelBuffer->Length];
	DataReader::FromBuffer(wb->PixelBuffer)->ReadBytes(ArrayReference<BYTE>(extracted, wb->PixelBuffer->Length));

	Mat imgForOperations(wb->PixelHeight, wb->PixelWidth, CV_8UC4, extracted);

	cvtColor(imgForOperations, imgForOperations, CV_BGR2GRAY);

	Scalar imgMean = mean(imgForOperations);

	Mat gradX;
	Mat gradY;
	Sobel(imgForOperations, gradX, CV_8U, 1, 0);
	Sobel(imgForOperations, gradY, CV_8U, 0, 1);
	subtract(gradX, gradY, imgForOperations);
	gradX.release();
	gradY.release();

	convertScaleAbs(imgForOperations, imgForOperations);

	blur(imgForOperations, imgForOperations, Size(9, 9));

	threshold(imgForOperations, imgForOperations, (int)(imgMean[0] * 0.65), 255, THRESH_BINARY);

	morphologyEx(imgForOperations, imgForOperations, MORPH_CLOSE, getStructuringElement(MORPH_RECT, Size(21, 7)));

	erode(imgForOperations, imgForOperations, Mat(), Point(-1, 1), 5);
	dilate(imgForOperations, imgForOperations, Mat(), Point(-1, 1), 5);

	vector<vector<Point>> contours;
	vector<Vec4i> hierarchy;
	findContours(imgForOperations, contours, hierarchy, RETR_EXTERNAL, CHAIN_APPROX_SIMPLE, Point());
	hierarchy.clear();
	imgForOperations.release();
	delete[] extracted;

	if (contours.size() > 0)
	{
		double largestArea = 0;
		int largestContourIndex = 0;

		for (unsigned int i = 0; i < contours.size(); i++)
		{
			double area = contourArea(contours[i]);

			if (area > largestArea)
			{
				largestArea = area;
				largestContourIndex = i;
			}
		}

		vector<vector<Point>> smoothedContour = SmoothContour(contours, largestContourIndex);
		contours.clear();

		int horizontalSize = abs(smoothedContour[0][0].x - smoothedContour[0][3].x);
		int verticalSize = abs(smoothedContour[0][0].y - smoothedContour[0][1].y);

		if (horizontalSize >= 100 && verticalSize >= 50)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	else
	{
		return false;
	}
}
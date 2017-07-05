#pragma once

using namespace Windows::Storage::Streams;
using namespace Windows::Graphics::Imaging;
using namespace Windows::UI::Xaml::Media::Imaging;

namespace OCV3
{
	[Windows::Foundation::Metadata::WebHostHidden]

	public ref class OCV3_Class sealed
	{
	public:
		bool DetectBarcode(WriteableBitmap^ wb);
	};
}

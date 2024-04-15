// *****************************************************************************
// File:       ImageInfo.cs
// Solution:   ResourceReflector
// Project:    Resource Reflector
// Date:       07/10/2019
// Author:     Latency McLaughlin
// Copywrite:  Bio-Hazard Industries - 1998-2019
// *****************************************************************************

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

[Serializable]
public class ImageInfo : IDisposable {
  #region Data

  private Image _image;

  #endregion // Data

  #region Constructor

  public ImageInfo(object sourceObject, string resourceName) {
    SourceObject = sourceObject;
    ResourceDetails = new ResourceDetails(Image, DetermineImageType(), resourceName);
  }

  #endregion // Constructor

  #region Image

  public Image Image {
    get {
      if (_image != null)
        return _image;
      if (SourceObject is Icon o) {
        _image = o.ToBitmap();
      } else {
        var cursor1 = SourceObject as Cursor;
        if (cursor1 != null) {
          var cursor = cursor1;
          var size = cursor.Size;
          _image = new Bitmap(size.Width, size.Height);
          using (var grfx = Graphics.FromImage(_image)) {
            cursor.Draw(grfx, new Rectangle(Point.Empty, size));
          }
        } else {
          if (SourceObject is Image image)
            _image = image;
          else
            Debug.Fail("Unexpected type of source object.");
        }
      }

      return _image;
    }
  }

  #endregion // Image

  #region ResourceDetails

  public ResourceDetails ResourceDetails { get; }

  #endregion // ResourceDetails

  #region SourceObject

  [Browsable(false)]
  public object SourceObject { get; }

  #endregion // SourceObject

  #region IDisposable Members

  public void Dispose() {
    _image?.Dispose();
    (SourceObject as IDisposable)?.Dispose();
  }

  #endregion // IDisposable Members

  [DllImport("gdi32.dll")]
  public static extern bool DeleteObject(IntPtr hObject);

  #region GIFConverter

  // ReSharper disable once InconsistentNaming
  public static System.Windows.Controls.Image GIFConverter(Stream bitmapStream) {
    try {
      // If image is a GIF, only capture the 1st frame.
      var decoder = new GifBitmapDecoder(bitmapStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
      BitmapSource bitmapSource = decoder.Frames[0];
      // Draw the Image
      return new System.Windows.Controls.Image {
        Source = bitmapSource
      };
    } catch {
      return null;
    }
  }

  #endregion // GIFConverter

  #region DetermineImageType

  private ImageType DetermineImageType() {
    switch (SourceObject) {
      case Icon _:
        return ImageType.Icon;
      case Cursor _:
        return ImageType.Cursor;
      case Image _:
        return ImageType.Image;
      default:
        throw new ApplicationException("Unexpected type of source object.");
    }
  }

  #endregion // DetermineImageType

  #region ImageConverter

  public static void Image_Converter(Image bmp, out System.Windows.Controls.Image image) {
    using (var bitmap = new Bitmap(bmp)) {
      var hBitmap = bitmap.GetHbitmap();

      var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

      image = new System.Windows.Controls.Image {
        Source = bitmapSource
      };

      DeleteObject(hBitmap);
    }
  }

  #region ScaleImage

  public static Image ScaleImage(System.Windows.Controls.Image image) => ScaleImage((BitmapSource) image.Source, new Size(-1, -1));

  public static Image ScaleImage(System.Windows.Controls.Image image, Size scale) => ScaleImage((BitmapSource) image.Source, scale);

  public static Image ScaleImage(Stream stm) => ScaleImage(stm, new Size(-1, -1));

  public static Image ScaleImage(Stream stm, Size scale) {
    // Since we're not specifying a System.Windows.Media.Imaging.BitmapCacheOption, the pixel format
    // will be System.Windows.Media.PixelFormats.Pbgra32.
    BitmapSource bitmapSource = BitmapFrame.Create(stm, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
    return ScaleImage(bitmapSource, scale);
  }

  private static Image ScaleImage(BitmapSource bitmapSource, Size scale) {
    // Scale the image so that it will display similarly to the WPF Image.
    var newWidthRatio = scale.Width != -1 ? scale.Width / (double) bitmapSource.PixelWidth : 1.0;
    var newHeightRatio = scale.Height != -1 ? scale.Width * bitmapSource.PixelHeight / (double) bitmapSource.PixelWidth / bitmapSource.PixelHeight : 1.0;

    BitmapSource transformedBitmapSource = new TransformedBitmap(bitmapSource, new ScaleTransform(newWidthRatio, newHeightRatio));

    var width = transformedBitmapSource.PixelWidth;
    var height = transformedBitmapSource.PixelHeight;
    var stride = width * ((transformedBitmapSource.Format.BitsPerPixel + 7) / 8);

    var bits = new byte[height * stride];

    transformedBitmapSource.CopyPixels(bits, stride, 0);

    unsafe {
      fixed (byte* pBits = bits) {
        var bitmap = new Bitmap(width, height, stride, PixelFormat.Format32bppPArgb, new IntPtr(pBits));
        return bitmap;
      }
    }
  }

  #endregion // ScaleImage

  #endregion // ImageConverter
}
// ****************************************************************************
// * Project:  Resource Reflector
// * File:     Helper Types.cs
// * Author:   Latency McLaughlin
// * Date:     04/19/2014
// ****************************************************************************

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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

namespace ResourceReflector {

  #region ImageInfo

  [Serializable]
  public class ImageInfo : IDisposable {
    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

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
        var o = SourceObject as Icon;
        if (o != null)
          _image = o.ToBitmap();
        else {
          var cursor1 = SourceObject as Cursor;
          if (cursor1 != null) {
            var cursor = cursor1;
            var size = cursor.Size;
            _image = new Bitmap(size.Width, size.Height);
            using (var grfx = Graphics.FromImage(_image))
              cursor.Draw(grfx, new Rectangle(Point.Empty, size));
          } else {
            var image = SourceObject as Image;
            if (image != null)
              _image = image;
            else
              Debug.Fail("Unexpected type of source object.");
          }
        }
        return _image;
      }
    }

    #endregion // Image

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

    public static Image ScaleImage(System.Windows.Controls.Image image) {
      return ScaleImage((BitmapSource) image.Source, new Size(-1, -1));
    }

    public static Image ScaleImage(System.Windows.Controls.Image image, Size scale) {
      return ScaleImage((BitmapSource) image.Source, scale);
    }

    public static Image ScaleImage(Stream stm) {
      return ScaleImage(stm, new Size(-1, -1));
    }

    public static Image ScaleImage(Stream stm, Size scale) {
      // Since we're not specifying a System.Windows.Media.Imaging.BitmapCacheOption, the pixel format
      // will be System.Windows.Media.PixelFormats.Pbgra32.
      BitmapSource bitmapSource = BitmapFrame.Create(stm, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
      return ScaleImage(bitmapSource, scale);
    }

    private static Image ScaleImage(BitmapSource bitmapSource, Size scale) {
      // Scale the image so that it will display similarly to the WPF Image.
      var newWidthRatio = (scale.Width != -1 ? scale.Width/(double) bitmapSource.PixelWidth : 1.0);
      var newHeightRatio = (scale.Height != -1 ? ((scale.Width*bitmapSource.PixelHeight)/(double) bitmapSource.PixelWidth)/bitmapSource.PixelHeight : 1.0);

      BitmapSource transformedBitmapSource = new TransformedBitmap(bitmapSource, new ScaleTransform(newWidthRatio, newHeightRatio));

      var width = transformedBitmapSource.PixelWidth;
      var height = transformedBitmapSource.PixelHeight;
      var stride = width*((transformedBitmapSource.Format.BitsPerPixel + 7)/8);

      var bits = new byte[height*stride];

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

    #region DetermineImageType

    private ImageType DetermineImageType() {
      if (SourceObject is Icon)
        return ImageType.Icon;

      if (SourceObject is Cursor)
        return ImageType.Cursor;

      if (SourceObject is Image)
        return ImageType.Image;

      throw new ApplicationException("Unexpected type of source object.");
    }

    #endregion // DetermineImageType

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
  }

  #endregion // ImageInfo

  #region ResourceDetails

  [Serializable]
  public struct ResourceDetails {
    #region Data

    private readonly Image _image;

    #endregion // Data

    #region Constructor

    public ResourceDetails(Image image, ImageType imageType, string resourceName) {
      _image = image;
      ImageType = imageType;
      ResourceName = resourceName;
    }

    #endregion // Constructor

    #region Properties

    [Description("The horizontal resolution, in pixels per inch, of the image.")]
    [Category("Image Data")]
    public float HorizontalResolution => _image.HorizontalResolution;

    [Description("The type of file the image was stored as in the assembly.")]
    [Category("Resource Data")]
    public ImageType ImageType { get; }

    [Description("The width and height of the image.")]
    [Category("Image Data")]
    public SizeF PhysicalDimension => _image.PhysicalDimension;

    [Description("The pixel format of the image.")]
    [Category("Image Data")]
    public PixelFormat PixelFormat => _image.PixelFormat;

    [Description("The format of the image (an ImageFormat value).")]
    [Category("Image Data")]
    public ImageFormat RawFormat => _image.RawFormat;

    [Description("The name of the resource in the assembly.")]
    [Category("Resource Data")]
    public string ResourceName { get; }

    [Description("The width and height, in pixels, of the image.")]
    [Category("Image Data")]
    public Size Size => _image.Size;

    [Description("The horizontal resolution, in pixels per inch, of the image.")]
    [Category("Image Data")]
    public float VerticalResolution => _image.VerticalResolution;

    #endregion // Properties
  }

  #endregion // ResourceDetails

  #region ImageType

  public enum ImageType {
    Cursor,
    Icon,
    Image
  }

  #endregion // ImageType
}
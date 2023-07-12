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

namespace Resource_Reflector;

[Serializable]
public class ImageInfo : IDisposable
{
    private Image? _image;


    public ImageInfo(object? sourceObject, string? resourceName)
    {
        SourceObject    = sourceObject;
        ResourceDetails = new(Image, DetermineImageType(), resourceName);
    }


    public Image? Image
    {
        get
        {
            if (_image != null)
                return _image;
            if (SourceObject is Icon o)
            {
                _image = o.ToBitmap();
            }
            else
            {
                var cursor1 = SourceObject as Cursor;
                if (cursor1 != null)
                {
                    var size = cursor1.Size;
                    _image = new Bitmap(size.Width, size.Height);
                    using var grfx = Graphics.FromImage(_image);
                    cursor1.Draw(grfx, new(Point.Empty, size));
                }
                else
                {
                    if (SourceObject is Image image)
                        _image = image;
                    else
                        Debug.Fail("Unexpected type of source object.");
                }
            }

            return _image;
        }
    }

    public ResourceDetails ResourceDetails { get; }

    [Browsable(false)]
    public object? SourceObject { get; }

    public void Dispose()
    {
        _image?.Dispose();
        (SourceObject as IDisposable)?.Dispose();
    }

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);

    // ReSharper disable once InconsistentNaming
    public static System.Windows.Controls.Image? GIFConverter(Stream bitmapStream)
    {
        try
        {
            // If image is a GIF, only capture the 1st frame.
            var          decoder      = new GifBitmapDecoder(bitmapStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];
            // Draw the Image
            return new()
            {
                Source = bitmapSource
            };
        }
        catch
        {
            return null;
        }
    }


    private ImageType DetermineImageType()
    {
        if (SourceObject is Icon)
            return ImageType.Icon;

        if (SourceObject is Cursor)
            return ImageType.Cursor;

        if (SourceObject is Image)
            return ImageType.Image;

        throw new ApplicationException("Unexpected type of source object.");
    }


    public static void Image_Converter(Image bmp, out System.Windows.Controls.Image image)
    {
        using var bitmap  = new Bitmap(bmp);
        var       hBitmap = bitmap.GetHbitmap();

        var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        image = new()
        {
            Source = bitmapSource
        };

        DeleteObject(hBitmap);
    }

    #region ScaleImage

    public static Image ScaleImage(System.Windows.Controls.Image image) => ScaleImage((BitmapSource)image.Source, new(-1, -1));

    public static Image ScaleImage(System.Windows.Controls.Image image, Size scale) => ScaleImage((BitmapSource)image.Source, scale);

    public static Image ScaleImage(Stream stm) => ScaleImage(stm, new(-1, -1));

    public static Image ScaleImage(Stream stm, Size scale)
    {
        // Since we're not specifying a System.Windows.Media.Imaging.BitmapCacheOption, the pixel format
        // will be System.Windows.Media.PixelFormats.Pbgra32.
        BitmapSource bitmapSource = BitmapFrame.Create(stm, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        return ScaleImage(bitmapSource, scale);
    }

    private static Image ScaleImage(BitmapSource bitmapSource, Size scale)
    {
        // Scale the image so that it will display similarly to the WPF Image.
        var newWidthRatio  = scale.Width  != -1 ? scale.Width                            / (double)bitmapSource.PixelWidth : 1.0;
        var newHeightRatio = scale.Height != -1 ? scale.Width * bitmapSource.PixelHeight / (double)bitmapSource.PixelWidth / bitmapSource.PixelHeight : 1.0;

        BitmapSource transformedBitmapSource = new TransformedBitmap(bitmapSource, new ScaleTransform(newWidthRatio, newHeightRatio));

        var width  = transformedBitmapSource.PixelWidth;
        var height = transformedBitmapSource.PixelHeight;
        var stride = width * ((transformedBitmapSource.Format.BitsPerPixel + 7) / 8);

        var bits = new byte[height * stride];

        transformedBitmapSource.CopyPixels(bits, stride, 0);

        unsafe
        {
            fixed (byte* pBits = bits)
            {
                var bitmap = new Bitmap(width, height, stride, PixelFormat.Format32bppPArgb, new(pBits));
                return bitmap;
            }
        }
    }

    #endregion // ScaleImage
}


[Serializable]
public readonly struct ResourceDetails
{
    private readonly Image _image;

    public ResourceDetails(Image? image, ImageType imageType, string? resourceName)
    {
        _image       = image ?? throw new NullReferenceException();
        ImageType    = imageType;
        ResourceName = resourceName;
    }


    [Description("The horizontal resolution, in pixels per inch, of the image.")]
    [Category("Image Data")]
    public readonly float HorizontalResolution => _image.HorizontalResolution;

    [Description("The type of file the image was stored as in the assembly.")]
    [Category("Resource Data")]
    public ImageType ImageType { get; }

    [Description("The width and height of the image.")]
    [Category("Image Data")]
    public readonly SizeF PhysicalDimension => _image.PhysicalDimension;

    [Description("The pixel format of the image.")]
    [Category("Image Data")]
    public readonly PixelFormat PixelFormat => _image.PixelFormat;

    [Description("The format of the image (an ImageFormat value).")]
    [Category("Image Data")]
    public readonly ImageFormat RawFormat => _image.RawFormat;

    [Description("The name of the resource in the assembly.")]
    [Category("Resource Data")]
    public string? ResourceName { get; }

    [Description("The width and height, in pixels, of the image.")]
    [Category("Image Data")]
    public readonly Size Size => _image.Size;

    [Description("The horizontal resolution, in pixels per inch, of the image.")]
    [Category("Image Data")]
    public readonly float VerticalResolution => _image.VerticalResolution;
}


public enum ImageType
{
    Cursor,
    Icon,
    Image
}
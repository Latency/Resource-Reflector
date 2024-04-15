// *****************************************************************************
// File:       ResourceDetails.cs
// Solution:   ResourceReflector
// Project:    Resource Reflector
// Date:       07/10/2019
// Author:     Latency McLaughlin
// Copywrite:  Bio-Hazard Industries - 1998-2019
// *****************************************************************************

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;

[Serializable]
public struct ResourceDetails {
  #region Data

  private readonly Image _image;

  #endregion Data

  #region Constructor

  public ResourceDetails(Image image, ImageType imageType, string resourceName) {
    _image = image;
    ImageType = imageType;
    ResourceName = resourceName;
  }

  #endregion Constructor

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

  #endregion Properties
}
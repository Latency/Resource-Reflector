// ****************************************************************************
// * Project:  Resource Reflector
// * File:     Form1.cs
// * Author:   Latency McLaughlin
// * Date:     04/19/2014
// ****************************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using Resource_Reflector.Properties;
using ResourceReflector;

namespace Resource_Reflector;

public partial class Form1 : Form
{
    private KeyValuePair<Assembly, DictionaryBindingList<ImageInfo, bool>> _assembly;

    public Form1() => InitializeComponent();

    public Form1(string? assemblyPath) : this()
    {
        if (assemblyPath != null)
            LoadImagesFromAssembly(assemblyPath);

        splitContainer.Panel2Collapsed = true;

        // ReSharper disable once CoVariantArrayConversion
        toolStripComboBox.Items.AddRange(Enum.GetNames(typeof(PictureBoxSizeMode)));
        toolStripComboBox.Items.Remove("AutoSize");
        toolStripComboBox.Text = @"CenterImage";

        toolStripComboBox.Visible       = false;
        toolStripComboSeparator.Visible = false;
    }


    private void ImageGrabberForm_DragEnter(object? sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var filePaths = e.Data.GetData(DataFormats.FileDrop) as string[];

        if (filePaths is not { Length: 1 })
            return;

        LoadImagesFromAssembly(filePaths[0]);

        e.Effect = DragDropEffects.Link;
    }


    private void ImageGrabberForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] strings)
            LoadImagesFromAssembly(strings[0]);
    }


    private void CloseToolStripButton_Click(object? sender, EventArgs e) => PerformClose();

    private void openToolStripButton_Click(object? sender, EventArgs e) => PerformOpen();

    private void saveAllToolStripButton_Click(object? sender, EventArgs e) => PerformSaveAll();

    private void saveToolStripButton_Click(object? sender, EventArgs e) => PerformSave();
    
    private void copyToolStripButton_Click(object? sender, EventArgs e) => PerformCopy();

    private void propertiesToolStripButton_Click(object? sender, EventArgs e) => PerformViewHideProperties();

    private void toolStripComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (Created)
            pictureBox.SizeMode = (PictureBoxSizeMode)Enum.Parse(typeof(PictureBoxSizeMode), toolStripComboBox.Text);
    }


    private void bindingSource_ListChanged(object? sender, ListChangedEventArgs e)
    {
        if (e.ListChangedType != ListChangedType.Reset)
            return;

        bindingSource.Position = 0;

        var imagesExist = bindingSource.Count > 0;

        closeToolStripButton.Enabled      = imagesExist;
        copyToolStripButton.Enabled       = imagesExist;
        saveAllToolStripButton.Enabled    = imagesExist;
        saveToolStripButton.Enabled       = imagesExist;
        toolStripComboBox.Enabled         = imagesExist;
        propertiesToolStripButton.Enabled = imagesExist;
        tabControl.Enabled                = imagesExist;
        propertyGrid.Enabled              = imagesExist;

        if (propertyGrid.DataBindings.Count != 0)
            return;

        pictureBox.DataBindings.Add("Image", bindingSource, "Image");
        propertyGrid.DataBindings.Add("SelectedObject", bindingSource, "ResourceDetails");

        dataGridView.DataSource = bindingSource;

        try
        {
            dataGridView.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView.Columns[1].Visible      = false;
        }
        catch
        {
            #if DEBUG
            MessageBox.Show(@"Column count != 2 in ListChanged region.");
            #endif
        }
    }


    private void menuItemCopy_Click(object? sender, EventArgs e) => PerformCopy();

    private void menuItemProperties_Click(object? sender, EventArgs e) => PerformViewHideProperties();

    private void menuItemSave_Click(object? sender, EventArgs e) => PerformSave();

    private void menuItemSaveAll_Click(object? sender, EventArgs e) => PerformSaveAll();

    private void dataGridView_MouseDown(object? sender, MouseEventArgs e)
    {
        // To ensure that the user is going to save/copy the image under the cursor, make the row under the cursor the current item in the binding source.
        if (e.Button == MouseButtons.Right)
        {
            var info = dataGridView.HitTest(e.X, e.Y);
            if (info.RowIndex > -1)
                bindingSource.Position = info.RowIndex;
        }
    }


    private void tabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        toolStripComboBox.Visible       = tabControl.SelectedTab == tabPageIndividualImage;
        toolStripComboSeparator.Visible = tabControl.SelectedTab == tabPageIndividualImage;
    }


    private static bool CreateDirectory(string path)
    {
        try
        {
            // Determine whether the directory exists. 
            if (Directory.Exists(path))
                return true;
            // Try to create the directory.
            Directory.CreateDirectory(path);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }


    private static string GetExtension(ImageInfo imageInfo) => imageInfo.ResourceDetails.ImageType switch
    {
        ImageType.Icon   => ".ico",
        ImageType.Cursor => " .cur",
        ImageType.Image  => imageInfo.Image?.Tag?.ToString() == ".wav" ? ".wav" : imageInfo.Image?.RawFormat.Guid == ImageFormat.Jpeg.Guid ? ".jpg" : imageInfo.Image?.RawFormat.Guid == ImageFormat.Gif.Guid ? ".gif" : imageInfo.Image?.RawFormat.Guid == ImageFormat.Png.Guid ? ".png" : ".bmp",
        _                => throw new ArgumentOutOfRangeException()
    };


    private static string GetFilter(string extension)
    {
        var name = extension switch
        {
            ".cur" => "Cursor",
            ".ico" => "Icon",
            ".png" => "Portable Network Graphic",
            ".jpg" => "Joint Photographic Experts Group",
            ".bmp" => "Bitmap",
            ".wav" => "Waveform Audio File Format",
            ".gif" => "Graphics Interchange Format",
            _ => "Unknown",
        };
        return string.Format("{0} (*{1})|*{1}", name, extension);
    }


    public static Dictionary<ImageInfo, bool> ExtractImagesFromAssembly(Assembly assembly)
    {
        var imageInfos = new Dictionary<ImageInfo, bool>();

        foreach (var name in assembly.GetManifestResourceNames())
        {
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null)
                continue;
            Console.WriteLine(name);

            // Try to exstract the resource as an icon.
            try
            {
                var icon = new Icon(stream);
                imageInfos.Add(new(icon, name), false);
                continue;
            }
            catch (ArgumentException)
            {
                stream.Position = 0;
            }

            // Try to exstract the resource as an image.
            try
            {
                var image    = Image.FromStream(stream);
                var frameDim = new FrameDimension(image.FrameDimensionsList[0]);
                imageInfos.Add(new(image, name), image.GetFrameCount(frameDim) > 1); // Flag true if .gif
                continue;
            }
            catch (ArgumentException)
            {
                stream.Position = 0;
            }

            // Try to extract the resource as an resource file.  (audio, other, etc.)
            try
            {
                // The embedded resource in the stream is not an image, so read it into a ResourceReader and extract the values from there.
                using IResourceReader reader = new ResourceReader(stream);
                foreach (DictionaryEntry entry in reader)
                    switch (entry.Value)
                    {
                        case System.Drawing.Icon or Image:
                            imageInfos.Add(new(entry.Value, entry.Key.ToString()), false);
                            break;
                        // Load an ImageList with the ImageListStreamer and store a reference to every image it contains.
                        case ImageListStreamer streamer:
                        {
                            using var imageList = new ImageList();
                            imageList.ImageStream = streamer;
                            for (var idx = 0; idx < imageList.Images.Count; idx++)
                            {
                                // Save node name.  - "AssemblyOfImages.Form1.resources"
                                //  Add key name + strip type name.
                                var key      = entry.Key.ToString();
                                var typeName = key.Remove(key.LastIndexOf('.'));
                                imageInfos.Add(new(imageList.Images[idx], $"{typeName}_{idx:D2}"), false);
                            }
                            break;
                        }
                    }

                continue;
                // ReSharper disable once EmptyGeneralCatchClause
            }
            catch (Exception)
            {
                stream.Position = 0;
            }

            try
            {
                var buffer = new byte[4];
                if (stream.Read(buffer, 0, 4) == 4 && buffer.SequenceEqual(new byte[] {
                        0x52,
                        0x49,
                        0x46,
                        0x46
                    }))
                {
                    var image = Resources.Wav;
                    image.Tag = ".wav";
                    imageInfos.Add(new(image, name), false);
                }
            }
            catch (ArgumentException)
            {
                stream.Position = 0;
            }
        }

        return imageInfos;
    }


    private void LoadImagesFromAssembly(string assemblyPath)
    {
        PerformClose();

        // Try to load the assembly at the specified locatiassemblyPathon.
        try
        {
            var manager = Assembly.ReflectionOnlyLoadFrom(assemblyPath);

            // using (var manager = new AssemblyReflectionManager()) {
            //if (!manager.LoadAssembly(assemblyPath, Settings.Default.Domain_Name)) {
            //  MessageBox.Show(String.Format("Unable to load assembly '{0}'", assemblyPath));
            //  return;
            //}

            _assembly = new(manager, ExtractImagesFromAssembly(manager).ToBindingList());

            //Func<Assembly, KeyValuePair<AssemblyInfo, DictionaryBindingList<ImageInfo, bool>>> del =
            //  asm => new KeyValuePair<AssemblyInfo, DictionaryBindingList<ImageInfo, bool>>(new AssemblyInfo(asm), );

            //_assembly = del(manager);

            //   var kvp = manager.Reflect(assemblyPath, asm => new KeyValuePair<AssemblyInfo, DictionaryBindingList<ImageInfo, bool>>(new AssemblyInfo(asm), ExtractImagesFromAssembly(asm).ToBindingList()));

            toolStripStatusLabel.Text        = _assembly.Key.FullName;
            toolStripStatusLabel.ToolTipText = _assembly.Key.Location;

            var dictBindingList = _assembly.Value;
            // Bind to a list of every image embedded in the assembly.
            if (dictBindingList.Count == 0)
                MessageBox.Show(@"The assembly does not have any embedded resources.", @"Assembly Analysis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                bindingSource.DataSource = dictBindingList.Select(x => x.Key).ToList();
            // }
        }
        catch (Exception)
        {
            MessageBox.Show(@"The specified file is not a .NET assembly.", @"Assembly Analysis", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private void PerformCopy()
    {
        if (pictureBox.Image != null)
            Clipboard.SetImage(pictureBox.Image);
    }


    private void PerformClose()
    {
        switch (bindingSource.DataSource)
        {
            // Dispose of the images currently being displayed, if any.
            case null:
                return;
            case List<ImageInfo> imageInfos:
            {
                foreach (var imgInfo in imageInfos)
                    imgInfo.Dispose();
                break;
            }
        }

        _assembly.Value?.Clear();
        dataGridView.Rows.Clear();
        // Update the text/tooltip in the StatusStrip to the new assembly info.
        toolStripStatusLabel.Text = toolStripStatusLabel.ToolTipText = string.Empty;
    }


    private void PerformOpen()
    {
        using var dlg = new OpenFileDialog();
        dlg.Filter = @"Assemblies (*.exe, *.dll)|*.exe;*.dll";
        if (dlg.ShowDialog() == DialogResult.OK)
            LoadImagesFromAssembly(dlg.FileName);
    }


    private void PerformSave()
    {
        using var dlg = new SaveFileDialog();
        if (bindingSource.Current is ImageInfo imageInfo)
        {
            dlg.Filter = GetFilter(GetExtension(imageInfo));
            if (dlg.ShowDialog() == DialogResult.OK)
                SaveResource(imageInfo, dlg.FileName);
        }
        else
        {
            MessageBox.Show(@"Unable to save file.", @"Save Resource", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private void PerformSaveAll()
    {
        using var fbd = new FolderBrowserDialog();
        // Show the FolderBrowserDialog.
        if (fbd.ShowDialog() == DialogResult.OK)
            SaveAllResources(fbd.SelectedPath);
    }


    private void PerformViewHideProperties()
    {
        splitContainer.Panel2Collapsed = !splitContainer.Panel2Collapsed;

        var text = splitContainer.Panel2Collapsed ? "View Properties" : "Hide Properties";

        propertiesToolStripButton.ToolTipText = text;
        menuItemProperties.Text               = text;

        // If the tab control does not repaint now, a section of it will not be
        // drawn properly.
        tabControl.Refresh();
    }


    private static void WriteStream(Stream? stream, string fileName)
    {
        if (stream == null)
            return;

        using var fileStream = new FileStream(fileName, FileMode.Create);
        int       i;
        while ((i = stream.ReadByte()) > -1)
            fileStream.WriteByte((byte)i);
    }


    private void SaveResource(ImageInfo imageInfo, string fileName)
    {
        var extension = string.Empty;
        if (fileName.Length > 4)
            extension = fileName.Substring(fileName.Length - 4, 4);

        try
        {
            switch (extension)
            {
                #region Icon

                case ".ico":
                    // If, for some bizarre reason, someone tries to save a bitmap as an icon, ignore that request and just save it as a bitmap.
                    if (imageInfo.SourceObject is Icon == false)
                        goto default;

                    using (var stream = new FileStream(fileName, FileMode.Create))
                    {
                        ((Icon)imageInfo.SourceObject).Save(stream);
                    }

                    break;

                #endregion // Icon

                #region Cursor

                case ".cur":
                    // If, for some bizarre reason, someone tries to save a bitmap as a cursor, ignore that request and just save it as a bitmap.
                    if (imageInfo.SourceObject is Cursor == false)
                        goto default;
                    goto Write_Stream;

                #endregion // Cursor

                #region Wav & GIF

                case ".gif":
                case ".wav":
                Write_Stream:
                    try
                    {
                        // Select any assembly out of the collection (since they are all the same) and obtain it's information.
                        var asm = _assembly.Key;
                        // Copy the cursor byte-by-byte out of the assembly into a file.
                        using var stream = asm.GetManifestResourceStream(imageInfo.ResourceDetails.ResourceName);
                        if (stream != null)
                            WriteStream(stream, fileName);
                    }
                    catch (Exception ex)
                    {
                        // Resource failed to be found or had a problem loading.
                        // Try the next loaded assembly.  Although, we only load 1x assembly at a time for now so there is only one iteration.
                        MessageBox.Show(ex.Message, @"Save Resource");
                    }

                    break;

                #endregion // Wav & GIF

                default:

                    #region Bitmap

                    // Copy the image to a new bitmap or else an error can occur while saving.
                    using (var bmp = new Bitmap(imageInfo.Image!))
                    {
                        bmp.Save(fileName);
                    }

                    break;

                #endregion // Bitmap
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(@"An exception was thrown while trying to save the image: " + ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private void SaveAllResources(string folderName)
    {
        var     failures = new Dictionary<string, string>();
        var     cnt      = 0;
        var last     = string.Empty;
        foreach (var item in (List<ImageInfo>)bindingSource.DataSource)
        {
            var     path = folderName;
            var file = item.ResourceDetails.ResourceName;
            file = file!.Remove(0, file.IndexOf('.') + 1);
            var ext    = GetExtension(item);
            var filter = GetFilter(ext);
            path += "\\" + filter.Remove(filter.IndexOf(' '));
            if (!CreateDirectory(path))
                failures.Add(file + ext, path);

            var index = file.LastIndexOf(".resources", StringComparison.Ordinal);
            if (index > 0)
                file = file.Remove(index);
            if (file != last)
                cnt = 0;
            last = file;
            file = last + $"-{++cnt:D2}";
            SaveResource(item, path + "\\" + file + ext);
        }

        if (failures.Count <= 0)
            return;

        var list = string.Empty;
        var idx  = 0;
        list = failures.Aggregate(list, (current, kvp) => current + string.Format("{2:D2} - {1}\\{0}\r\n", kvp.Key, kvp.Value, ++idx));
        MessageBox.Show($@"Unable to save the following items: {list}", @"Save Failures", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
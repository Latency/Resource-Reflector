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
using ImageGrabber.Properties;
using Clipboard = System.Windows.Forms.Clipboard;
using DataFormats = System.Windows.Forms.DataFormats;
using DragDropEffects = System.Windows.Forms.DragDropEffects;
using DragEventArgs = System.Windows.Forms.DragEventArgs;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ImageGrabber {
  public partial class ImageGrabberForm : Form {
    #region Data

    private KeyValuePair<Assembly, DictionaryBindingList<ImageInfo, bool>> _assembly;

    #endregion // Data

    #region Constructors

    public ImageGrabberForm() {
      InitializeComponent();
    }

    public ImageGrabberForm(string assemblyPath) : this() {
      if (assemblyPath != null)
        LoadImagesFromAssembly(assemblyPath);

      splitContainer.Panel2Collapsed = true;

      // ReSharper disable once CoVariantArrayConversion
      toolStripComboBox.Items.AddRange(Enum.GetNames(typeof(PictureBoxSizeMode)));
      toolStripComboBox.Items.Remove("AutoSize");
      toolStripComboBox.Text = @"CenterImage";

      toolStripComboBox.Visible = false;
      toolStripComboSeparator.Visible = false;
    }

    #endregion // Constructors

    #region Event Handlers

    #region Drag-Drop

    #region DragEnter

    private void ImageGrabberForm_DragEnter(object sender, DragEventArgs e) {
      if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        return;

      var filePaths = e.Data.GetData(DataFormats.FileDrop) as string[];

      if (filePaths == null || filePaths.Length != 1)
        return;

      LoadImagesFromAssembly(filePaths[0]);

      e.Effect = DragDropEffects.Link;
    }

    #endregion // DragEnter

    #region DragDrop

    private void ImageGrabberForm_DragDrop(object sender, DragEventArgs e) {
      var strings = e.Data.GetData(DataFormats.FileDrop) as string[];
      if (strings != null)
        LoadImagesFromAssembly(strings[0]);
    }

    #endregion // DragDrop

    #endregion // Drag-Drop

    #region ToolStrip Commands

    #region Close

    private void closeToolStripButton_Click(object sender, EventArgs e) {
      PerformClose();
    }

    #endregion // Close

    #region Open

    private void openToolStripButton_Click(object sender, EventArgs e) {
      PerformOpen();
    }

    #endregion // Open

    #region SaveAll

    private void saveAllToolStripButton_Click(object sender, EventArgs e) {
      PerformSaveAll();
    }

    #endregion // SaveAll

    #region Save

    private void saveToolStripButton_Click(object sender, EventArgs e) {
      PerformSave();
    }

    #endregion // Save

    #region Copy

    private void copyToolStripButton_Click(object sender, EventArgs e) {
      PerformCopy();
    }

    #endregion // Copy

    #region View/Hide Properties

    private void propertiesToolStripButton_Click(object sender, EventArgs e) {
      PerformViewHideProperties();
    }

    #endregion // View/Hide Properties

    #region SizeMode

    private void toolStripComboBox_SelectedIndexChanged(object sender, EventArgs e) {
      if (Created)
        pictureBox.SizeMode = (PictureBoxSizeMode) Enum.Parse(typeof(PictureBoxSizeMode), toolStripComboBox.Text);
    }

    #endregion // SizeMode

    #endregion // ToolStrip Commands

    #region BindingSource

    #region ListChanged

    private void bindingSource_ListChanged(object sender, ListChangedEventArgs e) {
      if (e.ListChangedType != ListChangedType.Reset)
        return;

      bindingSource.Position = 0;

      var imagesExist = bindingSource.Count > 0;

      closeToolStripButton.Enabled = imagesExist;
      copyToolStripButton.Enabled = imagesExist;
      saveAllToolStripButton.Enabled = imagesExist;
      saveToolStripButton.Enabled = imagesExist;
      toolStripComboBox.Enabled = imagesExist;
      propertiesToolStripButton.Enabled = imagesExist;
      tabControl.Enabled = imagesExist;
      propertyGrid.Enabled = imagesExist;

      if (propertyGrid.DataBindings.Count == 0) {
        pictureBox.DataBindings.Add("Image", bindingSource, "Image");
        propertyGrid.DataBindings.Add("SelectedObject", bindingSource, "ResourceDetails");

        dataGridView.DataSource = bindingSource;

        try {
          dataGridView.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
          dataGridView.Columns[1].Visible = false;
        } catch {
#if DEBUG
          MessageBox.Show(@"Column count != 2 in ListChanged region.");
#endif
        }
      }
    }

    #endregion // ListChanged

    #endregion // BindingSource

    #region ContextMenuStrip Commands

    private void menuItemCopy_Click(object sender, EventArgs e) {
      PerformCopy();
    }

    private void menuItemProperties_Click(object sender, EventArgs e) {
      PerformViewHideProperties();
    }

    private void menuItemSave_Click(object sender, EventArgs e) {
      PerformSave();
    }

    private void menuItemSaveAll_Click(object sender, EventArgs e) {
      PerformSaveAll();
    }

    #endregion // ContextMenuStrip Commands

    #region Misc

    #region dataGridView_MouseDown

    private void dataGridView_MouseDown(object sender, MouseEventArgs e) {
      // To ensure that the user is going to save/copy the image under the cursor, 
      // make the row under the cursor the current item in the binding source.
      if (e.Button == MouseButtons.Right) {
        DataGridView.HitTestInfo info = dataGridView.HitTest(e.X, e.Y);
        if (info.RowIndex > -1)
          bindingSource.Position = info.RowIndex;
      }
    }

    #endregion // dataGridView_MouseDown

    #region tabControl_SelectedIndexChanged

    private void tabControl_SelectedIndexChanged(object sender, EventArgs e) {
      toolStripComboBox.Visible = tabControl.SelectedTab == tabPageIndividualImage;
      toolStripComboSeparator.Visible = tabControl.SelectedTab == tabPageIndividualImage;
    }

    #endregion // tabControl_SelectedIndexChanged

    #endregion // Misc

    #endregion // Event Handlers

    #region Private Helpers

    #region CreateDirectory

    private static bool CreateDirectory(string path) {
      try {
        // Determine whether the directory exists. 
        if (Directory.Exists(path))
          return true;
        // Try to create the directory.
        Directory.CreateDirectory(path);
      } catch (Exception) {
        return false;
      }
      return true;
    }

    #endregion // CreateDirectory

    #region GetExtension

    private static string GetExtension(ImageInfo imageInfo) {
      switch (imageInfo.ResourceDetails.ImageType) {
        case ImageType.Icon:
          return ".ico";
        case ImageType.Cursor:
          return " .cur";
        default:
          return imageInfo.Image.Tag.ToString() == ".wav"
            ? ".wav"
            : (imageInfo.Image.RawFormat.Guid == ImageFormat.Jpeg.Guid
              ? ".jpg"
              : (imageInfo.Image.RawFormat.Guid == ImageFormat.Gif.Guid
                ? ".gif"
                : (imageInfo.Image.RawFormat.Guid == ImageFormat.Png.Guid
                  ? ".png"
                  : ".bmp")));
      }
    }

    #endregion GetExtension

    #region GetFilter

    private static string GetFilter(string extension) {
      string name;
      switch (extension) {
        case ".cur":
          name = "Cursor";
          break;
        case ".ico":
          name = "Icon";
          break;
        case ".png":
          name = "Portable Network Graphic";
          break;
        case ".jpg":
          name = "Joint Photographic Experts Group";
          break;
        case ".bmp":
          name = "Bitmap";
          break;
        case ".wav":
          name = "Waveform Audio File Format";
          break;
        case ".gif":
          name = "Graphics Interchange Format";
          break;
        default:
          name = "Unknown";
          break;
      }
      return string.Format("{0} (*{1})|*{1}", name, extension);
    }

    #endregion // GetFilter

    #region ExtractImagesFromAssembly

    public static Dictionary<ImageInfo, bool> ExtractImagesFromAssembly(Assembly assembly) {
      var imageInfos = new Dictionary<ImageInfo, bool>();

      foreach (var name in assembly.GetManifestResourceNames()) {
        using (var stream = assembly.GetManifestResourceStream(name)) {
          if (stream == null)
            continue;
          Console.WriteLine(name);
          #region Icon

          // Try to exstract the resource as an icon.
          try {
            var icon = new Icon(stream);
            imageInfos.Add(new ImageInfo(icon, name), false);
            continue;
          } catch (ArgumentException) {
            stream.Position = 0;
          }

          #endregion // Icon

          #region Cursor

          // Try to exstract the resource as an cursor.
          try {
            var cursor = new Cursor(stream);
            imageInfos.Add(new ImageInfo(cursor, name), false);
            continue;
          } catch (ArgumentException) {
            stream.Position = 0;
          }

          #endregion // Cursor

          #region Image

          // Try to exstract the resource as an image.
          try {
            var image = Image.FromStream(stream);
            var frameDim = new FrameDimension(image.FrameDimensionsList[0]);
            imageInfos.Add(new ImageInfo(image, name), image.GetFrameCount(frameDim) > 1); // Flag true if .gif
            continue;
          } catch (ArgumentException) {
            stream.Position = 0;
          }

          #endregion // Image

          #region Resource File

          // Try to exstract the resource as an resource file.  (audio, other, etc.)
          try {
            // The embedded resource in the stream is not an image, so read it into a ResourceReader and extract the values from there.
            using (IResourceReader reader = new ResourceReader(stream)) {
              foreach (DictionaryEntry entry in reader) {
                if (entry.Value is Icon || entry.Value is Image)
                  imageInfos.Add(new ImageInfo(entry.Value, name), false);
                else if (entry.Value is ImageListStreamer) {
                  // Load an ImageList with the ImageListStreamer and store a reference to every image it contains.
                  using (var imageList = new ImageList()) {
                    imageList.ImageStream = entry.Value as ImageListStreamer;
                    for (var idx = 0; idx < imageList.Images.Count; idx++) {
                      // Save node name.  - "AssemblyOfImages.Form1.resources"
                      //  Add key name + strip type name.
                      var key = entry.Key.ToString();
                      var typeName = key.Remove(key.LastIndexOf('.'));
                      imageInfos.Add(new ImageInfo(imageList.Images[idx], String.Format("{0}_{1:D2}", typeName, idx)), false);
                    }
                  }
                }
              }
            }
            continue;
            // ReSharper disable once EmptyGeneralCatchClause
          } catch (Exception) {
            stream.Position = 0;
          }

          #region Audio

          try {
            var buffer = new byte[4];
            if (stream.Read(buffer, 0, 4) == 4 && buffer.SequenceEqual(new byte[] {0x52, 0x49, 0x46, 0x46})) {
              var image = Resources.Wav;
              image.Tag = ".wav";
              imageInfos.Add(new ImageInfo(image, name), false);
            }
            continue;
          } catch (ArgumentException) {
            stream.Position = 0;
          }

          #endregion // Audio

          #endregion // Resource File
        }
      }

      return imageInfos;
    }

    #endregion // ExtractImagesFromAssembly

    #region LoadImagesFromAssembly

    private void LoadImagesFromAssembly(string assemblyPath) {
      PerformClose();

      // Try to load the assembly at the specified locatiassemblyPathon.
      try {
        Assembly manager = Assembly.ReflectionOnlyLoadFrom(assemblyPath);

      // using (var manager = new AssemblyReflectionManager()) {
          //if (!manager.LoadAssembly(assemblyPath, Settings.Default.Domain_Name)) {
          //  MessageBox.Show(String.Format("Unable to load assembly '{0}'", assemblyPath));
          //  return;
          //}

        _assembly = new KeyValuePair<Assembly, DictionaryBindingList<ImageInfo, bool>>(manager, ExtractImagesFromAssembly(manager).ToBindingList());

        //Func<Assembly, KeyValuePair<AssemblyInfo, DictionaryBindingList<ImageInfo, bool>>> del =
        //  asm => new KeyValuePair<AssemblyInfo, DictionaryBindingList<ImageInfo, bool>>(new AssemblyInfo(asm), );

        //_assembly = del(manager);

        //   var kvp = manager.Reflect(assemblyPath, asm => new KeyValuePair<AssemblyInfo, DictionaryBindingList<ImageInfo, bool>>(new AssemblyInfo(asm), ExtractImagesFromAssembly(asm).ToBindingList()));

        toolStripStatusLabel.Text = _assembly.Key.FullName;
        toolStripStatusLabel.ToolTipText = _assembly.Key.Location;

        var dictBindingList = _assembly.Value;
        // Bind to a list of every image embedded in the assembly.
        if (dictBindingList.Count == 0)
          MessageBox.Show(@"The assembly does not have any embedded resources.", @"Assembly Analysis", MessageBoxButtons.OK, MessageBoxIcon.Information);
        else
          bindingSource.DataSource = dictBindingList.Select(x => x.Key).ToList();
    // }
      } catch (Exception) {
        MessageBox.Show(@"The specified file is not a .NET assembly.", @"Assembly Analysis", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    #endregion // LoadImagesFromAssembly

    #region PerformXXX

    private void PerformCopy() {
      if (pictureBox.Image != null)
        Clipboard.SetImage(pictureBox.Image);
    }

    private void PerformClose() {
      // Dispose of the images currently being displayed, if any.
      if (bindingSource.DataSource == null)
        return;
      var imageInfos = bindingSource.DataSource as List<ImageInfo>;
      if (imageInfos != null) {
        foreach (var imgInfo in imageInfos)
          imgInfo.Dispose();
      }
      if (_assembly.Value != null)
        _assembly.Value.Clear();
      dataGridView.Rows.Clear();
      // Update the text/tooltip in the StatusStrip to the new assembly info.
      toolStripStatusLabel.Text = toolStripStatusLabel.ToolTipText = String.Empty;
    }

    private void PerformOpen() {
      using (var dlg = new OpenFileDialog()) {
        dlg.Filter = @"Assemblies (*.exe, *.dll)|*.exe;*.dll";
        if (dlg.ShowDialog() == DialogResult.OK)
          LoadImagesFromAssembly(dlg.FileName);
      }
    }

    private void PerformSave() {
      using (var dlg = new SaveFileDialog()) {
        var imageInfo = bindingSource.Current as ImageInfo;
        if (imageInfo != null) {
          dlg.Filter = GetFilter(GetExtension(imageInfo));
          if (dlg.ShowDialog() == DialogResult.OK)
            SaveResource(imageInfo, dlg.FileName);
        } else
          MessageBox.Show(@"Unable to save file.", @"Save Resource", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void PerformSaveAll() {
      using (var fbd = new FolderBrowserDialog()) {
        // Show the FolderBrowserDialog.
        if (fbd.ShowDialog() == DialogResult.OK)
          SaveAllResources(fbd.SelectedPath);
      }
    }

    private void PerformViewHideProperties() {
      splitContainer.Panel2Collapsed = !splitContainer.Panel2Collapsed;

      var text = splitContainer.Panel2Collapsed
        ? "View Properties"
        : "Hide Properties";

      propertiesToolStripButton.ToolTipText = text;
      menuItemProperties.Text = text;

      // If the tab control does not repaint now, a section of it will not be
      // drawn properly.
      tabControl.Refresh();
    }

    #endregion // PerformXXX

    #region WriteStream

    private static void WriteStream(Stream stream, string fileName) {
      if (stream == null)
        return;
      using (var fileStream = new FileStream(fileName, FileMode.Create)) {
        int i;
        while ((i = stream.ReadByte()) > -1)
          fileStream.WriteByte((byte) i);
      }
    }

    #endregion // WriteStream

    #region SaveResource

    private void SaveResource(ImageInfo imageInfo, string fileName) {
      var extension = String.Empty;
      if (fileName.Length > 4)
        extension = fileName.Substring(fileName.Length - 4, 4);

      try {
        switch (extension) {
          #region Icon

          case ".ico":
            // If, for some bizarre reason, someone tries to save a bitmap as an icon, ignore that request and just save it as a bitmap.
            if (imageInfo.SourceObject is Icon == false)
              goto default;

            using (var stream = new FileStream(fileName, FileMode.Create))
              (imageInfo.SourceObject as Icon).Save(stream);
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
             try {
                // Select any assembly out of the collection (since they are all the same) and obtain it's information.
                var asm = _assembly.Key;
                // Copy the cursor byte-by-byte out of the assembly into a file.
                using (var stream = asm.GetManifestResourceStream(imageInfo.ResourceDetails.ResourceName))
                  if (stream != null)
                    WriteStream(stream, fileName);
                break;
              } catch (Exception ex) {
                // Resource failed to be found or had a problem loading.
                // Try the next loaded assembly.  Although, we only load 1x assembly at a time for now so there is only one iteration.
                MessageBox.Show(ex.Message, @"Save Resource");
              }
             break;

           #endregion // Wav & GIF

           default:
             #region Bitmap

            // Copy the image to a new bitmap or else an error can occur while saving.
            using (var bmp = new Bitmap(imageInfo.Image))
              bmp.Save(fileName);
            break;

            #endregion // Bitmap
        }
      } catch (Exception ex) {
        MessageBox.Show(@"An exception was thrown while trying to save the image: " + ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    #endregion // SaveResource

    #region SaveAllResources

    private void SaveAllResources(string folderName) {
      var failures = new Dictionary<string, string>();
      var cnt = 0;
      var last = String.Empty;
      foreach (var kvp in (Dictionary<ImageInfo, bool>) bindingSource.DataSource) {
        var item = kvp.Key;
        var path = folderName;
        var file = item.ResourceDetails.ResourceName;
        file = file.Remove(0, file.IndexOf('.') + 1);
        var ext = GetExtension(item);
        var filter = GetFilter(ext);
        path += "\\" + filter.Remove(filter.IndexOf(' '));
        if (!CreateDirectory(path))
          failures.Add(file + ext, path);

        file = file.Remove(file.LastIndexOf(".resources", StringComparison.Ordinal));
        if (file != last)
          cnt = 0;
        last = file;
        file = last + String.Format("-{0:D2}", ++cnt);
        SaveResource(item, path + "\\" + file + ext);
      }

      if (failures.Count <= 0)
        return;

      var list = String.Empty;
      var idx = 0;
      list = failures.Aggregate(list, (current, kvp) => current + String.Format("{2:D2} - {1}\\{0}\r\n", kvp.Key, kvp.Value, ++idx));
      MessageBox.Show(String.Format("Unable to save the following items:\n{0}", list), @"Save Failures", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    #endregion // SaveAllResources

    #endregion // Private Helpers
  }
}
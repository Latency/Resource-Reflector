Resource-Reflector
==================

A .NET application written in C# for viewing and extracting assembly resources.

<p><img height=391 alt="Sample image" src="http://bio-hazard.cx/ResourceReflector/images/ResourceReflector.jpg" width=600></p>
<ul>
  <li><a href="#introduction">Introduction</a>
  <li><a href="#background">Background</a>
  <li><a href="#using">Using the code</a>
  <li><a href="#other">Other Features</a>
  <li><a href="#references">References</a> </li>
  <li><a href="#license">License</a> </li>
</ul>
<a name="introduction"><h2>Introduction</h2></a>
This article examines a simple utility application, Embedded Image Grabber, which allows you to view, save, and copy images, icons, and cursors embedded in an assembly. The utility was compiled against v2.0 of the .NET framework, but the core functionality could easily be ported to v1.x, if necessary.

<a name="background"><h2>Background</h2></a>
Before looking at how Embedded Image Grabber works, let's take a moment to review what an embedded resource is. When an assembly is created, it is possible to store arbitrary files within it, such as BMPs, XML files, etc. Those files are called embedded resources. Embedding a resource within an assembly has several benefits, such as:

Simplifying deployment (less files to manage).
Simplifying resource consumption (there is no chance that the file will be missing at runtime).
You can easily embed an image into an assembly using Visual Studio .NET, by following these steps:

Add an image file to a project.
In Solution Explorer, right click on the image file and select Properties from the context menu.
In the Properties window, select Embedded Resource for the Build Action property.
Compile the project into an assembly.
As you might imagine, the .NET framework provides support for programmatic retrieval of embedded resources. We will be examining how that is implemented, later in the article.

<a name="using"><h2>Using the utility</h2></a>
There are four essential steps to using this tool:

<ol>
<li>Run Embedded Image Grabber.exe.</li>
<li>Click the Open button to locate the assembly which contains the image(s) you want to extract.</li>
<li>Navigate to the image(s) you are interested in, via the BindingNavigator at the top of the window.</li>
<li>Click either the Save or Copy button to persist an image to the disk or clipboard.</li>
</ol>
* Tip - Steps 1 and 2 can be consolidated by simply drag-dropping the target assembly onto Embedded Image Grabber.exe.

<a name="other"><h2>Other features</h2></a>
<ul>
<li>Save Options</li> When saving an embedded icon or cursor, you have the option of saving it either as the original type of file or as a bitmap. The Save As dialog will default to using the extension which corresponds to the original type of the embedded resource.
<li>Open via Drag-Drop</li> In addition to being able to open the application with an assembly loaded by drag-dropping the assembly onto Embedded Image Grabber.exe, you can also load an assembly while the app is running, via drag-drop. Simply drop an assembly onto the form, and the embedded images it contains will be loaded.
<li>'All Images' tab</li> Provides a grid view of every image in the assembly. It makes searching for an image faster.
<li>Properties View</li> A PropertyGrid which displays detailed information about the current image. Click the rightmost button on the toolbar to show/hide this view.
<li>Context Menu</li> Provides quick access to save, copy, or show/hide properties of an image.
<li>View Options</li> When the 'Individual Image' tab is selected, the toolbar will have a combobox which allows you to alter the way that the current image is rendered (such as zoomed, centered, etc.).
</ul>

<a name="references"><h2>References</h2></a>
<p>John Smith, 3 Apr 2006 - Extracting Embedded Images From An Assembly</p>
<a name="license"><h2>License</h2></a>
<div id="LicenseTerms">
  <p>
    <a href="http://www.gnu.org/copyleft/gpl.html">GNU LESSER GENERAL PUBLIC LICENSE</a>
    Version 3, 29 June 2007
  </p>
</div>
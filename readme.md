# Todo.txt for Windows Phone 7

## Building The Project

First off, you're going to need to have the [Windows Phone 7 SDK](http://msdn.microsoft.com/en-us/library/ff402530%28v=vs.92%29.aspx).

Once you've got that, grab the latest version of the source code. This application has two submodules that you'll also need to grab - the easy way to do that is to open up git bash in the top-level folder of the source and run the following commands:

	git submodule init

	git submodule update

Then open the solution in Visual Studio and build. The source includes a copy of [NuGetPowerTools](https://github.com/davidfowl/NuGetPowerTools) and the projects are set up to automatically grab all of the necessary NuGet packages. 

To test out the Dropox sync functionality, you'll need a [developer API key from Dropbox]. Put this key in a file called 'apikeys.txt' in the main folder of the application and set its build action to 'Resource'. The file is in JSON object format, like this:

	{"dropboxkey":"...","dropboxsecret":"..."}
	

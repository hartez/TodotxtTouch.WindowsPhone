# Todo.txt for Windows Phone 7

## Building The Project

First off, you're going to need to have the [Windows Phone 7 SDK](http://msdn.microsoft.com/en-us/library/ff402530%28v=vs.92%29.aspx).

To test out the Dropox sync functionality, you'll need a [developer API key from Dropbox](http://www.dropbox.com/developers/quickstart). Put this key in the file called 'apikeys.txt' in the main folder of the application and set its build action to 'Resource'. The file is in JSON object format, like this:

	{"dropboxkey":"...","dropboxsecret":"..."}
	

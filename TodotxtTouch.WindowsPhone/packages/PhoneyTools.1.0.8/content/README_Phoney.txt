Welcome to PhoneyTools 1.0

Your project now has a reference to three assemblies:

- AgiliTrain.PhoneyTools:            The main PhoneyTools classes.  
- AgiliTrain.PhoneyTools.Net:        PhoneyTools Networking classes.
- AgiliTrain.PhoneyTools.Microphone: PhoneyTool Microphone class.

If you keep all three in your project you'll automatically add
the following capacities to your application:

ID_CAP_MICROPHONE
ID_CAP_NETWORKING

If you don't need the Microphone, remove that reference (to remove 
the Microphone capability) and if you don't need networking remove that 
reference (to remove the networking capability).

The classes supported in Phoney Tools include:

Controls
- FadingMessage class
- SelectSwitch Control
- SimpleLongListSelector Control
Other Classes
- BitlyHelper class
- GravatarHelper class
- MD5Managed class
- InputScopeHelper
- InputScopeProvider
- InputScopeRequirements
- PhoneLogger class
- ObservableObject class
- ObstructionDetector class
- Phone Resources classes
- Converters
- GameTimer class
- MicrophoneRecorder class
- SoundEffectPlayer class
- PhoneNetworking class
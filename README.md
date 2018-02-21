# UnityFloorFix

A Unity C# implementation of the floor fix implemented at https://github.com/matzman666/OpenVR-AdvancedSettings for OpenVR (SteamVR). 
It will adjust your calibrated floor level setup in SteamVR to the level of the lowest controller.

## Getting Started

Simply Download the [FloorFix.cs](FloorFix) file, add it to your project.
Add the script to a gameobject, and call the StartFloorFix method. Either via button click or from your own script.
There are a few UnityEvents you can subscribe to to get info from the progress of the floor fix.
If requested I will add a full example project.

### Prerequisites

This solution uses OpenVR's SDK. So you will need to download https://assetstore.unity.com/packages/templates/systems/steamvr-plugin-32647 and add it to your project. 
You will need to set it as your selected (active) SDK in Unity as well.

## Authors

* **Vincent Kruger** - *Initial Implementation* - (https://github.com/Vincent-1236/)

## License

See the [LICENSE](LICENSE) file for details

## Acknowledgments

* https://github.com/matzman666/ for his his OpenVR-AdvancedSettings Project
* Valve for the SteamVR and OpenVR Unity implementations
* https://github.com/purplebooth for the ReadMe Format (https://gist.github.com/PurpleBooth/109311bb0361f32d87a2)

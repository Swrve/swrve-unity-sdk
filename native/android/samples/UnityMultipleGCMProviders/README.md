Unity Swrve SDK Multiple GCM Providers Sample
---------------------------------------------
Example of how to integrate Swrve Push Notifications when your application already makes use of another push notification provider.

It showcases a custom GCM receiver to intercept push notifications that were intended for Swrve and redirect them, leaving all the others to the other provider:
- [CustomGcmReceiver](src/main/java/com/swrve/sdk/sample/CustomGcmReceiver.java)

How to use
----------
- Import the files into your project (except MyOtherPushProvider)
- Copy the modifications done in the AndroidManifest.xml to your custom AndroidManifest.xml

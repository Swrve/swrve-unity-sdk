using SwrveUnity;
using UnityEngine;

public class DemoMinimalIntegration : MonoBehaviour
{
    void Start()
    {
        SwrveConfig config = new SwrveConfig();
        // To use the EU stack, include this in your config.
        // config.SelectedStack = SwrveUnity.Stack.EU;

        //FIXME: FIX ME! Replace <app_id> and "<api_key>" with your app ID and API key.
#if PRODUCTION_BUILD
        SwrveComponent.Instance.Init(-1, "<production_api_key>", config);
#else
        SwrveComponent.Instance.Init(-1, "<dev_api_key>", config);
#endif
    }
}

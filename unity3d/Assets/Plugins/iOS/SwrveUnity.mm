#import "UnitySwrveHelper.h"

extern "C"
{
    const char* _swrveiOSIDFA()
    {
        return UnityAdvertisingIdentifier();
    }
}

#ifdef __cplusplus
extern "C"
{
#endif

    int _swrveiOSConversationVersion();
    char* _swrveiOSLanguage();
    char* _swrveiOSTimeZone();
    char* _swrveiOSAppVersion();
    char* _swrveiOSUUID();
    char* _swrveiOSCarrierName();
    char* _swrveiOSCarrierIsoCountryCode();
    char* _swrveiOSCarrierCode();
    char* _swrveiOSLocaleCountry();
    char* _swrveiOSIDFV();
    char* _swrveiOSIDFA();
    void _swrveiOSRegisterForPushNotifications(char* jsonUNCategorySet, char* jsonUICategorySet);
    void _swrveiOSInitNative(char* jsonConfig);
    void _swrveiOSShowConversation(char* conversation);
    void _swrveiOSStartLocation();
    void _swrveiOSLocationUserUpdate(char* jsonMap);
    char* _swrveiOSPlotNotifications();
    bool _swrveiOSIsSupportedOSVersion();
    char* _swrveInfluencedDataJson();
    char* _swrvePushNotificationStatus(char* componentName);
    char* _swrveBackgroundRefreshStatus();
    void _swrveiOSUpdateQaUser(char* jsonMap);

#ifdef __cplusplus
}
#endif

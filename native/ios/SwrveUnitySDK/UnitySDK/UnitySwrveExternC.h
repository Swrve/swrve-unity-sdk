#ifdef __cplusplus
extern "C"
{
#endif
    
    int _swrveiOSConversationVersion();
    char* _swrveiOSGetLanguage();
    char* _swrveiOSGetTimeZone();
    char* _swrveiOSGetAppVersion();
    char* _swrveiOSUUID();
    char* _swrveiOSCarrierName();
    char* _swrveiOSCarrierIsoCountryCode();
    char* _swrveiOSCarrierCode();
    char* _swrveiOSLocaleCountry();
    char* _swrveiOSIDFV();
    char* _swrveiOSIDFA();
    void _swrveiOSRegisterForPushNotifications(char* jsonCategorySet);
    void _swrveiOSInitNative(char* jsonConfig);
    void _swrveiOSShowConversation(char* conversation);
    void _swrveiOSStartLocation();
    void _swrveiOSLocationUserUpdate(char* jsonMap);
    char* _swrveiOSGetPlotNotifications();
    
#ifdef __cplusplus
}
#endif

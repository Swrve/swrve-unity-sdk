package com.swrve.sdk;

public class SwrveUnitySDK {
    private static SwrveNotificationCustomFilter notificationCustomFilter;

    public static SwrveNotificationCustomFilter getNotificationCustomFilter() {
        return notificationCustomFilter;
    }

    /**
     * Set the notification filter used for modifying remote notifications before they are displayed.
     *
     * @param notificationCustomFilter the notification custom filter to apply
     */
    public static void setNotificationCustomFilter(SwrveNotificationCustomFilter notificationCustomFilter) {
        SwrveUnitySDK.notificationCustomFilter = notificationCustomFilter;
    }
}

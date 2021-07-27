package com.swrve.sdk;

public class SwrveUnitySDK {
    private static SwrveNotificationFilter notificationFilter;

    public static SwrveNotificationFilter getNotificationFilter() {
        return notificationFilter;
    }

    /**
     * Set the notification filter used for modifying remote notifications before
     * they are displayed.
     *
     * @param notificationFilter the notification custom filter to apply
     */
    public static void setNotificationFilter(SwrveNotificationFilter notificationFilter) {
        SwrveUnitySDK.notificationFilter = notificationFilter;
    }
}

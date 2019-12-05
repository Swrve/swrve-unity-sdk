package com.swrve.sdk;

public class SwrveUnitySDK {
    private static SwrveNotificationCustomFilter notificationCustomFilter;
    private static SwrveNotificationFilter notificationFilter;

    @Deprecated
    public static SwrveNotificationCustomFilter getNotificationCustomFilter() {
        return notificationCustomFilter;
    }

    public static SwrveNotificationFilter getNotificationFilter() {
        return notificationFilter;
    }

    /**
     * Set the notification filter used for modifying remote notifications before
     * they are displayed.
     *
     * @param notificationCustomFilter the notification custom filter to apply
     */
    @Deprecated
    public static void setNotificationCustomFilter(SwrveNotificationCustomFilter notificationCustomFilter) {
        SwrveUnitySDK.notificationCustomFilter = notificationCustomFilter;
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

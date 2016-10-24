package com.swrve.unity.adm;

public class SwrveAdmHelper {
    public static final String SWRVE_TRACKING_KEY = "_p";
    public static final String TIMESTAMP_KEY = "_s.t";

    public static boolean isNullOrEmpty(String val) {
        return (val == null || val.length() == 0);
    }
}

package com.swrve.sdk;

class SwrveNativeCall {
    private final String object;
    private final String method;
    private final String msg;

    SwrveNativeCall(String object, String method, String msg) {
        this.object = object;
        this.method = method;
        this.msg = msg;
    }

    public String getObject() {
        return object;
    }

    public String getMethod() {
        return method;
    }

    public String getMsg() {
        return msg;
    }
}

package com.swrve.sdk;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

// This annotation is used by the JNI generator to create the necessary JNI
// bindings and expose this method to native code.
@Target(ElementType.METHOD)
@Retention(RetentionPolicy.RUNTIME)
@interface CalledByUnity {
}

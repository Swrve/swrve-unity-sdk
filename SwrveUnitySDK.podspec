Pod::Spec.new do |s|
  s.name             = "SwrveUnitySDK"
  s.version          = "4.10.0"
  s.summary          = "iOS Common library for Swrve."
  s.homepage         = "http://www.swrve.com"
  s.license          = { "type" => "Apache License, Version 2.0", "file" => "native/ios/#{s.name}/LICENSE" }
  s.authors          = "Swrve Mobile Inc or its licensors"
  s.source           = { :git => "https://github.com/Swrve/swrve-unity-sdk.git", :tag => s.version.to_s }
  s.social_media_url = "https://twitter.com/Swrve_Inc"

  s.platform     = :ios, "6.0"
  s.requires_arc = true

  s.source_files = "native/ios/#{s.name}/UnitySDK/**/*.{mm,m,h}"
  s.public_header_files = "native/ios/#{s.name}/UnitySDK/**/*.h"

  s.dependency "SwrveSDKCommon", "~> #{s.version.to_s}"
  s.dependency "SwrveConversationSDK", "~> #{s.version.to_s}"
end

Pod::Spec.new do |s|
  s.name             = "SwrveUnitySDK"
  s.version          = "4.5.1"
  s.summary          = "iOS Common library for Swrve."
  s.homepage         = "http://www.swrve.com"
  s.license          = { "type" => "Apache License, Version 2.0", "file" => 'native/ios/' + s.name.to_s + '/LICENSE' }
  s.authors          = "Swrve Mobile Inc or its licensors"
  s.source           = { :git => "https://github.com/Swrve/swrve-unity-sdk.git", :tag => s.version.to_s }
  s.social_media_url = 'https://twitter.com/Swrve_Inc'

  s.platform     = :ios, '6.0'
  s.requires_arc = true

  s.source_files = 'native/ios/' + s.name.to_s + '/UnitySDK/**/*.{mm,m,h}'
  s.public_header_files = 'native/ios/' + s.name.to_s + '/UnitySDK/**/*.h'

  s.dependency 'SwrveSDKCommon', '4.5.1'
  s.dependency 'SwrveConversationSDK', '4.5.1'
end

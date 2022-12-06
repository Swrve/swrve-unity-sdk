using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using SwrveUnity.Input;
using SwrveUnity.Helpers;

namespace SwrveUnity.Messaging
{
    /// <summary>
    /// Used internally to render in-app messages.
    /// </summary>
    public class SwrveInAppMessageView
    {
        private static Rect _wholeScreen = new Rect();

        private readonly string _swrveTemporaryPath;
        private readonly MonoBehaviour _container;
        public SwrveMessageFormat Format;
        private readonly SwrveInAppMessageConfig _inAppConfig;
        private readonly SwrveMessageTextTemplatingResolver _templatingResolver;

        public readonly SwrveMessage Message;
        public SwrveOrientation Orientation;
        private readonly ISwrveMessageListener _messageListener;
        private SwrveWidgetView[] _widgetViews;
        public bool Dismissed; // Flag to notify the SDK that it has been dismissed and needs to be cleared
        public bool Rotate; // Flag to rotate the current format if no other is present to be displayed
        private bool _ready; // Flag to Indicates when it can render and process events (after assets are pre-loaded)
        private readonly bool _renderBackgroundColor;
        private ISwrveAssetsManager _swrveAssetsManager;
        private SwrveOrientation _orientationChangeCache;
        public long CurrentPageId;
        public List<long> SentNavigationEvents = new List<long>(); // buttonIds
        public List<long> SentPageViewEvents = new List<long>(); // pageIds

        public SwrveInAppMessageView(string swrveTemporaryPath, MonoBehaviour container, SwrveMessageFormat format, SwrveInAppMessageConfig inAppConfig,
            SwrveMessageTextTemplatingResolver templatingResolver)
        {
            _swrveTemporaryPath = swrveTemporaryPath;
            _container = container;
            Format = format;
            _inAppConfig = inAppConfig;
            Message = format.Message;
            Orientation = format.Orientation;
            _messageListener = inAppConfig.MessageListener;
            _templatingResolver = templatingResolver;
            _renderBackgroundColor = format.BackgroundColor.HasValue;
            CurrentPageId = format.FirstPageId;

            // Create widgets to render and use the cached personalization values
            _widgetViews = BuildWidgets(format, format.FirstPageId, templatingResolver, _inAppConfig);
        }

        private static SwrveWidgetView[] BuildWidgets(SwrveMessageFormat format, long pageId, SwrveMessageTextTemplatingResolver templatingResolver, SwrveInAppMessageConfig inAppConfig)
        {
            SwrveMessagePage page = format.Pages[pageId];
            // Create widgets to render and use the cached personalization values
            SwrveWidgetView[] widgetViews = new SwrveWidgetView[page.Images.Count + page.Buttons.Count];
            int eindex = 0;
            for (int ii = 0; ii < page.Images.Count; ii++)
            {
                SwrveImage image = page.Images[ii];
                SwrveWidgetView renderer;
                if (image.Text != null)
                {
                    string resolvedTextTemplate = templatingResolver.TextResolution[image];
                    SwrveTextViewStyle style = new SwrveTextViewStyle();
                    style.FontSize = image.FontSize;
                    style.HorizontalAlignment = image.HorizontalAlignment;
                    style.TextBackgroundColor = inAppConfig.PersonalizedTextBackgroundColor;
                    style.TextForegroundColor = inAppConfig.PersonalizedTextForegroundColor;
                    style.TextFont = inAppConfig.PersonalizedTextFont;

                    renderer = new SwrveTextWidgetView(image, resolvedTextTemplate, inAppConfig, style, format.Calibration);
                }
                else if (image.DynamicImageUrl != null && templatingResolver.DynamicImageResolution.ContainsKey(image))
                {
                    string sha1DynamicImageAsset = ResolvePersonalization(image, templatingResolver, (image.File != null));
                    renderer = new SwrveImageView(image, sha1DynamicImageAsset);
                }
                else
                {
                    renderer = new SwrveImageView(image, null);
                }

                widgetViews[eindex++] = renderer;
            }

            for (int bi = 0; bi < page.Buttons.Count; bi++)
            {
                SwrveButton button = page.Buttons[bi];
                SwrveWidgetView renderer;
                if (button.Text != null)
                {
                    string resolvedTextTemplate = templatingResolver.TextResolution[button];

                    SwrveTextViewStyle style = new SwrveTextViewStyle();
                    style.FontSize = button.FontSize;
                    style.TextBackgroundColor = inAppConfig.PersonalizedTextBackgroundColor;
                    style.TextForegroundColor = inAppConfig.PersonalizedTextForegroundColor;
                    style.TextFont = inAppConfig.PersonalizedTextFont;

                    renderer = new SwrveTextWidgetView(button, resolvedTextTemplate, inAppConfig, style, format.Calibration);
                }
                else if (button.DynamicImageUrl != null)
                {
                    string sha1DynamicImageAsset = ResolvePersonalization(button, templatingResolver, (button.Image != null));
                    renderer = new SwrveButtonView(button, inAppConfig.ButtonClickTintColor, sha1DynamicImageAsset);
                }
                else
                {
                    renderer = new SwrveButtonView(button, inAppConfig.ButtonClickTintColor, null);
                }

                widgetViews[eindex++] = renderer;
            }

            return widgetViews;
        }

        private static string ResolvePersonalization(SwrveWidget widget, SwrveMessageTextTemplatingResolver templatingResolver, bool hasFallback)
        {
            SwrveMessage message = widget.Message;
            if (templatingResolver.DynamicImageResolution.ContainsKey(widget))
            {
                string resolvedDynamicImageTemplate = templatingResolver.DynamicImageResolution[widget];
                byte[] dynamicAssetBytes = System.Text.Encoding.UTF8.GetBytes(resolvedDynamicImageTemplate);
                string sha1DynamicImageAsset = SwrveHelper.sha1(dynamicAssetBytes);

                if (message.IsAssetDownloaded(sha1DynamicImageAsset))
                {
                    return sha1DynamicImageAsset;
                }
                SwrveLog.LogInfo("Personalized asset not found in cache: " + sha1DynamicImageAsset);
                SwrveQaUser.AssetFailedToDisplay(message.Campaign.Id, message.Id, sha1DynamicImageAsset, widget.DynamicImageUrl, resolvedDynamicImageTemplate, hasFallback, "Asset not found in cache");
                return null;
            }

            SwrveLog.LogInfo("Cannot resolve personalized asset: ", widget.DynamicImageUrl);
            SwrveQaUser.AssetFailedToDisplay(message.Campaign.Id, message.Id, null, widget.DynamicImageUrl, null, hasFallback, "Could not resolve url personalization");
            return null;
        }

        public SwrveButtonClickResult Update(IInputManager inputManager, bool nativeIsBackPressed)
        {
            SwrveButtonClickResult result = null;
            // Event processing
            if (!Dismissed)
            {
                if (inputManager.GetMouseButtonDown(0))
                {
                    ProcessButtonDown(inputManager);
                }
                else if (inputManager.GetMouseButtonUp(0))
                {
                    result = ProcessButtonUp(inputManager);
                }
            }

            if (nativeIsBackPressed)
            {
                Dismiss();
            }

            return result;
        }

        public void Render(SwrveOrientation orientation)
        {
            if (!_ready) return;

            // Save current GUI state
            int originalGuiDepth = ImgGUI.depth;
            Matrix4x4 originalTransform = ImgGUI.matrix;
            // Draw message
            ImgGUI.depth = 0;
            DrawMessage(Screen.width, Screen.height);
            // Revert previous GUI state
            ImgGUI.matrix = originalTransform;
            ImgGUI.depth = originalGuiDepth;

            if (this._messageListener != null)
            {
                this._messageListener.OnShowing(Format);
            }

            if (orientation != _orientationChangeCache)
            {
                if (orientation == this.Orientation)
                {
                    Rotate = false;
                }
                else
                {
                    // Start pre-loading the format for this new orientation if it is available
                    SwrveMessageFormat newFormat = Message.GetFormat(orientation);
                    if (newFormat != null)
                    {
                        StartTask("RedrawMessageView", RedrawMessageView(newFormat, CurrentPageId));
                    }
                    else
                    {
                        Rotate = true; // There is no new format so we should rotate this one
                    }
                }
                _orientationChangeCache = orientation; // Do not do this check again
            }
        }

        private void DrawMessage(int screenWidth, int screenHeight)
        {
            int centerx = (int)(Screen.width / 2);
            int centery = (int)(Screen.height / 2);

            if (_renderBackgroundColor)
            {
                Color backgroundColor = Format.BackgroundColor.Value;
                ImgGUI.color = backgroundColor;
                _wholeScreen.width = screenWidth;
                _wholeScreen.height = screenHeight;
                ImgGUI.DrawTexture(_wholeScreen, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0.0f);
                ImgGUI.color = Color.white;
            }

            bool rotatedFormat = Rotate;
            // Rotate the inner message if necessary
            if (rotatedFormat)
            {
                Vector2 pivotPoint = new Vector2(centerx, centery);
                GUIUtility.RotateAroundPivot(90, pivotPoint);
            }

            float scale = Format.Scale;
            for (int ii = 0; ii < _widgetViews.Length; ii++)
            {
                _widgetViews[ii].Render(scale, centerx, centery, rotatedFormat);
            }
        }

        public void ProcessButtonDown(IInputManager inputManager)
        {
            if (_ready)
            {
                Vector3 mousePosition = inputManager.GetMousePosition();
                for (int ii = 0; ii < _widgetViews.Length; ii++)
                {
                    SwrveWidgetView view = _widgetViews[ii];
                    if (view is ISwrveButtonView)
                    {
                        ((ISwrveButtonView)view).ProcessButtonDown(mousePosition);
                    }
                }
            }
        }

        public SwrveButtonClickResult ProcessButtonUp(IInputManager inputManager)
        {
            SwrveButtonClickResult clickResult = null;
            if (_ready)
            {
                // Capture last button clicked (last rendered, rendered on top)
                Vector3 mousePosition = inputManager.GetMousePosition();
                for (int ii = _widgetViews.Length - 1; ii >= 0 && clickResult == null; ii--)
                {
                    SwrveWidgetView view = _widgetViews[ii];
                    if (view is ISwrveButtonView)
                    {
                        clickResult = ((ISwrveButtonView)view).ProcessButtonUp(mousePosition, _templatingResolver);
                    }
                }
            }

            return clickResult;
        }

        /// <summary>
        /// Dismiss the message format
        /// </summary>
        public void Dismiss()
        {
            if (!Dismissed)
            {
                Dismissed = true;
                UnloadAssets(_widgetViews);
                Message.Campaign.MessageDismissed();
                if (_messageListener != null)
                {
                    _messageListener.OnDismiss(Format);
                }
            }
        }

        private static void UnloadAssets(SwrveWidgetView[] _widgetViews)
        {
            // for (int ii = 0; ii < _widgetViews.Length; ii++)
            // {
            //     SwrveWidgetView widget = _widgetViews[ii];
            //     widget.Unload();
            // }
        }

        public IEnumerator PreloadAndDisplay(CoroutineReference<bool> wereAllLoaded)
        {
            SwrveLog.Log("PreloadAndDisplay");
            yield return _container.StartCoroutine(PreloadWidgets(_container, wereAllLoaded, _widgetViews));
            if (wereAllLoaded.Value())
            {
                _ready = true;
                if (_messageListener != null)
                {
                    _messageListener.OnShow(Format);
                }
            }
        }

        public void PageNavigation(long pageId)
        {
            CurrentPageId = pageId;
            StartTask("RedrawMessageView", RedrawMessageView(Format, CurrentPageId));
        }

        private IEnumerator RedrawMessageView(SwrveMessageFormat format, long pageId)
        {
            SwrveLog.Log("RedrawMessageView");
            CoroutineReference<bool> wereAllLoaded = new CoroutineReference<bool>(false);
            SwrveWidgetView[] newWidgetViews = BuildWidgets(format, pageId, _templatingResolver, _inAppConfig);

            yield return _container.StartCoroutine(PreloadWidgets(_container, wereAllLoaded, newWidgetViews));
            if (wereAllLoaded.Value())
            {
                SwrveWidgetView[] oldViews = _widgetViews;
                Format = format;
                _widgetViews = newWidgetViews;
                Orientation = format.Orientation;
                UnloadAssets(oldViews); // Unload the old format
                if (Dismissed)
                {
                    UnloadAssets(_widgetViews); // Message was closed while the format was preloaded
                }
            }
            TaskFinished("RedrawMessageView");
        }

        private IEnumerator PreloadWidgets(MonoBehaviour Container, CoroutineReference<bool> wereAllLoaded, SwrveWidgetView[] _widgetViews)
        {
            bool allLoaded = true;
            for (int ii = 0; ii < _widgetViews.Length; ii++)
            {
                SwrveWidgetView view = _widgetViews[ii];
                string texturePath = view.GetTexturePath();

                if (!string.IsNullOrEmpty(texturePath))
                {
                    SwrveLog.Log("Preloading asset file " + texturePath);
                    CoroutineReference<Texture2D> result = new CoroutineReference<Texture2D>();
                    yield return Container.StartCoroutine(LoadAsset(texturePath, result));
                    if (result.Value() != null)
                    {
                        view.SetTexture(result.Value());
                    }
                    else
                    {
                        allLoaded = false;
                    }
                }
            }

            if (!allLoaded)
            {
                UnloadAssets(_widgetViews);
            }

            wereAllLoaded.Value(allLoaded);
        }

        private IEnumerator LoadAsset(string fileName, CoroutineReference<Texture2D> texture)
        {
            string filePath = GetTemporaryPathFileName(fileName);
            UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + filePath);
            yield return www.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
            if (www.result == UnityWebRequest.Result.Success)
            {
#else
            if (!www.isNetworkError && !www.isHttpError)
            {
#endif
                Texture2D loadedTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                texture.Value(loadedTexture);
            }
            else
            {
                SwrveLog.LogError("Could not load asset with WWW " + filePath + ": " + www.error);
                // Try to load from file system
                if (CrossPlatformFile.Exists(filePath))
                {
                    byte[] byteArray = CrossPlatformFile.ReadAllBytes(filePath);
                    Texture2D loadedTexture = new Texture2D(4, 4);
                    if (loadedTexture.LoadImage(byteArray))
                    {
                        texture.Value(loadedTexture);
                    }
                    else
                    {
                        SwrveLog.LogWarning("Could not load asset from I/O" + filePath);
                    }
                }
                else
                {
                    SwrveLog.LogError("The file " + filePath + " does not exist.");
                }
            }
        }

        private string GetTemporaryPathFileName(string fileName)
        {
            return Path.Combine(_swrveTemporaryPath, fileName);
        }

        // Visible for tests
        public SwrveWidgetView[] GetWidgetViews()
        {
            return _widgetViews;
        }

        public virtual Coroutine StartTask(string tag, IEnumerator task)
        {
            return _container.StartCoroutine(task);
        }

        protected virtual void TaskFinished(string tag)
        {
        }
    }
}

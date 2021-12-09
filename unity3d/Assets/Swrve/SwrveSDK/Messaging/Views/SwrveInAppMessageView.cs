using System;
using System.Collections;
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
    private static Rect WholeScreen = new Rect();

    private string swrveTemporaryPath;
    private MonoBehaviour Container;

    // Visible for tests
    public readonly SwrveMessage Message;
    private readonly SwrveInAppMessageConfig InAppConfig;

    // Visible for tests
    public SwrveMessageFormat Format;

    // Visible for tests
    public SwrveOrientation Orientation;

    private ISwrveMessageListener listener;

    private readonly SwrveMessageTextTemplatingResolver templatingResolver;

    private SwrveWidgetView[] widgetViews;

    // Notify the SDK that it has been dismissed and needs to be cleared
    public bool Dismissed = false;

    // Visible for tests
    // Rotate the current format if no other is present to be displayed
    public bool Rotate = false;

    // Indicates when it can render and process events (after assets are pre-loaded)
    private bool Ready = false;

    private bool renderBackgroundColor;

    private ISwrveAssetsManager swrveAssetsManager;

    private SwrveOrientation _orientationChangeCache;

    public SwrveInAppMessageView(string swrveTemporaryPath, MonoBehaviour container, SwrveMessageFormat format, SwrveInAppMessageConfig inAppConfig,
                                 SwrveMessageTextTemplatingResolver templatingResolver)
    {
        this.swrveTemporaryPath = swrveTemporaryPath;
        this.Container = container;
        this.Format = format;
        this.InAppConfig = inAppConfig;
        this.Message = format.Message;
        this.Orientation = format.Orientation;
        this.listener = inAppConfig.MessageListener;
        this.templatingResolver = templatingResolver;

        renderBackgroundColor = format.BackgroundColor.HasValue;

        // Create widgets to render and use the cached personalization values
        widgetViews = buildWidgets(format, templatingResolver, InAppConfig);
    }

    private static SwrveWidgetView[] buildWidgets(SwrveMessageFormat format, SwrveMessageTextTemplatingResolver templatingResolver, SwrveInAppMessageConfig inAppConfig)
    {
        // Create widgets to render and use the cached personalization values
        SwrveWidgetView[] widgetViews = new SwrveWidgetView[format.Images.Count + format.Buttons.Count];
        int eindex = 0;
        for (int ii = 0; ii < format.Images.Count; ii++) {
            SwrveImage image = format.Images[ii];
            SwrveWidgetView renderer;
            if (image.Text != null) {
                string resolvedTextTemplate = templatingResolver.TextResolution[image];
                SwrveTextViewStyle style = new SwrveTextViewStyle();
                style.FontSize = image.FontSize;
                style.HorizontalAlignment = image.HorizontalAlignment;
                style.TextBackgroundColor = inAppConfig.PersonalizedTextBackgroundColor;
                style.TextForegroundColor = inAppConfig.PersonalizedTextForegroundColor;
                style.TextFont = inAppConfig.PersonalizedTextFont;

                renderer = new SwrveTextWidgetView(image, resolvedTextTemplate, inAppConfig, style, format.Calibration);
            } else if (image.DynamicImageUrl != null && templatingResolver.DynamicImageResolution.ContainsKey(image)) {
                string sha1DynamicImageAsset = ResolvePersonalization(image, templatingResolver, (image.File != null));
                renderer = new SwrveImageView(image, sha1DynamicImageAsset);
            } else {
                renderer = new SwrveImageView(image, null);
            }
            widgetViews[eindex++] = renderer;
        }

        for (int bi = 0; bi < format.Buttons.Count; bi++) {
            SwrveButton button = format.Buttons[bi];
            SwrveWidgetView renderer;
            if (button.Text != null) {
                string resolvedTextTemplate = templatingResolver.TextResolution[button];
                
                SwrveTextViewStyle style = new SwrveTextViewStyle();
                style.FontSize = button.FontSize;
                style.TextBackgroundColor = inAppConfig.PersonalizedTextBackgroundColor;
                style.TextForegroundColor = inAppConfig.PersonalizedTextForegroundColor;
                style.TextFont = inAppConfig.PersonalizedTextFont;

                renderer = new SwrveTextWidgetView(button, resolvedTextTemplate, inAppConfig, style, format.Calibration);
            } else if (button.DynamicImageUrl != null) {
                string sha1DynamicImageAsset = ResolvePersonalization(button, templatingResolver, (button.Image != null));
                renderer = new SwrveButtonView(button, inAppConfig.ButtonClickTintColor, sha1DynamicImageAsset);
            } else {
                renderer = new SwrveButtonView(button, inAppConfig.ButtonClickTintColor, null);
            }
            widgetViews[eindex++] = renderer;
        }

        return widgetViews;
    }

    private static string ResolvePersonalization(SwrveWidget widget, SwrveMessageTextTemplatingResolver templatingResolver, bool hasFallback)
    {
        SwrveMessage message = widget.Message;
        if (templatingResolver.DynamicImageResolution.ContainsKey(widget)) {
            string resolvedDynamicImageTemplate = templatingResolver.DynamicImageResolution[widget];
            byte[] dynamicAssetBytes = System.Text.Encoding.UTF8.GetBytes(resolvedDynamicImageTemplate);
            string sha1DynamicImageAsset = SwrveHelper.sha1(dynamicAssetBytes);

            if (message.IsAssetDownloaded(sha1DynamicImageAsset)) {
                return sha1DynamicImageAsset;
            } else {
                SwrveLog.LogInfo("Personalized asset not found in cache: " + sha1DynamicImageAsset);
                SwrveQaUser.AssetFailedToDisplay(message.Campaign.Id, message.Id, sha1DynamicImageAsset, widget.DynamicImageUrl, resolvedDynamicImageTemplate, hasFallback, "Asset not found in cache");
                return null;
            }
        } else {
            SwrveLog.LogInfo("Cannot resolve personalized asset: ", widget.DynamicImageUrl);
            SwrveQaUser.AssetFailedToDisplay(message.Campaign.Id, message.Id, null, widget.DynamicImageUrl, null, hasFallback, "Could not resolve url personalization");
            return null;
        }
    }

    public SwrveButtonClickResult Update(IInputManager inputManager, bool nativeIsBackPressed)
    {
        SwrveButtonClickResult result = null;

        // Event processing
        if (!Dismissed) {
            if (inputManager.GetMouseButtonDown(0)) {
                ProcessButtonDown(inputManager);
            } else if (inputManager.GetMouseButtonUp(0)) {
                result = ProcessButtonUp(inputManager);
            }
        }

        if (nativeIsBackPressed) {
            Dismiss();
        }

        return result;
    }

    public void Render(SwrveOrientation orientation)
    {
        if (!Ready) return;

        // Save current GUI state
        int originalGuiDepth = ImgGUI.depth;
        Matrix4x4 originalTransform = ImgGUI.matrix;
        // Draw message
        ImgGUI.depth = 0;
        drawMessage(Screen.width, Screen.height);
        // Revert previous GUI state
        ImgGUI.matrix = originalTransform;
        ImgGUI.depth = originalGuiDepth;

        if (this.listener != null) {
            this.listener.OnShowing(Format);
        }

        if (orientation != _orientationChangeCache) {
            if (orientation == this.Orientation) {
                Rotate = false;
            } else {
                // Start pre-loading the format for this new orientation if it is available
                SwrveMessageFormat newFormat = Message.GetFormat(orientation);
                if (newFormat != null) {
                    StartTask("switchToNewFormat", switchToNewFormat(newFormat));
                } else {
                    // There is no new format so we should rotate this one
                    Rotate = true;
                }
            }

            // Do not do this check again
            _orientationChangeCache = orientation;
        }
    }

    private void drawMessage(int screenWidth, int screenHeight)
    {
        int centerx = (int)(Screen.width / 2);
        int centery = (int)(Screen.height / 2);

        if (renderBackgroundColor) {
            Color backgroundColor = Format.BackgroundColor.Value;
            ImgGUI.color = backgroundColor;
            WholeScreen.width = screenWidth;
            WholeScreen.height = screenHeight;
            ImgGUI.DrawTexture(WholeScreen, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0.0f);
            ImgGUI.color = Color.white;
        }

        bool rotatedFormat = Rotate;
        // Rotate the inner message if necessary
        if (rotatedFormat) {
            Vector2 pivotPoint = new Vector2(centerx, centery);
            GUIUtility.RotateAroundPivot(90, pivotPoint);
        }

        float scale = Format.Scale;
        for (int ii = 0; ii < widgetViews.Length; ii++) {
            widgetViews[ii].Render(scale, centerx, centery, rotatedFormat);
        }
    }

    public void ProcessButtonDown(IInputManager inputManager)
    {
        if (Ready) {
            Vector3 mousePosition = inputManager.GetMousePosition();
            for (int ii = 0; ii < widgetViews.Length; ii++) {
                SwrveWidgetView view = widgetViews[ii];
                if (view is ISwrveButtonView) {
                    ((ISwrveButtonView)view).ProcessButtonDown(mousePosition);
                }
            }
        }
    }

    public SwrveButtonClickResult ProcessButtonUp(IInputManager inputManager)
    {
        SwrveButtonClickResult clickResult = null;

        if (Ready) {
            // Capture last button clicked (last rendered, rendered on top)
            Vector3 mousePosition = inputManager.GetMousePosition();
            for (int ii = widgetViews.Length - 1; ii >= 0 && clickResult == null; ii--) {
                SwrveWidgetView view = widgetViews[ii];
                if (view is ISwrveButtonView) {
                    clickResult = ((ISwrveButtonView)view).ProcessButtonUp(mousePosition, templatingResolver);
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
        if (!Dismissed) {
            Dismissed = true;
            unloadAssets(widgetViews);

            Message.Campaign.MessageDismissed();

            if (listener != null) {
                listener.OnDismiss(Format);
            }
        }
    }

    private static void unloadAssets(SwrveWidgetView[] _widgetViews)
    {
        for (int ii = 0; ii < _widgetViews.Length; ii++) {
            SwrveWidgetView widget = _widgetViews[ii];
            widget.Unload();
        }
    }

    public IEnumerator PreloadAndDisplay(CoroutineReference<bool> wereAllLoaded)
    {
        SwrveLog.Log("Preloading format");

        yield return Container.StartCoroutine(preloadWidgets(Container, wereAllLoaded, widgetViews));

        if (wereAllLoaded.Value()) {
            Ready = true;

            if (listener != null) {
                listener.OnShow(Format);
            }
        }
    }

    private IEnumerator switchToNewFormat(SwrveMessageFormat format)
    {
        SwrveLog.Log("Preloading format");

        CoroutineReference<bool> wereAllLoaded = new CoroutineReference<bool>(false);
        SwrveWidgetView[] newWidgetViews = buildWidgets(format, templatingResolver, InAppConfig);

        yield return Container.StartCoroutine(preloadWidgets(Container, wereAllLoaded, newWidgetViews));
        if (wereAllLoaded.Value()) {
            SwrveWidgetView[] oldViews = widgetViews;

            Format = format;
            widgetViews = newWidgetViews;
            Orientation = format.Orientation;

            // Unload the old format
            unloadAssets(oldViews);

            if (Dismissed) {
                // Message was closed while the format was preloaded
                unloadAssets(widgetViews);
            }
        }

        TaskFinished("switchToNewFormat");
    }

    private IEnumerator preloadWidgets(MonoBehaviour Container, CoroutineReference<bool> wereAllLoaded, SwrveWidgetView[] _widgetViews)
    {
        bool allLoaded = true;

        for (int ii = 0; ii < _widgetViews.Length; ii++) {
            SwrveWidgetView view = _widgetViews[ii];
            string texturePath = view.GetTexturePath();

            if (!string.IsNullOrEmpty(texturePath)) {
                SwrveLog.Log("Preloading asset file " + texturePath);
                CoroutineReference<Texture2D> result = new CoroutineReference<Texture2D>();
                yield return Container.StartCoroutine(loadAsset(texturePath, result));

                if (result.Value() != null) {
                    view.SetTexture(result.Value());
                } else {
                    allLoaded = false;
                }
            }
        }

        if (!allLoaded) {
            unloadAssets(_widgetViews);
        }

        wereAllLoaded.Value(allLoaded);
    }

    private IEnumerator loadAsset(string fileName, CoroutineReference<Texture2D> texture)
    {
        string filePath = GetTemporaryPathFileName(fileName);

        UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + filePath);
        yield return www.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
        if (www.result == UnityWebRequest.Result.Success) {
#else
        if (!www.isNetworkError && !www.isHttpError) {
#endif
            Texture2D loadedTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            texture.Value(loadedTexture);
        } else {
            SwrveLog.LogError("Could not load asset with WWW " + filePath + ": " + www.error);

            // Try to load from file system
            if (CrossPlatformFile.Exists(filePath)) {
                byte[] byteArray = CrossPlatformFile.ReadAllBytes(filePath);
                Texture2D loadedTexture = new Texture2D(4, 4);
                if (loadedTexture.LoadImage(byteArray)) {
                    texture.Value(loadedTexture);
                } else {
                    SwrveLog.LogWarning("Could not load asset from I/O" + filePath);
                }
            } else {
                SwrveLog.LogError("The file " + filePath + " does not exist.");
            }
        }
    }

    private string GetTemporaryPathFileName(string fileName)
    {
        return Path.Combine(swrveTemporaryPath, fileName);
    }

    // Visible for tests
    public SwrveWidgetView[] GetWidgetViews()
    {
        return widgetViews;
    }

    public virtual Coroutine StartTask(string tag, IEnumerator task)
    {
        return Container.StartCoroutine(task);
    }

    protected virtual void TaskFinished(string tag)
    {
    }
}
}

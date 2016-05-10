#if UNITY_5

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Swrve.IAP;
using System;
using System.Linq;
using UnityEngine.Events;
using Swrve.Messaging;
using SwrveMiniJSON;
using System.Text.RegularExpressions;

public class HomeMenuComponent : MonoBehaviour, IGameController, IGame {
    enum ViewPages {
        MainMenu,
        MessageCenter
    }

    enum PositionState {
        Moving,
        Left,
        Centre,
        Right
    }

    public Image modalImage;
    public Transform footerPanel;

    public GameObject _buttonPrefab;
    public GameObject _modalQuestionPrefab;
    public GameObject _demoConversationPrefab;

    public GameObject _leftPanel;
    public GameObject _mainMenu;
    public GameObject _messageCenter;

    public List<GameObject> movableTargets;

    private static GameObject ButtonPrefab;
    private static GameObject ModalQuestionPrefab;

    void Awake() {
        ModalQuestionPrefab = _modalQuestionPrefab;
        ButtonPrefab = _buttonPrefab;
    }

  	// Use this for initialization
  	void Start () {
        // In-app messaging setup
        SwrveComponent.SDK.GlobalMessageListener = new CustomMessageListener (this);
        SwrveComponent.SDK.GlobalCustomButtonListener = new CustomButtonListener ();

    #if UNITY_EDITOR
        SwrveComponent.SDK.ConversationEditorCallback = OnConversation;
    #endif

        foreach(KeyValuePair<string, UnityAction> kvp in new Dictionary<string, UnityAction> {
            {"Left Panel", ToLeftPanel},
            {"Main Menu", ToMainMenu},
            {"Message Center", ToMessageCenter}
        } ) {
            SetButton (kvp, footerPanel);
        }
    }

    public static void SetButton(KeyValuePair<string, UnityAction> kvp, Transform transform) {
        CustomButtonComponent button = GameObject.Instantiate (ButtonPrefab).GetComponent<CustomButtonComponent> ();
        button.label.text = kvp.Key;
        button.GetComponent<Button> ().onClick.AddListener (kvp.Value);
        button.transform.SetParent (transform, false);
    }

    public IGame getGame() {
        return this;
    }

    public void pauseGame() {
        modalImage.gameObject.SetActive (true);
    }
    public void resumeGame() {
        modalImage.gameObject.SetActive (false);
    }

    class MoveInfo {
        public GameObject target;
        public float stepTarget;
        public Vector2 position;
        public float originalX;

        public MoveInfo(GameObject target, PositionState state, bool shift=false) {
            bool left = PositionState.Left == state;
            position = target.transform.position;
            originalX = position.x;
            if(shift) {
                position.x = position.x + ((left ? 1 : -1) * UnityEngine.Screen.width);
                target.transform.position = position;
            }
            stepTarget = position.x + ((left ? -1 : 1) * UnityEngine.Screen.width);
            this.target = target;
        }
    }

    void ToLeftPanel() {
        MoveTo (_leftPanel);
    }

    void ToMainMenu() {
        MoveTo (_mainMenu);
    }

    void ToMessageCenter() {
        MoveTo (_messageCenter);
    }

    List<MoveInfo> moves = new List<MoveInfo>();
    PositionState state = PositionState.Centre;
    PositionState nextState;
    GameObject nextInactive;

    void MoveTo(GameObject target) {
        if (state == PositionState.Moving || target.activeSelf) {
            return;
        }

        nextInactive = movableTargets.Where (a => a.activeSelf).First ();
        int i = movableTargets.IndexOf (nextInactive);
        nextState = (i < movableTargets.IndexOf(target) ? PositionState.Left : PositionState.Right) ;

        state = PositionState.Moving;
        moves.Add (new MoveInfo(movableTargets[i], nextState));

        target.SetActive (true);
        moves.Add (new MoveInfo (target, nextState, true));
    }

    void Update() {
        if(PositionState.Moving == state)
        {
            List<MoveInfo> toDelete = null;
            foreach (MoveInfo info in moves) {
                if (1 > Math.Abs (info.position.x - info.stepTarget)) {
                    info.position.x = info.stepTarget;
                }

                if (info.position.x != info.stepTarget) {
                    info.position.x = Mathf.Lerp (info.position.x, info.stepTarget, 5f * Time.deltaTime);
                } else {
                    if (null == toDelete) {
                        toDelete = new List<MoveInfo> ();
                    }
                    toDelete.Add (info);
                }

                info.target.transform.position = info.position;
            }

            if (null != toDelete) {
                foreach (MoveInfo info in toDelete) {
                    moves.Remove (info);
                    if (info.target == nextInactive) {
                        info.position.x = info.originalX;
                        info.target.transform.position = info.position;
                        info.target.SetActive (false);
                    }
                }
            }
            if (0 == moves.Count) {
                this.state = this.nextState;
            }
        }
    }

    void OnConversation(string conversation) {
        DemoEditorConversation view = GameObject.Instantiate(_demoConversationPrefab).GetComponent<DemoEditorConversation>();

        string pattern = @"<[^>]+>";
        Dictionary<string, object> convoDict = (Dictionary<string, object>)Json.Deserialize (conversation);
        view.header.text = (string)convoDict["name"];
        foreach (var page in (List<object>)convoDict["pages"]) {
            foreach (var content in (List<object>)((Dictionary<string, object>)page)["content"]) {
                Dictionary<string, object> _content = (Dictionary<string, object>)content;
                if (_content.ContainsKey ("value")) {
                    string value = (string)_content ["value"];

                    Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                    MatchCollection matches = rgx.Matches(value);
                    if (matches.Count > 0)
                    {
                        UnityEngine.Debug.Log (string.Format ("{0} ({1} matches):", value, matches.Count));
                        foreach (Match match in matches) {
                            UnityEngine.Debug.Log ("   " + match.Value);
                            value = value.Replace (match.Value, "");
                        }
                    }

                    view.conversation.text += value + "\n\n";
                }
            }
        }
    }

    public static void AskModalQuestion(
        string title, string text, UnityAction positiveCallback,
        string positiveText="OK", string negativeText="Cancel",
        UnityAction negativeCallback=null
    ) {
        ModalQuestionComponent modal = GameObject.Instantiate(ModalQuestionPrefab).GetComponent<ModalQuestionComponent>();

        modal.titleText.text = title;
        modal.questionText.text = text;
        Action<bool> afterClick = (positive) => {
            Destroy (modal.gameObject);
            if(positive) {
                positiveCallback.Invoke();
            }
            else if(null != negativeCallback) {
                negativeCallback.Invoke();
            }
        };

        modal.positiveButton.onClick.AddListener (() => afterClick(true));
        modal.negativeButton.onClick.AddListener (() => afterClick(false));
    }

    /// <summary>
    /// Process in-app message custom button clicks.
    /// </summary>
    private class CustomButtonListener : ISwrveCustomButtonListener/// 
    {
        public void OnAction (string customAction)
        {
            // Custom button logic
            UnityEngine.Debug.Log ("Custom action triggered " + customAction);
        }
    }

    /// <summary>
    /// Observe the SDK for in-app messages and pause/resume your game.
    /// </summary>
    private class CustomMessageListener : ISwrveMessageListener
    {
        IGameController gameController;
        
        public CustomMessageListener (IGameController gameController)
        {
            this.gameController = gameController;
        }

        public void OnShow (SwrveMessageFormat format)
        {
            // Pause game
            this.gameController.getGame().pauseGame();
        }

        public void OnShowing (SwrveMessageFormat format)
        {
        }

        public void OnDismiss (SwrveMessageFormat format)
        {
            // Resume game
            this.gameController.getGame().resumeGame();
        }
    }
}

#endif

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public sealed class DormDroneScreen : MonoBehaviour
{
    private static readonly string[] Questions =
    {
        "How encouraged did you feel to pick up the trash after the drone pointed it out?",
        "How much did you like the drone's encouragement?",
        "How likely would you be to clean your real room more often if you had a robot like this?",
        "How easy was it to tell which item the drone wanted you to clean up?",
        "How clear were the drone's spoken instructions?",
        "How comfortable did you feel with the drone flying around you?",
        "What would you change about how the drone helped you?"
    };

    private static readonly Color PanelColor = new Color32(23, 31, 36, 255);
    private static readonly Color ButtonColor = new Color32(65, 108, 91, 255);
    private Font font;
    private GameObject canvasObject;
    private RectTransform finish;
    private UnityEngine.UI.Text subtitle;
    private readonly List<string> answers = new();

    public IReadOnlyList<string> Answers => answers;
    public string ParticipantName { get; private set; }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        canvasObject = new GameObject("Drone UI", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        var captions = Rect(canvas.transform, "Captions", 0.13f, 0.035f, 0.87f, 0.135f);
        captions.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.035f, 0.04f, 0.90f);
        subtitle = Label(captions, "Subtitle", 0.035f, 0.08f, 0.965f, 0.92f, "", 24);
        captions.gameObject.SetActive(false);
        ShowStartScreen();
    }

    private void ShowStartScreen()
    {
        if (!EventSystem.current)
        {
            var events = new GameObject("Drone UI Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(canvasObject.transform, false);
        }

        var overlay = Rect(canvasObject.transform, "Start Screen", 0, 0, 1, 1);
        overlay.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color32(12, 18, 21, 255);
        var card = Rect(overlay, "Start Card", 0.23f, 0.22f, 0.77f, 0.78f);
        card.gameObject.AddComponent<UnityEngine.UI.Image>().color = PanelColor;
        Label(card, "Start Title", 0.08f, 0.66f, 0.92f, 0.88f, "Dorm Room Cleanup", 43);
        Label(card, "Participant Label", 0.1f, 0.52f, 0.9f, 0.67f, "Enter your name to begin.", 23);
        var input = Input(card, "Participant Name", "Your name", 0.16f, 0.34f, 0.84f, 0.50f);
        input.characterLimit = 120;
        var start = Button(card, "Start", 0.33f, 0.12f, 0.67f, 0.28f, () =>
        {
            string name = input.text.Trim();
            if (name.Length == 0 || ParticipantName != null) return;
            ParticipantName = name;
            overlay.gameObject.SetActive(false);
            Destroy(overlay.gameObject);
        });
        start.interactable = false;
        input.onValueChanged.AddListener(value => start.interactable = !string.IsNullOrWhiteSpace(value));
        input.ActivateInputField();
    }

    public void ShowSubtitle(string message)
    {
        subtitle.text = message ?? "";
        subtitle.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    public void ShowCompletion()
    {
        ShowSubtitle(null);
        var overlay = Rect(canvasObject.transform, "Completion Screen", 0, 0, 1, 1);
        overlay.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.035f, 0.04f, 0.94f);
        finish = Rect(overlay, "Questionnaire Card", 0.20f, 0.22f, 0.80f, 0.78f);
        finish.gameObject.AddComponent<UnityEngine.UI.Image>().color = PanelColor;
        var page = Page();
        Label(page, "Completion Title", 0.1f, 0.48f, 0.9f, 0.82f, "Cleaning Complete", 48);
        Button(page, "Continue", 0.32f, 0.20f, 0.68f, 0.36f, () => ShowQuestion(0));
    }

    private void ShowQuestion(int index)
    {
        var page = Page();
        if (index == Questions.Length)
        {
            Label(page, "Thank You", 0.1f, 0.22f, 0.9f, 0.78f, "Thank you for your feedback.", 38);
            return;
        }

        Label(page, "Questionnaire Progress", 0.08f, 0.77f, 0.92f, 0.94f, $"Question {index + 1} of {Questions.Length}", 20);
        Label(page, "Question", 0.08f, 0.48f, 0.92f, 0.79f, Questions[index], 28);
        if (index < Questions.Length - 1)
        {
            Label(page, "Rating Scale", 0.08f, 0.33f, 0.92f, 0.49f, "1 = Not at all     5 = Very much", 18);
            for (int i = 0; i < 5; i++)
            {
                string rating = (i + 1).ToString();
                float left = 0.14f + i * 0.15f;
                Button(page, rating, left, 0.13f, left + 0.12f, 0.30f, () => Answer(index, rating));
            }
            return;
        }

        var input = Input(page, "Optional Response", "Optional", 0.10f, 0.29f, 0.90f, 0.45f);
        var error = Label(page, "Save Error", 0.04f, 0.01f, 0.96f, 0.11f, "", 17);
        error.color = new Color32(255, 181, 162, 255);
        Button(page, "Finish", 0.33f, 0.11f, 0.67f, 0.26f, () => Finish(input.text, error));
    }

    private UnityEngine.UI.InputField Input(Transform parent, string name, string hint, float x, float y, float right, float top)
    {
        var field = Rect(parent, name, x, y, right, top);
        var image = field.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color32(47, 59, 66, 255);
        var input = field.gameObject.AddComponent<UnityEngine.UI.InputField>();
        input.targetGraphic = image;
        input.textComponent = Label(field, "Response", 0.03f, 0.02f, 0.97f, 0.98f, "", 20);
        input.textComponent.alignment = TextAnchor.MiddleLeft;
        var placeholder = Label(field, "Placeholder", 0.03f, 0.02f, 0.97f, 0.98f, hint, 20);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.color = new Color32(194, 202, 203, 255);
        input.placeholder = placeholder;
        return input;
    }

    private void Finish(string response, UnityEngine.UI.Text error)
    {
        if (answers.Count != Questions.Length - 1) return;
        answers.Add(response);
        try
        {
            QuestionnaireCsv.Append(ParticipantName, answers);
        }
        catch (System.Exception ex) when (ex is IOException || ex is System.UnauthorizedAccessException)
        {
            answers.RemoveAt(answers.Count - 1);
            error.text = "Could not save. Close the CSV if it is open, then select Finish again.";
            Debug.LogWarning($"Questionnaire CSV could not be saved: {ex.Message}");
            return;
        }
        ShowQuestion(Questions.Length);
    }

    private void Answer(int index, string answer)
    {
        if (answers.Count != index) return;
        answers.Add(answer);
        ShowQuestion(index + 1);
    }

    private RectTransform Page()
    {
        if (finish.childCount > 0)
        {
            var previous = finish.GetChild(0).gameObject;
            previous.SetActive(false);
            previous.transform.SetParent(null);
            Destroy(previous);
        }
        return Rect(finish, "Page", 0, 0, 1, 1);
    }

    private UnityEngine.UI.Button Button(Transform parent, string title, float x, float y, float right, float top, UnityEngine.Events.UnityAction clicked)
    {
        var rect = Rect(parent, title + " Button", x, y, right, top);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = ButtonColor;
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(clicked);
        Label(rect, "Button Label", 0, 0, 1, 1, title, 22);
        return button;
    }

    private UnityEngine.UI.Text Label(Transform parent, string name, float x, float y, float right, float top, string message, int size)
    {
        var label = Rect(parent, name, x, y, right, top).gameObject.AddComponent<UnityEngine.UI.Text>();
        label.font = font;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = size;
        label.raycastTarget = false;
        label.text = message;
        return label;
    }

    private static RectTransform Rect(Transform parent, string name, float x, float y, float right, float top)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void OnDestroy() => Destroy(canvasObject);
}

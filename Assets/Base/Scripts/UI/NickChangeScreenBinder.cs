using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ручная раскладка: повесь на пустой объект (например корень UI), укажи ссылки в инспекторе.
/// </summary>
public class NickChangeScreenBinder : MonoBehaviour, INickChangeUi
{
    [Header("Обязательно")]
    [SerializeField] private AppManager appManager;

    [Header("Главный экран — кнопка с текущим именем")]
    [SerializeField] private Button nickOpenButton;
    [SerializeField] private TextMeshProUGUI nickButtonLabel;

    [Header("Экран / панель редактирования")]
    [SerializeField] private GameObject nickEditPanelRoot;
    [SerializeField] private TMP_InputField nickInput;
    [SerializeField] private Button saveNickButton;
    [SerializeField] private Button cancelNickButton;
    [Tooltip("Опционально: Image с Button на полноэкранном затемнении — закрывает панель по клику")]
    [SerializeField] private Button backgroundCloseButton;

    private void Awake()
    {
        if (nickOpenButton != null)
            nickOpenButton.onClick.AddListener(OnOpenClicked);
        if (saveNickButton != null)
            saveNickButton.onClick.AddListener(OnSaveClicked);
        if (cancelNickButton != null)
            cancelNickButton.onClick.AddListener(ClosePanel);
        if (backgroundCloseButton != null)
            backgroundCloseButton.onClick.AddListener(ClosePanel);

        if (nickEditPanelRoot != null)
            nickEditPanelRoot.SetActive(false);
    }

    private void Start()
    {
        if (appManager != null)
            appManager.RegisterNickChangeUi(this);

        RefreshButtonLabel();
    }

    public void RefreshButtonLabel()
    {
        if (nickButtonLabel != null && appManager != null)
            nickButtonLabel.text = appManager.GetNickButtonLabelText();
    }

    public void OnOpenClicked()
    {
        if (appManager == null)
            return;

        if (nickInput != null)
        {
            string seed = appManager.GetNickForEditField();
            nickInput.SetTextWithoutNotify(string.IsNullOrEmpty(seed) ? string.Empty : seed);
        }

        if (nickEditPanelRoot != null)
            nickEditPanelRoot.SetActive(true);
    }

    public void ClosePanel()
    {
        if (nickEditPanelRoot != null)
            nickEditPanelRoot.SetActive(false);
    }

    private void OnSaveClicked()
    {
        if (appManager == null || nickInput == null)
            return;

        appManager.OnChangeNickRequested(
            nickInput.text,
            onSuccess: ClosePanel,
            onError: _ => { });
    }

    private void OnDestroy()
    {
        if (nickOpenButton != null)
            nickOpenButton.onClick.RemoveListener(OnOpenClicked);
        if (saveNickButton != null)
            saveNickButton.onClick.RemoveListener(OnSaveClicked);
        if (cancelNickButton != null)
            cancelNickButton.onClick.RemoveListener(ClosePanel);
        if (backgroundCloseButton != null)
            backgroundCloseButton.onClick.RemoveListener(ClosePanel);
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class LoadingPanelView : MonoBehaviour
{
    private static readonly Color TransparentWhite = new Color(1f, 1f, 1f, 0f);

    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TMP_Text progressPercentText;

    [SerializeField, FormerlySerializedAs("useCapsuleFillSprite")]
    private bool useProceduralRoundedFill = true;

    [SerializeField, FormerlySerializedAs("capsuleTextureHeight")]
    private int fillTextureHeight = 64;

    [SerializeField, Range(2, 48)]
    private int cornerRadiusPixels = 10;

    private Sprite _runtimeFillSprite;

    private void Awake()
    {
        if (progressSlider == null)
            progressSlider = GetComponentInChildren<Slider>(true);
        if (progressPercentText == null)
            progressPercentText = GetComponentInChildren<TMP_Text>(true);

        if (!useProceduralRoundedFill)
            return;

        if (progressSlider != null && progressSlider.fillRect != null)
        {
            var fillImg = progressSlider.fillRect.GetComponent<Image>();
            if (fillImg != null)
                ApplyRoundedRectFillSprite(fillImg);
        }
        else if (progressFillImage != null)
            ApplyRoundedRectFillSprite(progressFillImage);
    }

    private void OnDestroy()
    {
        if (_runtimeFillSprite == null)
            return;
        Texture2D t = _runtimeFillSprite.texture;
        Destroy(_runtimeFillSprite);
        if (t != null)
            Destroy(t);
    }

    private void ApplyRoundedRectFillSprite(Image image)
    {
        int h = Mathf.Clamp(fillTextureHeight, 16, 256);
        int w = Mathf.Max(h * 4, 64);
        int r = Mathf.Clamp(cornerRadiusPixels, 2, Mathf.Max(2, Mathf.Min(w, h) / 2 - 2));

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "LoadingRoundedRectFill",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                bool inner = IsInsideRoundedRect(px, py, w, h, r);
                tex.SetPixel(x, y, inner ? Color.white : TransparentWhite);
            }
        }

        tex.Apply(false, false);

        _runtimeFillSprite = Sprite.Create(
            tex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);

        image.sprite = _runtimeFillSprite;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = 0;
    }

    private static bool IsInsideRoundedRect(float px, float py, int w, int h, int r)
    {
        if (px >= r && px < w - r)
            return true;

        if (py >= r && py < h - r && (px < r || px >= w - r))
            return true;

        float r2 = r * r;

        if (px < r && py < r)
            return DistSq(px, py, r, r) <= r2 + 0.01f;

        if (px >= w - r && py < r)
            return DistSq(px, py, w - r, r) <= r2 + 0.01f;

        if (px < r && py >= h - r)
            return DistSq(px, py, r, h - r) <= r2 + 0.01f;

        if (px >= w - r && py >= h - r)
            return DistSq(px, py, w - r, h - r) <= r2 + 0.01f;

        return false;
    }

    private static float DistSq(float px, float py, float cx, float cy)
    {
        float dx = px - cx;
        float dy = py - cy;
        return dx * dx + dy * dy;
    }

    public void SetProgress01(float value01)
    {
        float v = Mathf.Clamp01(value01);

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = v;
        }
        else if (progressFillImage != null)
            progressFillImage.fillAmount = v;

        if (progressPercentText != null)
            progressPercentText.text = $"{Mathf.RoundToInt(v * 100f)}%";
    }
}

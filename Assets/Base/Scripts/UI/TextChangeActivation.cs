using System;
using TMPro;
using UnityEngine;

[RequireComponent (typeof(TextMeshProUGUI))]
public class TextChangeActivation : MonoBehaviour
{
    TextMeshProUGUI m_TextComponent;
    [SerializeField] Color actionColor = Color.green;
    Color staticColor = Color.white;
    void Start()
    {
        m_TextComponent = GetComponent<TextMeshProUGUI>();
        staticColor = m_TextComponent.color;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
    }

    private void ON_TEXT_CHANGED(UnityEngine.Object @object)
    {
        if (@object == m_TextComponent)
        {
            m_TextComponent.color = actionColor;
        }
    }

    private void Update()
    {
        m_TextComponent.color = Color.Lerp(m_TextComponent.color, staticColor, Time.deltaTime * 2f);
    }
}

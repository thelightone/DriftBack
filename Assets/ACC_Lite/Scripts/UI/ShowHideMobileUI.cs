using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowHideMobileUI : MonoBehaviour
{
    [SerializeField] GameObject MobileUI;
    [SerializeField] GameObject PcConcoleUI;

    void Start()
    {
        if (MobileUI != null)
        {
            MobileUI.SetActive(true);
        }

        if (PcConcoleUI != null && PcConcoleUI != MobileUI)
        {
            PcConcoleUI.SetActive(false);
        }
    }
}

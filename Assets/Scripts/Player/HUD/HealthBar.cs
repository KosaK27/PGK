using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Transform   heartsContainer;
    [SerializeField] private GameObject  heartPrefab;
    [SerializeField] private int         heartsPerRow = 10;
    [SerializeField] private float       heartSize    = 32f;
    [SerializeField] private float       heartSpacing = 4f;

    [Header("Heart Sprites")]
    [SerializeField] private Sprite heartEmpty;
    [SerializeField] private Sprite heartQuarter;
    [SerializeField] private Sprite heartHalf;
    [SerializeField] private Sprite heartThreeQuarter;
    [SerializeField] private Sprite heartFull;

    private List<Image> _heartImages = new();
    private int         _lastMaxHP   = -1;

    void Start()
    {
        StartCoroutine(Init());
    }

    System.Collections.IEnumerator Init()
    {
        while (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
            yield return null;
        }

        playerStats.OnHealthChanged += Refresh;
        BuildHearts();
        Refresh(playerStats.currentHP, playerStats.maxHP);
    }

    void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged -= Refresh;
    }

    private void BuildHearts()
    {
        foreach (Transform child in heartsContainer)
            Destroy(child.gameObject);
        _heartImages.Clear();

        int heartCount = playerStats.maxHP / 4;
        _lastMaxHP     = playerStats.maxHP;

        for (int i = 0; i < heartCount; i++)
        {
            var go = Instantiate(heartPrefab, heartsContainer);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(heartSize, heartSize);

            int row = i / heartsPerRow;
            int col = i % heartsPerRow;
            rt.anchoredPosition = new Vector2(
                col * (heartSize + heartSpacing),
               -row * (heartSize + heartSpacing));

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            _heartImages.Add(img);
        }
    }

    public void Refresh(int currentHP, int maxHP)
    {
        if (maxHP != _lastMaxHP)
            BuildHearts();

        for (int i = 0; i < _heartImages.Count; i++)
        {
            int heartMin = i * 4;
            int hp       = currentHP - heartMin;

            Sprite sprite;
            if      (hp <= 0) sprite = heartEmpty;
            else if (hp == 1) sprite = heartQuarter;
            else if (hp == 2) sprite = heartHalf;
            else if (hp == 3) sprite = heartThreeQuarter;
            else              sprite = heartFull;

            _heartImages[i].sprite = sprite;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject container;
    [SerializeField] private TMPro.TextMeshProUGUI nameText;
    [SerializeField] private string bossName = "Boss1";

    void Awake()
    {
        if (container != null)
            container.SetActive(false);
    }

    public void Initialize(EntityStats stats)
    {
        if (container != null) container.SetActive(true);
        if (nameText != null) nameText.text = bossName;
        if (slider != null)
        {
            slider.minValue = 0;
            slider.maxValue = stats.data.maxHP;
            slider.value = stats.CurrentHP;
        }
        stats.OnHPChanged += OnHPChanged;
    }

    private void OnHPChanged(int current, int max)
    {
        if (slider != null) slider.value = current;
    }

    public void Hide()
    {
        if (container != null) container.SetActive(false);
    }
}
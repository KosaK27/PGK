using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldRowUI : MonoBehaviour
{
    [SerializeField] private Image _selectedIndicator;
    [SerializeField] private TextMeshProUGUI _nameLabel;
    [SerializeField] private TextMeshProUGUI _detailLabel;
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _deleteButton;

    private string _id;

    public void Setup(WorldSaveData data, bool selected, Action<string> onSelect, Action<string> onDelete)
    {
        _id = data.id;
        _nameLabel.text = data.worldName;
        string played = data.lastPlayedAt > 0 ? new DateTime(data.lastPlayedAt).ToString("MMM d, yyyy") : "Never played";
        _detailLabel.text = $"{data.width}x{data.height}  |  {played}";
        _selectedIndicator.gameObject.SetActive(selected);
        _selectButton.onClick.RemoveAllListeners();
        _deleteButton.onClick.RemoveAllListeners();
        _selectButton.onClick.AddListener(() => onSelect(_id));
        _deleteButton.onClick.AddListener(() => onDelete(_id));
    }
}
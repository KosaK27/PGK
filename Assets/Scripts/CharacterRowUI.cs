using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterRowUI : MonoBehaviour
{
    [SerializeField] private Image _selectedIndicator;
    [SerializeField] private TextMeshProUGUI _nameLabel;
    [SerializeField] private TextMeshProUGUI _dateLabel;
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _deleteButton;
    [SerializeField] private Color _selectedColor = new Color(0.3f, 0.8f, 0.4f, 1f);
    [SerializeField] private Color _unselectedColor = new Color(1f, 1f, 1f, 0f);

    private string _id;

    public void Setup(CharacterSaveData data, bool selected, Action<string> onSelect, Action<string> onDelete)
    {
        _id = data.id;
        _nameLabel.text = data.characterName;
        _dateLabel.text = new DateTime(data.createdAt).ToString("MMM d, yyyy");
        _selectedIndicator.color = selected ? _selectedColor : _unselectedColor;
        _selectButton.interactable = !selected;
        _selectButton.onClick.RemoveAllListeners();
        _deleteButton.onClick.RemoveAllListeners();
        _selectButton.onClick.AddListener(() => onSelect(_id));
        _deleteButton.onClick.AddListener(() => onDelete(_id));
    }
}
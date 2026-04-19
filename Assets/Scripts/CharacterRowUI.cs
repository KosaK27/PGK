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

    private string _id;

    public void Setup(CharacterSaveData data, bool selected, Action<string> onSelect, Action<string> onDelete)
    {
        _id = data.id;
        _nameLabel.text = data.characterName;
        _dateLabel.text = new DateTime(data.createdAt).ToString("MMM d, yyyy");
        _selectButton.interactable = !selected;
        _selectedIndicator.gameObject.SetActive(selected);
        _selectButton.onClick.RemoveAllListeners();
        _deleteButton.onClick.RemoveAllListeners();
        _selectButton.onClick.AddListener(() => onSelect(_id));
        _deleteButton.onClick.AddListener(() => onDelete(_id));
    }
}
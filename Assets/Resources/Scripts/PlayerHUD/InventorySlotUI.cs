using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image _durabilityBar;
    [SerializeField] private GameObject _durabilityBarGameObject;
    [SerializeField] private Image _itemIcon;
    [SerializeField] private float _selectedHeightOffset = 10f; // Offset for the selected slot
    [SerializeField] private float _animationDuration = 0.2f; // Duration of the animation
    [SerializeField] private Ease _animationEase = Ease.OutQuad; // Easing function for the animation
    float _originalYPosition; // Store the original Y position of the slot
    Tween _currentTween; // Store the current tween for the animation

    void Awake()
    {
        _originalYPosition = transform.localPosition.y; // Store the original Y position
    }
    public void UpdateSlot(InventoryItemData itemData, Sprite icon)
    {
        if (itemData.itemID == ItemID.None)
        {
            _itemIcon.sprite = null;
            _itemIcon.enabled = false; // Disable the icon if there's no item
            _durabilityBarGameObject.SetActive(false);
        }
        else
        {
            _itemIcon.sprite = icon;
            _itemIcon.enabled = true; // Enable the icon if there's an item
            if (itemData.infiniteDurability)
            {
                _durabilityBarGameObject.SetActive(false);
            }
            else
            {
                _durabilityBarGameObject.SetActive(true);
                _durabilityBar.fillAmount = Mathf.Clamp01(itemData.currentDurability / itemData.maxDurability);
            }
        }
    }
    public void SetSelected(bool isSelected)
    {
        if (_currentTween != null)
        {
            _currentTween.Kill(true); // Stop the current tween if it's active
        }
        float targetYPosition = isSelected ? _originalYPosition + _selectedHeightOffset : _originalYPosition;
        _currentTween = transform.DOLocalMoveY(targetYPosition, _animationDuration).SetEase(_animationEase);
    }
}
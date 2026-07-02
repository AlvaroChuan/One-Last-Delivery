using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image _durabilityBar;
    [SerializeField] private Image _itemIcon;
    [SerializeField] private float _selectedHeightOffset = 10f; // Offset for the selected slot
    [SerializeField] private float _animationDuration = 0.2f; // Duration of the animation
    [SerializeField] private Ease _animationEase = Ease.OutQuad; // Easing function for the animation
    float _originalYPosition; // Store the original Y position of the slot

    void Awake()
    {
        _originalYPosition = transform.localPosition.y; // Store the original Y position
    }
    public void UpdateSlot(InventoryItemData itemData, Sprite icon)
    {
        if (itemData.itemID == ItemID.None)
        {
            _itemIcon.sprite = null;
            _itemIcon.color = new Color(1, 1, 1, 0); // Make the icon transparent if no item
        }
        else
        {
            _itemIcon.sprite = icon;
            _itemIcon.color = new Color(1, 1, 1, 1); // Make the icon visible if there's an item
            if (itemData.infiniteDurability)
            {
                _durabilityBar.fillAmount = 1f; // Full bar for infinite durability
            }
            else
            {
                _durabilityBar.fillAmount = Mathf.Clamp01(itemData.currentDurability / itemData.maxDurability);
            }
        }
    }
    public void SetSelected(bool isSelected)
    {
        float targetYPosition = isSelected ? _originalYPosition + _selectedHeightOffset : _originalYPosition;
        transform.DOLocalMoveY(targetYPosition, _animationDuration).SetEase(_animationEase);
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeyBinder : MonoBehaviour
{
    [SerializeField] private Button _keyButton;
    [SerializeField] private InputAction _action;

    [SerializeField] private TMP_Text _actionText;
    [SerializeField] private TMP_Text _bindingText;

    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

    public void OnCreate(InputAction actionReference)
    {
        _action = actionReference;
        _actionText.text = actionReference.name;

        InputPersistenceManager.LoadRebind(_action);

        UpdateBindingDisplay();
        _keyButton.onClick.AddListener(StartRebinding);
    }

    private void StartRebinding()
    {
        if (_action == null) return;

        _action.Disable();

        _bindingText.text = "Press key";

        _rebindOperation = _action.PerformInteractiveRebinding()
            .WithBindingGroup("Keyboard&Mouse")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(x => FinishRebinding())
            .OnCancel(x => CancelRebinding())
            .Start();
    }

    private void FinishRebinding()
    {
        InputPersistenceManager.SaveRebind(_action);
        UpdateBindingDisplay();
        CleanUp();
    }

    private void CancelRebinding()
    {
        UpdateBindingDisplay();
        CleanUp();
    }

    private void CleanUp()
    {
        _rebindOperation.Dispose();

        _bindingText.gameObject.SetActive(true);

        _action.Enable();
    }

    private void UpdateBindingDisplay()
    {
        string displayString = _action.GetBindingDisplayString(
            InputBinding.DisplayStringOptions.DontUseShortDisplayNames,
            "Keyboard&Mouse");
        _bindingText.text = displayString;
    }

    private void OnDestroy()
    {
        _rebindOperation?.Dispose();
    }
}

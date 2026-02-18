public interface IInteractable
{
    void Interact();
    void OnFocus();
    void OnUnfocus();
    string GetInteractionText();
}
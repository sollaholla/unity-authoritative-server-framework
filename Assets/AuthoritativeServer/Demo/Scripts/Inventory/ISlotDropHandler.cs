namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// Receive item slot drag-drop events.
    /// </summary>
    public interface ISlotDropHandler
    {
        /// <summary>
        /// Called when a slot wants a drop command to be executed on this drop handler.
        /// </summary>
        /// <param name="slot"></param>
        void OnDrop(ItemSlotInterface slot);
    }
}

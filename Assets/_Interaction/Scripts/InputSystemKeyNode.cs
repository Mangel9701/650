/*
2026-05-07 AI-Tag
This was created with the help of Assistant, a Unity Artificial Intelligence product.
*/
using System;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

[UnitCategory("Events/Input")]
[UnitTitle("On Input System Key")]
[UnitSubtitle("Detects New Input System key events")]
public class InputSystemKeyNode : EventUnit<EmptyEventArgs>
{
    [DoNotSerialize]
    public ValueInput key;

    [DoNotSerialize]
    public ValueInput actionType;

    public enum KeyActionType { WasPressed, WasReleased, IsPressed }

    protected override bool register => true;

    protected override void Definition()
    {
        base.Definition();
        key = ValueInput<Key>("Key", Key.E);
        actionType = ValueInput<KeyActionType>("Action", KeyActionType.WasPressed);
    }

    // Usamos el hook de Update para comprobar la tecla cada frame
    public override EventHook GetHook(GraphReference reference)
    {
        return new EventHook(EventHooks.Update);
    }

    protected override bool ShouldTrigger(Flow flow, EmptyEventArgs args)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return false;

        Key targetKey = flow.GetValue<Key>(key);
        KeyActionType type = flow.GetValue<KeyActionType>(actionType);

        return type switch
        {
            KeyActionType.WasPressed => keyboard[targetKey].wasPressedThisFrame,
            KeyActionType.WasReleased => keyboard[targetKey].wasReleasedThisFrame,
            KeyActionType.IsPressed => keyboard[targetKey].isPressed,
            _ => false
        };
    }
}

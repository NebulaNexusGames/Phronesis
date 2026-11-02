using UnityEngine;

public enum ControlLayout
{
    Player1Standard,
    Player2Arrows
}

public static class PlayerInput
{
    public static float Horizontal(ControlLayout layout)
    {
        bool right = layout == ControlLayout.Player1Standard ? Input.GetKey(KeyCode.D) : Input.GetKey(KeyCode.RightArrow);
        bool left = layout == ControlLayout.Player1Standard ? Input.GetKey(KeyCode.A) : Input.GetKey(KeyCode.LeftArrow);
        return (right ? 1f : 0f) - (left ? 1f : 0f);
    }

    public static bool JumpPressed(ControlLayout layout)
    {
        return layout == ControlLayout.Player1Standard ? Input.GetKeyDown(KeyCode.W) : Input.GetKeyDown(KeyCode.UpArrow);
    }

    public static bool JumpHeld(ControlLayout layout)
    {
        return layout == ControlLayout.Player1Standard ? Input.GetKey(KeyCode.W) : Input.GetKey(KeyCode.UpArrow);
    }

    public static bool CrouchHeld(ControlLayout layout)
    {
        return layout == ControlLayout.Player1Standard ? Input.GetKey(KeyCode.S) : Input.GetKey(KeyCode.DownArrow);
    }

    public static bool RunHeld(ControlLayout layout)
    {
        return layout == ControlLayout.Player1Standard ? Input.GetKey(KeyCode.LeftShift) : Input.GetKey(KeyCode.RightShift);
    }

    public static bool DashPressed(ControlLayout layout)
    {
        return layout == ControlLayout.Player1Standard ? Input.GetKeyDown(KeyCode.LeftControl) : Input.GetKeyDown(KeyCode.RightControl);
    }

    public static bool InteractHeld(ControlLayout layout)
    {
        return layout == ControlLayout.Player1Standard ? Input.GetKey(KeyCode.E) : Input.GetKey(KeyCode.Return);
    }

    public static bool InteractPressed(ControlLayout layout)
    {
        return layout == ControlLayout.Player1Standard ? Input.GetKeyDown(KeyCode.E) : Input.GetKeyDown(KeyCode.Return);
    }

    public static bool HealPressed()
    {
        return Input.GetKeyDown(KeyCode.Q);
    }
}

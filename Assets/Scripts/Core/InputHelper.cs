using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

// Đọc bàn phím, chạy được với cả Input Manager cũ lẫn Input System mới.
public static class InputHelper
{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    static bool Down(Key k) { var kb = Keyboard.current; return kb != null && kb[k].isPressed; }
    static bool Pressed(Key k) { var kb = Keyboard.current; return kb != null && kb[k].wasPressedThisFrame; }
    public static float Vertical() { float v = 0; if (Down(Key.W) || Down(Key.UpArrow)) v += 1; if (Down(Key.S) || Down(Key.DownArrow)) v -= 1; return v; }
    public static float Horizontal() { float v = 0; if (Down(Key.D) || Down(Key.RightArrow)) v += 1; if (Down(Key.A) || Down(Key.LeftArrow)) v -= 1; return v; }
    public static bool Brake() { return Down(Key.Space); }
    public static bool Nitro() { return Down(Key.LeftShift) || Down(Key.RightShift); }
    public static bool ResetPressed() { return Pressed(Key.R); }
    public static bool EscPressed() { return Pressed(Key.Escape); }
#else
    public static float Vertical() { return (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f); }
    public static float Horizontal() { return (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f); }
    public static bool Brake() { return Input.GetKey(KeyCode.Space); }
    public static bool Nitro() { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
    public static bool ResetPressed() { return Input.GetKeyDown(KeyCode.R); }
    public static bool EscPressed() { return Input.GetKeyDown(KeyCode.Escape); }
#endif
}

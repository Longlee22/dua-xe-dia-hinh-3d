#if UNITY_EDITOR
using UnityEditor;

// Chạy 1 lần: xuất ảnh preview rồi dựng lại toàn bộ scene.
public static class RacingSetup
{
    [MenuItem("Game/0. Setup toan bo (anh + scene)")]
    public static void RunAll()
    {
        PreviewRenderer.RenderAll();
        GameBuilder.BuildAll();
    }
}
#endif

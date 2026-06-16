namespace XOSC.Motor.UI;

public static class ThemePresets
{
    public record Preset(string Name, float[] Accent, float[] Bg, float[] Sidebar, float[] Card);

    public static readonly Preset[] All =
    {
        new("XOSC Default", new[]{ 0.38f, 0.73f, 1.00f }, new[]{ 0.10f, 0.10f, 0.13f }, new[]{ 0.07f, 0.07f, 0.09f }, new[]{ 0.14f, 0.14f, 0.18f }),
        new("Midnight Purple", new[]{ 0.70f, 0.40f, 1.00f }, new[]{ 0.08f, 0.06f, 0.12f }, new[]{ 0.05f, 0.04f, 0.09f }, new[]{ 0.12f, 0.10f, 0.18f }),
        new("Forest Green", new[]{ 0.30f, 0.85f, 0.50f }, new[]{ 0.06f, 0.11f, 0.08f }, new[]{ 0.04f, 0.08f, 0.05f }, new[]{ 0.09f, 0.15f, 0.11f }),
        new("Sunset Orange", new[]{ 1.00f, 0.55f, 0.20f }, new[]{ 0.13f, 0.09f, 0.07f }, new[]{ 0.09f, 0.06f, 0.05f }, new[]{ 0.18f, 0.12f, 0.09f }),
        new("Rose Pink", new[]{ 1.00f, 0.45f, 0.65f }, new[]{ 0.13f, 0.08f, 0.10f }, new[]{ 0.09f, 0.05f, 0.07f }, new[]{ 0.18f, 0.11f, 0.14f }),
        new("Ice White", new[]{ 0.10f, 0.45f, 0.90f }, new[]{ 0.94f, 0.95f, 0.97f }, new[]{ 0.84f, 0.86f, 0.90f }, new[]{ 0.99f, 0.99f, 1.00f }),
        new("Deep Red", new[]{ 1.00f, 0.25f, 0.25f }, new[]{ 0.10f, 0.06f, 0.06f }, new[]{ 0.07f, 0.04f, 0.04f }, new[]{ 0.15f, 0.09f, 0.09f }),
        new("Cyberpunk", new[]{ 1.00f, 0.95f, 0.00f }, new[]{ 0.05f, 0.05f, 0.07f }, new[]{ 0.03f, 0.03f, 0.05f }, new[]{ 0.09f, 0.09f, 0.12f })
    };
}
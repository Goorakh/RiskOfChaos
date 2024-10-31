namespace RiskOfChaos.Utilities
{
    public static class Ease
    {
        public static float InOutQuad(float t)
        {
            if (t < 0.5f)
            {
                return 2f * t * t;
            }
            else
            {
                float v = (-2f * t) + 2f;
                return 1f - (v * v / 2f);
            }
        }
    }
}

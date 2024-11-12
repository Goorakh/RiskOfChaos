namespace RiskOfChaos.Utilities
{
    public static class Ease
    {
        public static float InQuad(float t)
        {
            return t * t;
        }

        public static float OutQuad(float t)
        {
            return 1f - ((1f - t) * (1f - t));
        }

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

        public static float InCubic(float t)
        {
            return t * t * t;
        }

        public static float OutCubic(float t)
        {
            float v = 1f - t;
            return 1f - (v * v * v);
        }

        public static float InOutCubic(float t)
        {
            if (t < 0.5f)
            {
                return 4f * t * t * t;
            }
            else
            {
                float v = (-2f * t) + 2f;
                return 1f - (v * v * v / 2f);
            }
        }
    }
}

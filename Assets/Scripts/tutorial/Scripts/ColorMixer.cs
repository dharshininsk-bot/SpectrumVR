using UnityEngine;

namespace EscapeRoom.Tutorial
{
    public static class ColorMixer
    {
        /// <summary>
        /// Mixes two colors using a subtractive color model (CMY), 
        /// which simulates physical paint mixing.
        /// </summary>
        public static Color MixSubtractive(Color color1, Color color2)
        {
            // Convert RGB to CMYK
            Vector4 cmyk1 = RGBToCMYK(color1);
            Vector4 cmyk2 = RGBToCMYK(color2);

            // Average the CMYK values
            Vector4 mixedCMYK = new Vector4(
                (cmyk1.x + cmyk2.x) / 2f,
                (cmyk1.y + cmyk2.y) / 2f,
                (cmyk1.z + cmyk2.z) / 2f,
                Mathf.Max(cmyk1.w, cmyk2.w) // Use the darkest key (black) value
            );

            // Convert back to RGB
            return CMYKToRGB(mixedCMYK);
        }

        private static Vector4 RGBToCMYK(Color rgb)
        {
            float k = 1.0f - Mathf.Max(rgb.r, Mathf.Max(rgb.g, rgb.b));
            if (k >= 1.0f)
            {
                return new Vector4(0, 0, 0, 1);
            }

            float c = (1.0f - rgb.r - k) / (1.0f - k);
            float m = (1.0f - rgb.g - k) / (1.0f - k);
            float y = (1.0f - rgb.b - k) / (1.0f - k);

            return new Vector4(c, m, y, k);
        }

        private static Color CMYKToRGB(Vector4 cmyk)
        {
            float c = cmyk.x;
            float m = cmyk.y;
            float y = cmyk.z;
            float k = cmyk.w;

            float r = (1.0f - c) * (1.0f - k);
            float g = (1.0f - m) * (1.0f - k);
            float b = (1.0f - y) * (1.0f - k);

            return new Color(r, g, b, 1.0f);
        }
    }
}

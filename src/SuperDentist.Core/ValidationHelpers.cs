using System;
namespace SuperDentist.Core
{
    public static class ValidationHelpers
    {
        public static bool IsNumeric(string text)
        {
            bool isValid = true;
            char current;
            for (int i = 0; i < text.Length && isValid != false; i++)
            {
                current = text[i];
                if (!('0' <= text[i] && '9' >= text[i]))
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        public static bool IsAlphabetic(string text)
        {
            char current;
            for (int i = 0; i < text.Length; i++)
            {
                current = text[i];
                if (char.IsNumber(current))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsValidId(string text)
        {
            if (text.Length != 9 || !IsNumeric(text))
            {
                return false;
            }

            return true;
        }

        public static bool IsValidPhoneNumber(string text)
        {
            if (!IsNumeric(text))
            {
                return false;
            }

            if (text.Length != 9 && text.Length != 10)
            {
                return false;
            }

            if (text[0] != '0')
            {
                return false;
            }

            bool isLandline = text.Length == 9;
            if (isLandline && text[1] != '8')
            {
                return false;
            }

            if (!isLandline && text[1] != '5')
            {
                return false;
            }

            return true;
        }
    }
}


